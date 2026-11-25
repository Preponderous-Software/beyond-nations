using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The season, how long until winter, and the region the player stands in,
    * just above the player box in the HUD's right column. Read-only, so it works the same by
    * touch: nothing to hover or press.
    *
    *   Autumn - day 2/2, year 1
    *   Winter in 1 day!          (amber; in winter, blue: "Winter: no harvests")
    *   Food: 9 of 84 for winter  (autumn, for a member of a nation; in winter "Food: 9 of 42 to spring")
    *   Food: no members to feed yet. ...  (the same, for a nation with no members yet, #500)
    *   Too little food: no births or newcomers  (amber, while the nation cannot feed its members)
    *   Region: Forest
    *   plenty of wood, little stone
    */
    public class SeasonInfoBox : InfoBox {
        private static readonly Vector4 WarningColour = new Vector4(1f, 0.75f, 0.3f, 1f);
        private static readonly Vector4 WinterColour = new Vector4(0.7f, 0.85f, 1f, 1f);

        /** How many days ahead the HUD starts warning that winter is coming. */
        public const int WarnDaysAhead = 2;

        private readonly SeasonCalendar calendar;
        private readonly RegionMap regionMap;
        private readonly Player player;

        private readonly EntityRepository entityRepository;

        public SeasonInfoBox(Simulation simulation) : base("Season") {
            calendar = simulation.getSeasonCalendar();
            regionMap = simulation.getRegionMap();
            player = simulation.getPlayer();
            entityRepository = simulation.getEntityRepository();
        }

        /**
        * "Food: 9 of 84 for winter, +3 rotting" in autumn ("Food: 9 of 42 to spring" in winter: what the rest of it needs) for a member of a
        * nation with pawns, NoMembersLine for a nation with none yet, and null otherwise: the food the nation's members
        * carry against the WINTER_FOOD_STORE each wants (WinterStores, #353).
        * Before it, no screen showed a nation's food at all.
        */
        public static string getStoresLine(SeasonCalendar calendar, EntityRepository entityRepository, Player player) {
            Season season = calendar.getSeason();
            if ((season != Season.AUTUMN && season != Season.WINTER) || player.getNationId() == null) {
                return null;
            }
            WinterStores.Count stores = WinterStores.count(entityRepository, player.getNationId());
            if (stores.Members == 0) {
                // a ruler with no members yet saw no food line at all, and
                // nothing said that members are who need it or how to get
                // one (#500)
                return NoMembersLine;
            }
            // apples beyond what a member keeps rot within minutes, so they are not stores (#495 playtest);
            // they are on top of the count, not part of it: "17 of 14, 5 rotting" read as more
            // rotting than the 3 over the target (playtest w2)
            // (kept short: the HUD chip cuts the line off past about 36 characters)
            string rotting = stores.Spoiling > 0 ? ", +" + stores.Spoiling + " rotting" : "";
            if (season == Season.WINTER) {
                // in winter the target is what the rest of winter needs, which falls as it is eaten (playtest w2f)
                return "Food: " + stores.Food + " of " + stores.wantedFor(season, calendar.getSeasonProgress()) + " to spring" + rotting;
            }
            return "Food: " + stores.Food + " of " + stores.Wanted + " for winter" + rotting;
        }

        /**
        * The stores line for a nation with no members yet: nobody to feed,
        * and how a first member is won. Keys are not named, so it reads the
        * same by touch.
        */
        public const string NoMembersLine = "Food: no members yet. Win people over with talk and gifts, then invite them.";

        /** The line shown while the player's nation cannot feed its members (WinterStores.cannotFeed, #489). */
        public const string FamineLine = "Too little food: no births or newcomers";

        /**
        * FamineLine while the player's nation cannot feed its members, so
        * that no child is born and no newcomer joins it, and null otherwise:
        * the playtest's ruler watched four mouths arrive at "Food: 0" with
        * nothing to say why or how to stop it.
        */
        public static string getFamineLine(SeasonCalendar calendar, EntityRepository entityRepository, Player player) {
            if (player.getNationId() == null) {
                return null;
            }
            return WinterStores.cannotFeed(entityRepository, player.getNationId(), calendar) ? FamineLine : null;
        }

        /** The collapsed chip: "Autumn 2/2", and the winter warning when there is one. */
        public override string getChipText() {
            return getChipLine(calendar);
        }

        /** "Autumn 2/2", or "Autumn 2/2 - Winter in 5 hours!" */
        public static string getChipLine(SeasonCalendar calendar) {
            string line = SeasonCalendar.name(calendar.getSeason()) + " " + (calendar.getDayOfSeason() + 1) + "/" + SeasonCalendar.DAYS_PER_SEASON;
            string winter = getWinterLine(calendar);
            return winter == null ? line : line + " - " + winter;
        }

        /** Winter coming, or a nation that cannot feed its members, marks the chip. */
        public override HudBadge getBadge() {
            return badgeFor(getWinterLine(calendar) != null && !calendar.isWinter(), getFamineLine(calendar, entityRepository, player) != null);
        }

        public static HudBadge badgeFor(bool winterComing, bool famine) {
            return winterComing || famine ? HudBadge.Warning : HudBadge.None;
        }

        public override List<string> getLines() {
            List<string> lines = new List<string>(4);
            lines.Add(getSeasonLine(calendar));
            string warning = getWinterLine(calendar);
            if (warning != null) {
                lines.Add(warning);
            }
            string stores = getStoresLine(calendar, entityRepository, player);
            if (stores != null) {
                lines.Add(stores);
            }
            string famine = getFamineLine(calendar, entityRepository, player);
            if (famine != null) {
                lines.Add(famine);
            }
            Region region = regionMap.regionAt(player.getPosition());
            if (isCompact()) {
                // the season and the winter warning stay; the region lines go
                return lines;
            }
            lines.Add("Region: " + RegionCatalog.name(region));
            lines.Add(RegionCatalog.describe(region));
            return lines;
        }

        /** "Autumn - day 2/2, year 1". */
        public static string getSeasonLine(SeasonCalendar calendar) {
            return SeasonCalendar.name(calendar.getSeason()) + " - day " + (calendar.getDayOfSeason() + 1) + "/" + SeasonCalendar.DAYS_PER_SEASON
                + ", year " + (calendar.getYear() + 1);
        }

        /**
        * "Winter in N days" once winter is WarnDaysAhead days away or nearer,
        * "Winter in N hours!" through the last day before it, what winter
        * means while it lasts, and null otherwise.
        */
        public static string getWinterLine(SeasonCalendar calendar) {
            if (calendar.isWinter()) {
                return "Winter: no harvests";
            }
            int days = calendar.getDaysUntilWinter();
            if (days > WarnDaysAhead) {
                return null;
            }
            if (days == 1) {
                // --- #501 ---
                // the last day counts down in hours: at 0.8 of it the box still
                // read "Winter in 1 day!" with winter a few game hours away
                int hours = hoursLeftInDay(calendar.getTimeOfDay().getFraction());
                if (hours < 24) {
                    return "Winter in " + hours + (hours == 1 ? " hour!" : " hours!");
                }
            }
            return "Winter in " + days + (days == 1 ? " day!" : " days");
        }

        /** Game hours (24 to a day) left before midnight, rounded up: 24 at midnight, 1 in the last hour. */
        public static int hoursLeftInDay(float fraction) {
            return System.Math.Clamp((int) System.MathF.Ceiling((1f - fraction) * 24f), 1, 24);
        }

        /**
        * Each line wrapped to the box, which on a phone is only a third of
        * the screen wide; the winter line in colour.
        */
        protected override void drawBody() {
            List<string> lines = getLines();
            float width = System.MathF.Max(40f, ImGui.GetContentRegionAvail().X);
            bool hasWarning = getWinterLine(calendar) != null;
            for (int i = 0; i < lines.Count; i++) {
                foreach (string part in TextWrap.wrap(lines[i], width, text => ImGui.CalcTextSize(text).X)) {
                    if (hasWarning && i == 1) {
                        Overlay.textColoured(calendar.isWinter() ? WinterColour : WarningColour, part);
                    }
                    else if (lines[i] == FamineLine) {
                        Overlay.textColoured(WarningColour, part);
                    }
                    else {
                        ImGui.TextUnformatted(part);
                    }
                }
            }
        }
    }
}
