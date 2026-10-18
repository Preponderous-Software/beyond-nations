namespace beyondnations {

    /**
    * A colour, as four channels in the range 0 to 1.
    *
    * This replaces UnityEngine.Color in the simulation. Nations pick one, tiles
    * are randomly green, and entities carry one so the host knows what to tint
    * them. None of that needs a graphics context, so none of it belongs to the
    * engine.
    */
    public struct Rgba {
        public readonly float r;
        public readonly float g;
        public readonly float b;
        public readonly float a;

        public Rgba(float r, float g, float b) : this(r, g, b, 1f) {
        }

        public Rgba(float r, float g, float b, float a) {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Rgba White { get { return new Rgba(1f, 1f, 1f); } }
        public static Rgba Black { get { return new Rgba(0f, 0f, 0f); } }
        public static Rgba Red { get { return new Rgba(1f, 0f, 0f); } }
        public static Rgba Green { get { return new Rgba(0f, 1f, 0f); } }
        public static Rgba Blue { get { return new Rgba(0f, 0f, 1f); } }
        public static Rgba Gray { get { return new Rgba(0.5f, 0.5f, 0.5f); } }
        public static Rgba Yellow { get { return new Rgba(1f, 0.92f, 0.016f); } }
        public static Rgba Cyan { get { return new Rgba(0f, 1f, 1f); } }
        public static Rgba Magenta { get { return new Rgba(1f, 0f, 1f); } }

        public override string ToString() {
            return "Rgba(" + r + ", " + g + ", " + b + ", " + a + ")";
        }

        public override bool Equals(object obj) {
            if (!(obj is Rgba)) {
                return false;
            }
            Rgba other = (Rgba) obj;
            return r == other.r && g == other.g && b == other.b && a == other.a;
        }

        public override int GetHashCode() {
            return r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode() ^ a.GetHashCode();
        }
    }
}
