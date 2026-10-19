using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * One coloured primitive, positioned relative to the thing it belongs to.
    *
    * A tree is a cylinder trunk with a cube of leaves above it, so a single
    * shape per entity is not enough; each part carries its own offset, scale and
    * colour.
    */
    public class AppearancePart {
        private PrimitiveKind kind;
        private Vector3 offset;
        private Vector3 scale;
        private Rgba color;

        public AppearancePart(PrimitiveKind kind, Vector3 offset, Vector3 scale, Rgba color) {
            this.kind = kind;
            this.offset = offset;
            this.scale = scale;
            this.color = color;
        }

        public PrimitiveKind getKind() { return kind; }
        public Vector3 getOffset() { return offset; }
        public Vector3 getScale() { return scale; }
        public Rgba getColor() { return color; }

        public void setColor(Rgba color) { this.color = color; }
        public void setScale(Vector3 scale) { this.scale = scale; }
        public void setOffset(Vector3 offset) { this.offset = offset; }
    }

    /**
    * What something looks like, expressed purely as data.
    *
    * Before #213 every entity owned the GameObject that drew it, so creating an
    * entity required a graphics context and destroying one required the engine.
    * An entity now carries an Appearance instead: one or more shapes, and
    * optionally a label to float above it. The host reads that and decides what
    * to draw.
    *
    * Nothing here allocates a graphics resource, so entities can be created and
    * destroyed with no window open at all.
    */
    public class Appearance {
        private readonly List<AppearancePart> parts = new List<AppearancePart>();
        private string label;
        private Vector3 labelOffset = Vector3.Zero;
        private float labelScale = 1f;

        public Appearance() {
        }

        public Appearance(PrimitiveKind kind, Vector3 scale, Rgba color) {
            parts.Add(new AppearancePart(kind, Vector3.Zero, scale, color));
        }

        public Appearance addPart(PrimitiveKind kind, Vector3 offset, Vector3 scale, Rgba color) {
            parts.Add(new AppearancePart(kind, offset, scale, color));
            return this;
        }

        public IReadOnlyList<AppearancePart> getParts() {
            return parts;
        }

        /**
        * Drops every part, keeping the label. For something whose look is
        * rebuilt from its state -- a settlement redraws itself when a stall or
        * a building goes up -- rather than recoloured in place.
        */
        public void clearParts() {
            parts.Clear();
        }

        /**
        * The first part, which is the whole appearance for anything built from a
        * single primitive. Convenient for recolouring, which is all the
        * simulation ever does to an appearance after construction.
        */
        public AppearancePart getPrimaryPart() {
            return parts.Count > 0 ? parts[0] : null;
        }

        public void setColor(Rgba color) {
            AppearancePart primary = getPrimaryPart();
            if (primary != null) {
                primary.setColor(color);
            }
        }

        /**
        * The world-space label drawn above this thing, or null for no label.
        * Pawns and settlements carry their name here; everything else does not.
        */
        public string getLabel() {
            return label;
        }

        public void setLabel(string label) {
            this.label = label;
        }

        /**
        * Where the label hangs, relative to the thing's origin. Zero for
        * everything one primitive tall; a settlement lifts its nametag above
        * its banner so the name is not buried in the buildings.
        */
        public Vector3 getLabelOffset() {
            return labelOffset;
        }

        public void setLabelOffset(Vector3 labelOffset) {
            this.labelOffset = labelOffset;
        }

        /**
        * How large the label is drawn, relative to a pawn's nametag. A
        * settlement's name is larger, so it reads from across its own plaza.
        */
        public float getLabelScale() {
            return labelScale;
        }

        public void setLabelScale(float labelScale) {
            this.labelScale = labelScale;
        }

        public bool hasLabel() {
            return label != null && label.Length > 0;
        }
    }
}
