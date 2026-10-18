using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Silk.NET.OpenGL;
using beyondnations;

namespace beyondnations.desktop.text {

    /**
    * World-space nametags: a font atlas packed once at startup, one billboarded
    * quad per glyph, and one draw call for every label on screen (#222).
    *
    * What this replaces is worth stating, because it is the whole reason the
    * issue exists. Unity drew each nametag through CanvasFactory, which built a
    * fresh screen-space Canvas and a Text component per label. That is a
    * GameObject, a material and at least one draw call each, created and
    * destroyed as pawns came and went, and it was the single most expensive
    * thing the old UI layer did. Here every glyph of every visible label is a
    * quad in one vertex buffer, and the cost of showing a hundred names is one
    * glDrawElements.
    *
    * FontStashSharp owns the packing; this class owns the texture, the buffer
    * and the transform from the flat pen space the font measures in to the
    * billboarded quads the world wants. The camera is only ever a view matrix
    * and a projection matrix, so #219 can replace the camera underneath this
    * without touching it.
    */
    public unsafe class WorldLabelRenderer : IFontStashRenderer2, IDisposable {

        /**
        * The atlas is square and generous. The printable ASCII range at the
        * size below occupies a small fraction of it, which is what keeps every
        * glyph on one texture and therefore every label in one draw call.
        */
        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1024;

        /**
        * Glyphs are rasterised at this many pixels and then scaled in the
        * world, so a nametag stays legible when the camera is close without
        * needing a second atlas.
        */
        private const int FontSizePixels = 32;

        /**
        * How tall one line of a nametag is, in world units. A pawn is one unit
        * across, so this is roughly the height of the thing the name belongs
        * to.
        */
        private const float LineHeightInWorldUnits = 1.15f;

        /**
        * How far above an entity's origin the bottom of its nametag sits.
        */
        private const float HeightAboveEntity = 1.4f;

        /**
        * Everything drawable in a name or a behaviour description. Packed at
        * startup so that the atlas never has to grow during a frame, which is
        * what would otherwise split the labels across two textures and cost a
        * second draw call.
        */
        private const int PrewarmFirstCodepoint = 32;
        private const int PrewarmLastCodepoint = 126;

        private readonly GL gl;
        private readonly GlTextureManager textureManager;
        private readonly LabelShader shader;
        private readonly LabelCulling culling = new LabelCulling();

        private FontSystem fontSystem;
        private DynamicSpriteFont font;

        /**
        * One buffer and one batch per atlas texture. There is one of each in
        * practice, because the prewarm above fits comfortably on a single
        * atlas; the list exists so that a run which somehow needed a second
        * atlas would draw correctly and report two draw calls, rather than
        * sampling the wrong texture and quietly drawing nonsense.
        */
        private readonly List<AtlasGroup> groups = new List<AtlasGroup>();

        private bool ready;
        private int packedGlyphCount;
        private int drawCalls;
        private int quadsDrawn;
        private int labelsDrawn;
        private int labelsCulled;

        private Vector4 color = new Vector4(1f, 1f, 1f, 1f);

        // Set for the duration of one label and read by DrawQuad, which
        // FontStashSharp calls back into once per glyph.
        private Vector3 currentAnchor;
        private Vector3 currentRight;
        private Vector3 currentUp;
        private Vector2 currentPenOffset;
        private float currentWorldUnitsPerPixel;
        private bool counting;
        private int countedQuads;

        public WorldLabelRenderer(GL gl) {
            this.gl = gl;
            this.textureManager = new GlTextureManager(gl);
            this.shader = new LabelShader(gl);

            byte[] fontData = readFont();
            if (fontData == null) {
                Log.error("world-space labels are disabled: the bundled font could not be read");
                return;
            }

            FontSystemSettings settings = new FontSystemSettings();
            settings.TextureWidth = AtlasWidth;
            settings.TextureHeight = AtlasHeight;
            // Straight alpha, matching the source-alpha blend set up in
            // render(). Premultiplied would need a different blend func and
            // would gain nothing for single-colour glyphs.
            settings.PremultiplyAlpha = false;

            fontSystem = new FontSystem(settings);
            fontSystem.AddFont(fontData);
            font = fontSystem.GetFont(FontSizePixels);

            packedGlyphCount = prewarmAtlas();
            ready = true;

            Log.info("world-space labels ready: font DejaVu Sans at " + FontSizePixels + "px, atlas "
                + AtlasWidth + "x" + AtlasHeight + ", " + packedGlyphCount + " glyphs packed across "
                + textureManager.getTextureCount() + " atlas texture(s)");
        }

