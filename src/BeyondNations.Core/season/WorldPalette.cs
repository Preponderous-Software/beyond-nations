namespace beyondnations {

    /**
    * The colours the seasons and regions give the world, as data the
    * snapshot applies (WorldSnapshot.setPalette): ground tinted by its region
    * and then by the season -- fresh in spring, golden in autumn, snowy in
    * winter -- and leaves that turn and fall under snow. Nothing an entity or
    * location stores is changed; the palette only recolours what is drawn,
    * so it never touches the simulation or a save.
    *
    * Colours change gradually: over the last quarter of each season they
    * blend into the next season's.
    */
    public class WorldPalette {
        private readonly SeasonCalendar calendar;
        private readonly RegionMap regionMap;

        /** The fraction of a season over which it blends into the next. */
        public const float BlendFraction = 0.25f;

        /** The leaf colour trees and saplings are given in code (AppleTree, Sapling): pure green. */
        public static readonly Rgba DefaultLeaf = Rgba.Green;

        // worked out once a frame by beginFrame
        private Rgba seasonTint;
        private float seasonAmount;
        private Rgba leaf;

        public WorldPalette(SeasonCalendar calendar, RegionMap regionMap) {
            this.calendar = calendar;
            this.regionMap = regionMap;
            beginFrame();
        }

        public RegionMap getRegionMap() { return regionMap; }

        /** Reads the season once, so every colour in a frame agrees. */
        public void beginFrame() {
            Season season = calendar.getSeason();
            Season next = SeasonCalendar.next(season);
            float progress = calendar.getSeasonProgress();
            float blend = progress <= 1f - BlendFraction ? 0f : (progress - (1f - BlendFraction)) / BlendFraction;
            Rgba nowTint = groundTint(season);
            float nowAmount = groundTintAmount(season);
            Rgba nextTint = groundTint(next);
            float nextAmount = groundTintAmount(next);
            // blend the amounts and the colours separately, so summer (no tint) fades in and out cleanly
            seasonAmount = lerp(nowAmount, nextAmount, blend);
            float weightNext = nowAmount + nextAmount <= 0f ? 0f : (nextAmount * blend) / (nowAmount * (1f - blend) + nextAmount * blend + 1e-6f);
            seasonTint = mix(nowTint, nextTint, weightNext);
            leaf = mix(leafColour(season), leafColour(next), blend);
        }

        /** The colour to draw a ground tile of this chunk whose stored colour is the one given. */
        public Rgba groundColour(Region region, Rgba stored) {
            // stored ground is (0, v, 0) with v random: keep that as light and shade
            float shade = 0.8f + 0.4f * stored.g;
            Rgba baseColour = RegionCatalog.groundColour(region);
            Rgba shaded = new Rgba(clamp(baseColour.r * shade), clamp(baseColour.g * shade), clamp(baseColour.b * shade), stored.a);
            float amount = region == Region.HIGHLANDS && seasonAmount > 0.5f ? System.MathF.Min(1f, seasonAmount + 0.1f) : seasonAmount;
            return mix(shaded, seasonTint, amount);
        }

        /** The colour a part should be drawn in: leaves follow the season, everything else is as stored. */
        public Rgba partColour(EntityType type, Rgba stored) {
            // compared field by field: Rgba.Equals(object) would box, once a part, every frame (#218)
            if ((type == EntityType.TREE || type == EntityType.SAPLING) && stored.r == 0f && stored.g == 1f && stored.b == 0f) {
                return leaf;
            }
            return stored;
        }

        // the seasons' colours

        public static Rgba groundTint(Season season) {
            switch (season) {
                case Season.SPRING: return new Rgba(0.55f, 0.88f, 0.35f);
                case Season.AUTUMN: return new Rgba(0.74f, 0.54f, 0.18f);
                case Season.WINTER: return new Rgba(0.84f, 0.87f, 0.92f);
                default: return new Rgba(0.4f, 0.7f, 0.2f);
            }
        }

        public static float groundTintAmount(Season season) {
            switch (season) {
                case Season.SPRING: return 0.15f;
                case Season.AUTUMN: return 0.45f;
                case Season.WINTER: return 0.65f;
                default: return 0f;
            }
        }

        public static Rgba leafColour(Season season) {
            switch (season) {
                case Season.SPRING: return new Rgba(0.45f, 0.9f, 0.45f);
                case Season.AUTUMN: return new Rgba(0.9f, 0.48f, 0.1f);
                case Season.WINTER: return new Rgba(0.9f, 0.93f, 0.97f);
                default: return DefaultLeaf;
            }
        }

        private static Rgba mix(Rgba a, Rgba b, float t) {
            return new Rgba(lerp(a.r, b.r, t), lerp(a.g, b.g, t), lerp(a.b, b.b, t), a.a);
        }

        private static float lerp(float a, float b, float t) {
            return a + (b - a) * t;
        }

        private static float clamp(float v) {
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }
    }
}
