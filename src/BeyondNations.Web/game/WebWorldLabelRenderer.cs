using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using beyondnations;
using beyondnations.desktop.text;
using beyondnations.web.host;

namespace beyondnations.web {

    /**
    * WorldLabelRenderer (#222), ported to WebGL2: pawn nametags as
    * billboarded glyph quads out of one FontStashSharp atlas, drawn in one
    * call per atlas, with depth testing off so they read as an overlay.
    *
    * The constants, the culling (LabelCulling), the billboard maths
    * (LabelBillboard) and the quad buffer (LabelQuadBuffer) are the desktop's
    * own, compiled in; only the texture and the draw go to WebGL2 instead of
    * Silk.NET.OpenGL.
    */
    public unsafe class WebWorldLabelRenderer : IFontStashRenderer2, ITexture2DManager {
        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1024;
        private const int FontSizePixels = 32;
        private const float LineHeightInWorldUnits = 0.6f;
        private const float HeightAboveEntity = 1.4f;
        private const int PrewarmFirstCodepoint = 32;
        private const int PrewarmLastCodepoint = 126;

        private readonly LabelCulling culling = new LabelCulling();
        private readonly Dictionary<int, LabelQuadBuffer> buffers = new Dictionary<int, LabelQuadBuffer>();
        private readonly Dictionary<int, Point> sizes = new Dictionary<int, Point>();
        private readonly List<int> textures = new List<int>();
        private readonly float[] matrices = new float[32];

        private readonly FontSystem fontSystem;
        private readonly DynamicSpriteFont font;
        private readonly bool ready;

        private int labelsDrawn;
        private int labelsCulled;
        private int quadsDrawn;
        private int drawCalls;

        private Vector3 currentAnchor;
        private Vector3 currentRight;
        private Vector3 currentUp;
        private Vector2 currentPenOffset;
        private float currentWorldUnitsPerPixel;
        private bool counting;
        private int countedQuads;
        private readonly Vector4 color = new Vector4(1f, 1f, 1f, 1f);

        public WebWorldLabelRenderer() {
            byte[] fontData = readFont();
            if (fontData == null) {
                Log.error("world-space labels are disabled: the bundled font could not be read");
                return;
            }
            FontSystemSettings settings = new FontSystemSettings();
            settings.TextureWidth = AtlasWidth;
            settings.TextureHeight = AtlasHeight;
            settings.PremultiplyAlpha = false;
            fontSystem = new FontSystem(settings);
            fontSystem.AddFont(fontData);
            font = fontSystem.GetFont(FontSizePixels);
            int packed = prewarmAtlas();
            ready = true;
            Log.info("world-space labels ready: font DejaVu Sans at " + FontSizePixels + "px, atlas "
                + AtlasWidth + "x" + AtlasHeight + ", " + packed + " glyphs packed across " + textures.Count + " atlas texture(s)");
        }

        public bool isReady() { return ready; }
        public int getLabelsDrawn() { return labelsDrawn; }
        public int getLabelsCulled() { return labelsCulled; }
        public int getQuadsDrawn() { return quadsDrawn; }
        public int getDrawCallCount() { return drawCalls; }

