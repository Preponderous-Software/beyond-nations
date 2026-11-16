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
    *   Food: 9 of 60 for winter  (autumn and winter, for a member of a nation)
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
        * "Food: 9 of 20 for winter" in autumn and winter for a member of a
        * nation with pawns, and null otherwise: the food the nation's members
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
                return null;
            }
            return "Food: " + stores.Food + " of " + stores.Wanted + " for winter";
        }

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
            return WinterStores.cannotFeed(entityRepository, player.getNationId(), calendar.getSeason()) ? FamineLine : null;
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
        * what winter means while it lasts, and null otherwise.
        */
        public static string getWinterLine(SeasonCalendar calendar) {
            if (calendar.isWinter()) {
                return "Winter: no harvests";
            }
            int days = calendar.getDaysUntilWinter();
            if (days > WarnDaysAhead) {
                return null;
            }
            return "Winter in " + days + (days == 1 ? " day!" : " days");
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