        public bool isReady() { return ready; }
        public int getPackedGlyphCount() { return packedGlyphCount; }
        public int getAtlasWidth() { return AtlasWidth; }
        public int getAtlasHeight() { return AtlasHeight; }
        public int getAtlasTextureCount() { return textureManager.getTextureCount(); }
        public int getDrawCallCount() { return drawCalls; }
        public int getQuadsDrawn() { return quadsDrawn; }
        public int getLabelsDrawn() { return labelsDrawn; }
        public int getLabelsCulled() { return labelsCulled; }
        public LabelCulling getCulling() { return culling; }

        public Vector4 getColor() { return color; }

        /**
        * The tint every nametag is drawn in. Configurable mainly so that a
        * headless run can render labels in a colour nothing else in the scene
        * produces, and the screenshot can then be checked for those pixels; a
        * claim that text appeared is worth no more than the count behind it.
        */
        public void setColor(Vector4 color) { this.color = color; }

        /**
        * Draws every label in the snapshot that survives culling.
        *
        * The camera arrives as two matrices and nothing else, which is what
        * keeps this independent of whichever camera #219 settles on.
        */
        public void render(IReadOnlyList<LabelItem> labels, Matrix4x4 view, Matrix4x4 projection) {
            drawCalls = 0;
            quadsDrawn = 0;
            labelsDrawn = 0;
            labelsCulled = 0;

            if (!ready || labels == null || labels.Count == 0) {
                return;
            }

            for (int i = 0; i < groups.Count; i++) {
                groups[i].buffer.clear();
            }

            build(labels, view, projection);

            if (quadsDrawn == 0) {
                return;
            }

            shader.use();

            Matrix4x4 viewCopy = view;
            Matrix4x4 projectionCopy = projection;
            gl.UniformMatrix4(shader.getViewLocation(), 1, false, (float*) &viewCopy);
            gl.UniformMatrix4(shader.getProjectionLocation(), 1, false, (float*) &projectionCopy);
            gl.Uniform1(shader.getAtlasLocation(), 0);

            // Nametags read as an overlay on the world rather than as objects
            // in it: they are never occluded by terrain, and never occlude each
            // other. Depth testing is therefore off for the label pass and
            // restored afterwards, so the primitive renderer is unaffected.
            // Face culling is off too, since a billboard has no meaningful
            // winding once the camera can be behind it.
            gl.Disable(EnableCap.DepthTest);
            gl.Disable(EnableCap.CullFace);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            gl.ActiveTexture(TextureUnit.Texture0);

            for (int i = 0; i < groups.Count; i++) {
                AtlasGroup group = groups[i];
                if (group.buffer.isEmpty()) {
                    continue;
                }
                gl.BindTexture(TextureTarget.Texture2D, group.texture);
                drawCalls += group.batch.uploadAndDraw(group.buffer);
            }

            gl.Disable(EnableCap.Blend);
            gl.Enable(EnableCap.CullFace);
            gl.Enable(EnableCap.DepthTest);
            gl.BindVertexArray(0);
            gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        /**
        * Indexed rather than foreach: enumerating an IReadOnlyList through its
        * interface allocates an enumerator every frame, which is the
        * per-object allocation #218 rules out.
        */
        private void build(IReadOnlyList<LabelItem> labels, Matrix4x4 view, Matrix4x4 projection) {
            Vector3 right = LabelBillboard.getRight(view);
            Vector3 up = LabelBillboard.getUp(view);

            currentRight = right;
            currentUp = up;
            currentWorldUnitsPerPixel = LineHeightInWorldUnits / font.LineHeight;

            for (int i = 0; i < labels.Count; i++) {
                LabelItem label = labels[i];
                if (string.IsNullOrEmpty(label.text)) {
                    continue;
                }

                float alpha;
                if (!culling.tryAccept(label.position, view, projection, out alpha)) {
                    labelsCulled++;
                    continue;
                }

                Vector2 measured = font.MeasureString(label.text);
                currentPenOffset = LabelBillboard.getCenteringOffset(measured);
                currentAnchor = label.position + new Vector3(0f, HeightAboveEntity, 0f);

                FSColor tint = new FSColor(color.X, color.Y, color.Z, color.W * alpha);
                font.DrawText(this, label.text, Vector2.Zero, tint);
                labelsDrawn++;
            }
        }

        // --- IFontStashRenderer2 ---

        public ITexture2DManager TextureManager {
            get { return textureManager; }
        }

        /**
        * One glyph, handed over in pen space: x runs right along the line and y
        * runs down it. Every corner is lifted onto the camera's right and up
        * axes here, which is what makes the quad face the camera without the
        * shader having to know anything about billboarding -- and therefore
        * what lets every glyph of every label share one buffer.
        */
        public void DrawQuad(
                object texture,
                ref VertexPositionColorTexture topLeft,
                ref VertexPositionColorTexture topRight,
                ref VertexPositionColorTexture bottomLeft,
                ref VertexPositionColorTexture bottomRight) {
            if (counting) {
                countedQuads++;
                return;
            }

            LabelQuadBuffer buffer = groupFor((uint) texture).buffer;

            LabelVertex a = toVertex(ref topLeft);
            LabelVertex b = toVertex(ref topRight);
            LabelVertex c = toVertex(ref bottomRight);
            LabelVertex d = toVertex(ref bottomLeft);
            buffer.addQuad(ref a, ref b, ref c, ref d);
            quadsDrawn++;
        }

        private LabelVertex toVertex(ref VertexPositionColorTexture source) {
            LabelVertex vertex;
            vertex.position = LabelBillboard.toWorld(
                currentAnchor,
                currentRight,
                currentUp,
                source.Position.X + currentPenOffset.X,
                source.Position.Y + currentPenOffset.Y,
                currentWorldUnitsPerPixel);
            vertex.texCoord = source.TextureCoordinate;
            vertex.color = new Vector4(
                source.Color.R / 255f,
                source.Color.G / 255f,
                source.Color.B / 255f,
                source.Color.A / 255f);
            return vertex;
        }

        private AtlasGroup groupFor(uint texture) {
            for (int i = 0; i < groups.Count; i++) {
                if (groups[i].texture == texture) {
                    return groups[i];
                }
            }
            AtlasGroup group = new AtlasGroup(texture, new LabelBatch(gl));
            groups.Add(group);
            if (groups.Count > 1) {
                Log.info("a second label atlas appeared, so labels now cost " + groups.Count + " draw calls");
            }
            return group;
        }

        // --- startup ---

        /**
        * Rasterises and packs every printable ASCII glyph before the first
        * frame, and reports how many actually produced ink.
        *
        * FontStashSharp packs lazily, so without this the atlas would grow
        * during rendering: the first frame a pawn was named would upload
        * glyphs mid-draw, and a full atlas would silently start a second
        * texture and split the labels across two draw calls. Counting the
        * quads each character emits is also the only honest way to say how many
        * glyphs the atlas holds -- a space produces none.
        */
        private int prewarmAtlas() {
            counting = true;
            int packed = 0;
            FSColor white = new FSColor(1f, 1f, 1f, 1f);
            for (int codepoint = PrewarmFirstCodepoint; codepoint <= PrewarmLastCodepoint; codepoint++) {
                countedQuads = 0;
                font.DrawText(this, ((char) codepoint).ToString(), Vector2.Zero, white);
                if (countedQuads > 0) {
                    packed++;
                }
            }
            counting = false;
            return packed;
        }

        /**
        * The font is bundled rather than looked up on the machine, because the
        * host cannot assume any particular face is installed and Unity's
        * built-in LegacyRuntime.ttf is gone with Unity. See the licence beside
        * it in assets/fonts.
        */
        private static byte[] readFont() {
            string path = Path.Combine(AppContext.BaseDirectory, "assets", "fonts", "DejaVuSans.ttf");
            if (!File.Exists(path)) {
                Log.error("the bundled font was not found at " + path);
                return null;
            }
            return File.ReadAllBytes(path);
        }

        public void Dispose() {
            for (int i = 0; i < groups.Count; i++) {
                groups[i].batch.Dispose();
            }
            groups.Clear();
            shader.Dispose();
            textureManager.Dispose();
            if (fontSystem != null) {
                fontSystem.Dispose();
                fontSystem = null;
            }
            font = null;
            ready = false;
        }

        private class AtlasGroup {
            public readonly uint texture;
            public readonly LabelQuadBuffer buffer = new LabelQuadBuffer();
            public readonly LabelBatch batch;

            public AtlasGroup(uint texture, LabelBatch batch) {
                this.texture = texture;
                this.batch = batch;
            }
        }
    }
}