        public void render(IReadOnlyList<LabelItem> labels, Matrix4x4 view, Matrix4x4 projection) {
            labelsDrawn = 0;
            labelsCulled = 0;
            quadsDrawn = 0;
            drawCalls = 0;
            if (!ready || labels == null || labels.Count == 0) {
                return;
            }
            foreach (LabelQuadBuffer buffer in buffers.Values) {
                buffer.clear();
            }

            currentRight = LabelBillboard.getRight(view);
            currentUp = LabelBillboard.getUp(view);

            for (int i = 0; i < labels.Count; i++) {
                LabelItem label = labels[i];
                if (string.IsNullOrEmpty(label.text)) {
                    continue;
                }
                if (!culling.tryAccept(label.position, view, projection, out float alpha)) {
                    labelsCulled++;
                    continue;
                }
                currentWorldUnitsPerPixel = culling.getLineHeight(LineHeightInWorldUnits, label.position, view, projection) / font.LineHeight;
                Vector2 measured = font.MeasureString(label.text);
                currentPenOffset = LabelBillboard.getCenteringOffset(measured);
                // a label may ask to be drawn larger (a settlement's name); 0 means normal size
                float labelScale = label.scale > 0f ? label.scale : 1f;
                currentWorldUnitsPerPixel = LineHeightInWorldUnits * labelScale / font.LineHeight;
                currentAnchor = label.position + new Vector3(0f, HeightAboveEntity, 0f);
                font.DrawText(this, label.text, Vector2.Zero, new FSColor(color.X, color.Y, color.Z, color.W * alpha));
                labelsDrawn++;
            }

            if (quadsDrawn == 0) {
                return;
            }
            write(view, 0);
            write(projection, 16);
            foreach (KeyValuePair<int, LabelQuadBuffer> entry in buffers) {
                LabelQuadBuffer buffer = entry.Value;
                if (buffer.isEmpty()) {
                    continue;
                }
                fixed (float* m = matrices)
                fixed (LabelVertex* v = buffer.getVertices())
                fixed (uint* idx = buffer.getIndices()) {
                    WebGpu.drawLabels((int) m, entry.Key, (int) v, buffer.getVertexCount(), (int) idx, buffer.getIndexCount());
                }
                drawCalls++;
            }
        }

        // --- IFontStashRenderer2 ---

        public ITexture2DManager TextureManager { get { return this; } }

        public void DrawQuad(object texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
                ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight) {
            if (counting) {
                countedQuads++;
                return;
            }
            int handle = (int) texture;
            if (!buffers.TryGetValue(handle, out LabelQuadBuffer buffer)) {
                buffer = new LabelQuadBuffer();
                buffers[handle] = buffer;
            }
            LabelVertex a = toVertex(ref topLeft);
            LabelVertex b = toVertex(ref topRight);
            LabelVertex c = toVertex(ref bottomRight);
            LabelVertex d = toVertex(ref bottomLeft);
            buffer.addQuad(ref a, ref b, ref c, ref d);
            quadsDrawn++;
        }

        private LabelVertex toVertex(ref VertexPositionColorTexture source) {
            LabelVertex vertex;
            vertex.position = LabelBillboard.toWorld(currentAnchor, currentRight, currentUp,
                source.Position.X + currentPenOffset.X, source.Position.Y + currentPenOffset.Y, currentWorldUnitsPerPixel);
            vertex.texCoord = source.TextureCoordinate;
            vertex.color = new Vector4(source.Color.R / 255f, source.Color.G / 255f, source.Color.B / 255f, source.Color.A / 255f);
            return vertex;
        }

        // --- ITexture2DManager ---

        public object CreateTexture(int width, int height) {
            int texture = WebGpu.createTexture(width, height, true);
            textures.Add(texture);
            sizes[texture] = new Point(width, height);
            return texture;
        }

        public Point GetTextureSize(object texture) {
            return sizes[(int) texture];
        }

        public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
            fixed (byte* pixels = data) {
                WebGpu.updateTexture((int) texture, bounds.X, bounds.Y, bounds.Width, bounds.Height, (int) pixels, data.Length);
            }
        }

        // --- startup ---

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

        private static byte[] readFont() {
            using (Stream stream = typeof(WebWorldLabelRenderer).Assembly.GetManifestResourceStream("DejaVuSans.ttf")) {
                if (stream == null) {
                    return null;
                }
                using (MemoryStream memory = new MemoryStream()) {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        private void write(Matrix4x4 m, int at) {
            matrices[at + 0] = m.M11; matrices[at + 1] = m.M12; matrices[at + 2] = m.M13; matrices[at + 3] = m.M14;
            matrices[at + 4] = m.M21; matrices[at + 5] = m.M22; matrices[at + 6] = m.M23; matrices[at + 7] = m.M24;
            matrices[at + 8] = m.M31; matrices[at + 9] = m.M32; matrices[at + 10] = m.M33; matrices[at + 11] = m.M34;
            matrices[at + 12] = m.M41; matrices[at + 13] = m.M42; matrices[at + 14] = m.M43; matrices[at + 15] = m.M44;
        }
    }
}
