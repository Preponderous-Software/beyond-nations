using System.Collections.Generic;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.screens;

namespace beyondnations.desktop.ui.journal {

    /** The Journal's five pages, in the order their tabs sit. */
    public enum JournalTab {
        Goals,
        Chronicle,
        Era,
        Inventory,
        Nation
    }

    /** How a tile reads at a glance: reached, being worked on, still ahead, or needing attention. */
    public enum TileState {
        Plain,
        Done,
        Current,
        Locked,
        Warning,
        Danger
    }

    /**
    * One tile on a Journal page: a picture, a short label, a big number (or
    * none) and how far along it is; the words (the title and its lines)
    * show only when it is tapped or chosen.
    */
    public sealed class JournalTile {
        public readonly string id;
        public readonly JournalIcon icon;
        public readonly string label;
        /** The big number or short value ("12", "3/10"); null for none. */
        public readonly string value;
        /** 0 to 1 for a progress ring; negative for none. */
        public readonly float progress;
        public readonly TileState state;
        public readonly string detailTitle;
        public readonly List<string> detailLines;

        public JournalTile(string id, JournalIcon icon, string label, string value, float progress, TileState state,
                string detailTitle, List<string> detailLines) {
            this.id = id;
            this.icon = icon;
            this.label = label;
            this.value = value;
            this.progress = progress;
            this.state = state;
            this.detailTitle = detailTitle;
            this.detailLines = detailLines ?? new List<string>();
        }

        public bool hasProgress() {
            return progress >= 0f;
        }

        public override string ToString() {
            return id + " [" + state + "] " + label + (value != null ? " " + value : "");
        }
    }

    /** The tabs: their order, names and pictures, and turning between them. */
    public static class JournalTabs {
        public static readonly JournalTab[] All = {
            JournalTab.Goals, JournalTab.Chronicle, JournalTab.Era, JournalTab.Inventory, JournalTab.Nation
        };

        public static string label(JournalTab tab) {
            switch (tab) {
                case JournalTab.Chronicle: return "Chronicle";
                case JournalTab.Era: return "Era";
                case JournalTab.Inventory: return "Inventory";
                case JournalTab.Nation: return "Nation";
                default: return "Goals";
            }
        }

        /** A shorter name, for a tab too narrow for the full one. */
        public static string shortLabel(JournalTab tab) {
            switch (tab) {
                case JournalTab.Chronicle: return "News";
                case JournalTab.Inventory: return "Bag";
                default: return label(tab);
            }
        }

        public static JournalIcon icon(JournalTab tab) {
            switch (tab) {
                case JournalTab.Chronicle: return JournalIcon.Scroll;
                case JournalTab.Era: return JournalIcon.Hourglass;
                case JournalTab.Inventory: return JournalIcon.Bag;
                case JournalTab.Nation: return JournalIcon.Flag;
                default: return JournalIcon.Target;
            }
        }

        /** The tab after this one, wrapping round. */
        public static JournalTab next(JournalTab tab) {
            return All[(indexOf(tab) + 1) % All.Length];
        }

        /** The tab before this one, wrapping round. */
        public static JournalTab previous(JournalTab tab) {
            return All[(indexOf(tab) + All.Length - 1) % All.Length];
        }

        public static int indexOf(JournalTab tab) {
            return System.Array.IndexOf(All, tab);
        }

        /** "goals", "chronicle", "era", "inventory" or "nation" (and "news", "bag"), any case; null otherwise. */
        public static JournalTab? parse(string name) {
            if (name == null) {
                return null;
            }
            switch (name.Trim().ToLowerInvariant()) {
                case "goals": return JournalTab.Goals;
                case "chronicle":
                case "news": return JournalTab.Chronicle;
                case "era": return JournalTab.Era;
                case "inventory":
                case "bag": return JournalTab.Inventory;
                case "nation": return JournalTab.Nation;
            }
            return null;
        }

        /**
        * "era" or "era:2" (the second tile's card open), as --stage-journal
        * and ?stage-journal= take it. False for anything else.
        */
        public static bool tryParseStage(string text, out JournalTab tab, out int card) {
            tab = JournalTab.Goals;
            card = 0;
            if (string.IsNullOrEmpty(text)) {
                return false;
            }
            string name = text;
            int colon = text.IndexOf(':');
            if (colon >= 0) {
                name = text.Substring(0, colon);
                if (!int.TryParse(text.Substring(colon + 1), System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out card) || card < 1) {
                    card = 0;
                    return false;
                }
            }
            JournalTab? parsed = parse(name);
            if (parsed == null) {
                card = 0;
                return false;
            }
            tab = parsed.Value;
            return true;
        }

        /** The names --stage-journal and ?stage-journal= take. */
        public static readonly string[] Names = { "goals", "chronicle", "era", "inventory", "nation" };
    }

    /**
    * What each Journal page shows, as tiles, read from the same places the
    * HUD's boxes and the other screens read: the objective tracker
    * (ObjectiveTrackerBox), the chronicle (ChroniclePanel), the era and the
    * season (EraBox, SeasonInfoBox), the inventory (InventoryInfoBox) and
    * the nation (NationStanding, NationScreen). Nothing here works anything
    * out the game does not already; it only picks pictures and numbers.
    * Needs no ImGui frame, so the tests read it from a staged world.
    */
    public static class JournalContent {

        /** The most chronicle entries the page lists; the chronicle itself keeps 200. */
        public const int ChronicleTiles = 60;

        public static List<JournalTile> tilesFor(JournalTab tab, Simulation simulation, ChronicleScope scope = ChronicleScope.YourNews) {
            switch (tab) {
                case JournalTab.Chronicle: return chronicle(simulation, scope);
                case JournalTab.Era: return era(simulation);
                case JournalTab.Inventory: return inventory(simulation);
                case JournalTab.Nation: return nation(simulation);
                default: return goals(simulation);
            }
        }

        // --- Goals ---------------------------------------------------------------

        /** How many of the first arc's goals are done, and how many it has, for the vine over the Goals page. */
        public static void arcProgress(Simulation simulation, out int done, out int total) {
            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            total = 0;
            foreach (Objective objective in tracker.getObjectives()) {
                if (!objective.isAmbition()) {
                    total++;
                }
            }
            done = System.Math.Min(tracker.getCompletedCount(), total);
        }

        public static List<JournalTile> goals(Simulation simulation) {
            List<JournalTile> tiles = new List<JournalTile>();
            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            IReadOnlyList<Objective> objectives = tracker.getObjectives();
            int completed = tracker.getCompletedCount();
            // the one being worked on first, then the rest in order
            Objective current = tracker.getCurrent();
            if (current != null) {
                ObjectiveProgress progress = tracker.getCurrentProgress();
                List<string> lines = new List<string>();
                if (progress.hasCounter()) {
                    lines.Add(progress.describe());
                }
                string hint = ObjectiveTrackerBox.hint(simulation);
                if (!string.IsNullOrEmpty(hint)) {
                    lines.Add(hint);
                }
                if (!current.getReward().isEmpty()) {
                    lines.Add("Reward: " + current.getReward().describe());
                }
                tiles.Add(new JournalTile("goal-" + current.getId(), iconForGoal(current), shortGoalLabel(current),
                    counterValue(progress), progress.fraction(), TileState.Current,
                    ObjectiveTrackerBox.heading(tracker) + ": " + current.getTitle(), lines));
            }
            for (int i = 0; i < objectives.Count; i++) {
                Objective objective = objectives[i];
                if (objective == current) {
                    continue;
                }
                bool done = i < completed;
                List<string> lines = new List<string>();
                lines.Add(done ? "Done." : "Still ahead.");
                if (!objective.getReward().isEmpty()) {
                    lines.Add((done ? "Earned: " : "Reward: ") + objective.getReward().describe());
                }
                tiles.Add(new JournalTile("goal-" + objective.getId(), iconForGoal(objective), shortGoalLabel(objective),
                    null, -1f, done ? TileState.Done : TileState.Locked,
                    (objective.isAmbition() ? "Ambition: " : "Goal: ") + objective.getTitle(), lines));
            }
            return tiles;
        }

        /** "12/40" from a counter; null without one. */
        public static string counterValue(ObjectiveProgress progress) {
            if (!progress.hasCounter() || progress.getTarget() <= 0) {
                return null;
            }
            return System.Math.Min(System.Math.Max(0, progress.getCurrent()), progress.getTarget()) + "/" + progress.getTarget();
        }

        /** A goal's picture, from its title: wood, food, a nation, a settlement, a stall, coin, people, an era. */
        public static JournalIcon iconForGoal(Objective objective) {
            return goalKind(objective, out string ignored);
        }

        /** A goal's word on its tile: one short word for what it is about; the title shows on tap. */
        public static string shortGoalLabel(Objective objective) {
            goalKind(objective, out string label);
            return label;
        }

        private static JournalIcon goalKind(Objective objective, out string label) {
            string title = objective.getTitle().ToLowerInvariant();
            if (title.Contains("wood")) { label = "Wood"; return JournalIcon.Log; }
            if (title.StartsWith("eat")) { label = "Food"; return JournalIcon.Apple; }
            if (title.Contains("nation") && !title.Contains("grow")) { label = "Nation"; return JournalIcon.Flag; }
            if (title.StartsWith("enter")) { label = "Home"; return JournalIcon.Tent; }
            if (title.Contains("stall")) { label = "Stall"; return JournalIcon.Stall; }
            if (title.StartsWith("sell")) { label = "Market"; return JournalIcon.Coin; }
            if (title.Contains("coins")) { label = "Coins"; return JournalIcon.Coin; }
            if (title.Contains("second settlement")) { label = "2nd home"; return JournalIcon.House; }
            if (title.Contains("settlement")) { label = "Settle"; return JournalIcon.House; }
            if (title.StartsWith("grow")) { label = "People"; return JournalIcon.People; }
            if (title.Contains("buildings")) { label = "Build"; return JournalIcon.Hammer; }
            if (title.StartsWith("win")) { label = "Victory"; return JournalIcon.Crown; }
            if (title.Contains("era")) {
                foreach (Era era in new[] { Era.KINGDOM, Era.TOWN, Era.VILLAGE }) {
                    if (title.Contains(Eras.getDisplayName(era).ToLowerInvariant())) {
                        label = Eras.getDisplayName(era);
                        return iconForEra(era);
                    }
                }
                label = "Era";
                return JournalIcon.Hourglass;
            }
            label = "Goal";
            return JournalIcon.Star;
        }

        // --- Chronicle -------------------------------------------------------------

        public static List<JournalTile> chronicle(Simulation simulation, ChronicleScope scope) {
            List<JournalTile> tiles = new List<JournalTile>();
            Chronicle chronicle = simulation.getChronicle();
            if (chronicle == null) {
                return tiles;
            }
            ChronicleFocus focus = ChronicleFocus.of(simulation);
            List<ChronicleEntry> entries = ChroniclePanel.select(chronicle.getEntries(), simulation.getPlayer(), focus, scope, false);
            // newest first, as the strip reads
            for (int i = entries.Count - 1; i >= 0 && tiles.Count < ChronicleTiles; i--) {
                ChronicleEntry entry = entries[i];
                bool mine = focus.isPlayers(entry);
                tiles.Add(new JournalTile("chronicle-" + entry.getSequence(), iconForEntry(entry), labelForEntry(entry),
                    // among every nation's news, the player's own stand out; among their own, none need to
                    null, -1f, mine && scope == ChronicleScope.AllNations ? TileState.Current : TileState.Plain,
                    entry.getTimestamp(), new List<string> { entry.getText() }));
            }
            return tiles;
        }

        public static JournalIcon iconForEntry(ChronicleEntry entry) {
            switch (entry.getEventType()) {
                case EventType.NationCreation: return JournalIcon.Flag;
                case EventType.NationDisband: return JournalIcon.Flag;
                case EventType.NationJoin: return JournalIcon.People;
                case EventType.NationLeave: return JournalIcon.People;
                case EventType.PawnSpawn: return JournalIcon.Heart;
                case EventType.PawnDeath:
                case EventType.PlayerDeath: return JournalIcon.Skull;
                case EventType.PawnRelationshipIncrease: return JournalIcon.Heart;
                case EventType.NationRelationChange: return JournalIcon.Handshake;
                case EventType.NationRaid: return JournalIcon.Swords;
                case EventType.SettlementCapture: return JournalIcon.Swords;
                case EventType.SettlementFounded: return JournalIcon.House;
                case EventType.StallBuilt: return JournalIcon.Stall;
                case EventType.LeadershipChange: return JournalIcon.Crown;
                case EventType.Statecraft: return JournalIcon.Feather;
                case EventType.WorldEvent: return JournalIcon.Globe;
                case EventType.Caravan:
                case EventType.Trader: return JournalIcon.Cart;
                default: return JournalIcon.Scroll;
            }
        }

        /** One word for the kind of entry; the line and its date show on tap. */
        public static string labelForEntry(ChronicleEntry entry) {
            switch (entry.getEventType()) {
                case EventType.NationCreation: return "Founded";
                case EventType.NationDisband: return "Disbanded";
                case EventType.NationJoin: return "Joined";
                case EventType.NationLeave: return "Left";
                case EventType.PawnSpawn: return "Born";
                case EventType.PawnDeath:
                case EventType.PlayerDeath: return "Died";
                case EventType.PawnRelationshipIncrease: return "Friends";
                case EventType.NationRelationChange: return "Relations";
                case EventType.NationRaid: return "Raid";
                case EventType.SettlementCapture: return "Captured";
                case EventType.SettlementFounded: return "Settled";
                case EventType.StallBuilt: return "Stall";
                case EventType.LeadershipChange: return "New ruler";
                case EventType.Statecraft: return "Statecraft";
                case EventType.WorldEvent: return "Event";
                case EventType.Caravan: return "Caravan";
                case EventType.Trader: return "Trade";
                default: return "News";
            }
        }

        // --- Era -----------------------------------------------------------------

        public static List<JournalTile> era(Simulation simulation) {
            List<JournalTile> tiles = new List<JournalTile>();
            EraTracker eras = simulation.getEraTracker();
            Era now = eras.getEra();

            // what needs attention first: a collapse or a siege
            string collapse = EraBox.collapseWarning(simulation);
            if (collapse != null) {
                tiles.Add(new JournalTile("era-collapse", JournalIcon.Warning, "Collapsing", null, -1f, TileState.Danger,
                    collapse, new List<string> { "Your nation is falling apart. Grow it back before the time runs out." }));
            }
            string underWay = EraBox.siegeUnderWay(simulation);
            if (underWay != null) {
                tiles.Add(new JournalTile("era-siege", JournalIcon.Swords, "Besieged", null, -1f, TileState.Danger,
                    "Siege", new List<string> { underWay }));
            }
            else {
                string countdown = EraBox.siegeWarning(simulation);
                if (countdown != null) {
                    tiles.Add(new JournalTile("era-siege-warning", JournalIcon.Shield, "Sieges soon", null, -1f, TileState.Warning,
                        "Shelter ending", new List<string> { countdown }));
                }
            }

            // the ladder of eras: reached, now, ahead
            foreach (Era step in new[] { Era.CAMP, Era.VILLAGE, Era.TOWN, Era.KINGDOM }) {
                bool reached = step < now;
                bool current = step == now;
                List<string> lines = new List<string>();
                if (current) {
                    foreach (KeyValuePair<string, bool> line in EraBox.detailLines(simulation)) {
                        lines.Add(line.Key);
                    }
                    if (lines.Count == 0) {
                        lines.Add(EraBox.progressLabel(simulation));
                    }
                }
                else if (reached) {
                    int tick = eras.getReachedTick(step);
                    TimeOfDay time = simulation.getTimeOfDay();
                    lines.Add(tick >= 0
                        ? "Reached on day " + (TimeOfDay.dayAt(tick, time.getTicksPerDay(), time.getStartFraction()) + 1) + "."
                        : "Reached.");
                }
                else {
                    List<string> unlocks = EraRules.describeUnlocks(step);
                    lines.Add(unlocks.Count > 0 ? "Unlocks: " + string.Join(", ", unlocks) : "Still ahead.");
                }
                tiles.Add(new JournalTile("era-" + step.ToString().ToLowerInvariant(), iconForEra(step), Eras.getDisplayName(step),
                    current ? Eras.number(step) + "/" + Eras.count() : null,
                    current ? EraBox.progressFraction(simulation) : -1f,
                    current ? TileState.Current : reached ? TileState.Done : TileState.Locked,
                    "The " + Eras.getDisplayName(step) + " era", lines));
            }

            // what the next era asks, each with a ring; the calendar wait among them
            Era? next = Eras.next(now);
            foreach (EraRequirement requirement in eras.getRequirements(simulation)) {
                List<string> lines = new List<string> { requirement.describe() };
                if (requirement.label == "Day" && !requirement.isMet()) {
                    int wait = requirement.target - requirement.current;
                    lines.Add("The calendar: the " + (next.HasValue ? Eras.getDisplayName(next.Value) : "next") + " era waits for day "
                        + requirement.target + ", " + wait + (wait == 1 ? " more day." : " more days."));
                }
                string hint = ObjectiveKeyNames.resolve(EraRules.hintFor(requirement));
                if (!string.IsNullOrEmpty(hint)) {
                    lines.Add(hint);
                }
                tiles.Add(new JournalTile("need-" + requirement.label.ToLowerInvariant(), iconForRequirement(requirement.label),
                    requirement.label, System.Math.Min(requirement.current, requirement.target) + "/" + requirement.target,
                    requirement.target <= 0 ? 1f : System.Math.Clamp((float) requirement.current / requirement.target, 0f, 1f),
                    requirement.isMet() ? TileState.Done : TileState.Plain,
                    "To reach the " + (next.HasValue ? Eras.getDisplayName(next.Value) : "next") + " era", lines));
            }

            // the season and winter, the other wait on the calendar
            SeasonCalendar calendar = simulation.getSeasonCalendar();
            if (calendar != null) {
                List<string> lines = new List<string> { SeasonInfoBox.getSeasonLine(calendar) };
                string winter = SeasonInfoBox.getWinterLine(calendar);
                if (winter != null) {
                    lines.Add(winter);
                }
                string stores = SeasonInfoBox.getStoresLine(calendar, simulation.getEntityRepository(), simulation.getPlayer());
                if (stores != null) {
                    lines.Add(stores);
                }
                bool warn = winter != null && !calendar.isWinter();
                tiles.Add(new JournalTile("season", iconForSeason(calendar.getSeason()), SeasonCalendar.name(calendar.getSeason()),
                    "Day " + EraTracker.dayOf(simulation), -1f, warn ? TileState.Warning : TileState.Plain,
                    "The season", lines));
            }

            // the victories, once they are within reach
            if (now >= VictoryRules.ConcordEra || now >= VictoryRules.DominionEra) {
                NationStanding standing = NationStanding.of(simulation);
                foreach (VictoryRules.Progress progress in new[] {
                        VictoryRules.dominionProgress(simulation, now, standing),
                        VictoryRules.prosperityProgress(now, standing),
                        VictoryRules.concordProgress(simulation, now, standing) }) {
                    tiles.Add(new JournalTile("victory-" + progress.outcome.ToString().ToLowerInvariant(), JournalIcon.Crown,
                        VictoryRules.getDisplayName(progress.outcome), null, -1f,
                        progress.met ? TileState.Done : progress.available ? TileState.Plain : TileState.Locked,
                        "Victory: " + VictoryRules.getDisplayName(progress.outcome), new List<string> { progress.text }));
                }
            }
            return tiles;
        }

        public static JournalIcon iconForEra(Era era) {
            switch (era) {
                case Era.VILLAGE: return JournalIcon.House;
                case Era.TOWN: return JournalIcon.Tower;
                case Era.KINGDOM: return JournalIcon.Castle;
                default: return JournalIcon.Tent;
            }
        }

        public static JournalIcon iconForRequirement(string label) {
            switch (label) {
                case "Village": return JournalIcon.House;
                case "Town":
                case "Towns": return JournalIcon.Tower;
                case "Members": return JournalIcon.People;
                case "Stalls": return JournalIcon.Stall;
                case "Settlements": return JournalIcon.Flag;
                case "Funds": return JournalIcon.Coin;
                case "Day": return JournalIcon.Calendar;
                default: return JournalIcon.Star;
            }
        }

        public static JournalIcon iconForSeason(Season season) {
            switch (season) {
                case Season.SPRING: return JournalIcon.Sapling;
                case Season.SUMMER: return JournalIcon.Sun;
                case Season.AUTUMN: return JournalIcon.Leaf;
                default: return JournalIcon.Snow;
            }
        }

        // --- Inventory -------------------------------------------------------------

        public static List<JournalTile> inventory(Simulation simulation) {
            List<JournalTile> tiles = new List<JournalTile>();
            Inventory inventory = simulation.getPlayer().getInventory();
            // the HUD box's items, in its order: the six always, the crafted ones once held
            foreach (ItemType itemType in InventoryInfoBox.AlwaysShown) {
                tiles.Add(itemTile(inventory, itemType));
            }
            foreach (ItemType itemType in InventoryInfoBox.ShownWhenHeld) {
                if (inventory.getNumItems(itemType) > 0) {
                    tiles.Add(itemTile(inventory, itemType));
                }
            }
            return tiles;
        }

        private static JournalTile itemTile(Inventory inventory, ItemType itemType) {
            int count = inventory.getNumItems(itemType);
            string name = ItemNames.getDisplayName(itemType);
            List<string> lines = new List<string> { name + ": " + count };
            float progress = -1f;
            if (Tools.isTool(itemType) && count > 0) {
                int left = Tools.getUsesLeft(inventory, itemType);
                int durability = Tools.getDurability(itemType);
                lines.Add(left + " uses left on the one in hand.");
                progress = durability > 0 ? System.Math.Clamp((float) left / durability, 0f, 1f) : -1f;
            }
            return new JournalTile("item-" + itemType.ToString().ToLowerInvariant(), iconForItem(itemType), name,
                count.ToString(), progress, count > 0 ? TileState.Plain : TileState.Locked, name, lines);
        }

        public static JournalIcon iconForItem(ItemType itemType) {
            switch (itemType) {
                case ItemType.COIN: return JournalIcon.Coin;
                case ItemType.WOOD: return JournalIcon.Log;
                case ItemType.STONE: return JournalIcon.Stone;
                case ItemType.APPLE: return JournalIcon.Apple;
                case ItemType.SAPLING: return JournalIcon.Sapling;
                case ItemType.CHICKEN_MEAT: return JournalIcon.Meat;
                case ItemType.COOKED_MEAT: return JournalIcon.CookedMeat;
                case ItemType.PLANKS: return JournalIcon.Planks;
                case ItemType.BRICKS: return JournalIcon.Bricks;
                case ItemType.AXE: return JournalIcon.Axe;
                case ItemType.PICKAXE: return JournalIcon.Pickaxe;
                default: return JournalIcon.Bag;
            }
        }

        // --- Nation --------------------------------------------------------------

        public static List<JournalTile> nation(Simulation simulation) {
            List<JournalTile> tiles = new List<JournalTile>();
            NationStanding standing = NationStanding.of(simulation);
            List<string> header = NationScreen.getHeaderLines(simulation);
            if (standing == null) {
                tiles.Add(new JournalTile("nation-none", JournalIcon.Flag, "No nation", null, -1f, TileState.Locked,
                    "No nation yet", header));
                tiles.Add(here(simulation));
                tiles.Add(otherNationsTile(simulation, null));
                return tiles;
            }
            Nation own = standing.nation;
            tiles.Add(new JournalTile("nation-name", JournalIcon.Flag, own.getName(),
                Eras.getDisplayName(simulation.getEraTracker().getEra()), -1f, TileState.Current, own.getName(), header));
            tiles.Add(new JournalTile("nation-ruler", standing.playerLeads ? JournalIcon.Crown : JournalIcon.Shield,
                standing.playerLeads ? "You lead" : "Member", null, -1f, TileState.Plain,
                standing.playerLeads ? "You lead " + own.getName() : "You serve " + own.getName(),
                new List<string> { standing.playerLeads ? "You speak for your nation in diplomacy and law." : "Only the leader speaks for the nation." }));
            tiles.Add(here(simulation));
            tiles.Add(new JournalTile("nation-members", JournalIcon.People, "Members", standing.members.ToString(), -1f, TileState.Plain,
                "Members", new List<string> { NationScreen.describeSize(simulation, own) + "." }));
            int limit = own.getSettlementLimit();
            tiles.Add(new JournalTile("nation-settlements", JournalIcon.House, "Settlements",
                limit > 0 ? standing.settlements + "/" + limit : standing.settlements.ToString(),
                limit > 0 ? System.Math.Clamp((float) standing.settlements / limit, 0f, 1f) : -1f, TileState.Plain,
                "Settlements", NationScreen.describeOwnSettlements(simulation, own, standing.playerLeads)));
            tiles.Add(new JournalTile("nation-buildings", JournalIcon.Hammer, "Buildings", standing.buildings.ToString(), -1f, TileState.Plain,
                "Buildings", new List<string> { standing.buildings + " buildings across your settlements; "
                    + standing.villages + (standing.villages == 1 ? " is a village or better" : " are villages or better")
                    + ", " + standing.towns + (standing.towns == 1 ? " a town." : " towns.") }));
            tiles.Add(new JournalTile("nation-stalls", JournalIcon.Stall, "Stalls", standing.stalls.ToString(), -1f, TileState.Plain,
                "Market stalls", new List<string> { standing.stalls + " market stalls in your settlements." }));
            tiles.Add(new JournalTile("nation-funds", JournalIcon.Coin, "Funds", standing.funds.ToString(), -1f, TileState.Plain,
                "Funds", new List<string> { "Your coins and your settlements' treasuries: " + standing.funds + "." }));

            int allies = 0;
            int wars = 0;
            List<string> alliedNames = new List<string>();
            List<string> warNames = new List<string>();
            foreach (NationScreen.Row row in NationScreen.getRows(simulation)) {
                if (row.allied) {
                    allies++;
                    alliedNames.Add(row.nation.getName());
                }
                if (row.atWar) {
                    wars++;
                    warNames.Add(row.nation.getName());
                }
            }
            tiles.Add(new JournalTile("nation-allies", JournalIcon.Handshake, "Allies", allies.ToString(), -1f, TileState.Plain,
                "Allies", new List<string> { allies == 0 ? "No allies yet." : string.Join(", ", alliedNames) + "." }));
            tiles.Add(new JournalTile("nation-wars", JournalIcon.Swords, "Wars", wars.ToString(), -1f,
                wars > 0 ? TileState.Danger : TileState.Plain,
                "At war", new List<string> { wars == 0 ? "At peace with everyone." : "At war with " + string.Join(", ", warNames) + "." }));
            tiles.Add(otherNationsTile(simulation, own));
            return tiles;
        }

        /**
        * Where the player stands, which the HUD's player and season boxes
        * used to say: whose land, the region underfoot and which way they
        * face (as a compass point, the way founding hints give directions).
        */
        public static JournalTile here(Simulation simulation) {
            Player player = simulation.getPlayer();
            EntityRepository entities = simulation.getEntityRepository();
            Settlement claim = Territory.findClaimAt(entities, player.getPosition());
            bool own = claim != null && player.getNationId() != null && claim.getNationId() == player.getNationId();
            Region region = simulation.getRegionMap().regionAt(player.getPosition());
            return hereTile(claim?.getNationName(), own,
                Territory.describeFor(player, entities, simulation.getNationRepository()),
                RegionCatalog.name(region), RegionCatalog.describe(region), Compass.facing(player.getForward()));
        }

        /** The "Here" tile from plain values: owner null for unclaimed land; facing a compass point ("north-east"). */
        public static JournalTile hereTile(string owner, bool own, string landDetail, string region, string regionNote, string facing) {
            string label = owner == null ? "Unclaimed" : (own ? "Your land" : owner);
            return new JournalTile("nation-here", JournalIcon.Compass, label, compassAbbreviation(facing), -1f,
                own ? TileState.Current : TileState.Plain, "Where you stand",
                new List<string> {
                    "Land: " + (owner == null ? "unclaimed wilds" : landDetail),
                    "Region: " + region + (string.IsNullOrEmpty(regionNote) ? "" : ", " + regionNote),
                    "Facing: " + facing,
                });
        }

        /** "north-east" as "NE"; "here" (no heading) as "-". */
        public static string compassAbbreviation(string facing) {
            if (string.IsNullOrEmpty(facing) || facing == "here") {
                return "-";
            }
            string result = "";
            foreach (string part in facing.Split('-')) {
                result += char.ToUpperInvariant(part[0]);
            }
            return result;
        }

        private static JournalTile otherNationsTile(Simulation simulation, Nation own) {
            int others = 0;
            foreach (Nation nation in simulation.getNationRepository().getNations()) {
                if (nation != own) {
                    others++;
                }
            }
            return new JournalTile("nation-others", JournalIcon.Globe, "Nations", others.ToString(), -1f, TileState.Plain,
                "Other nations", new List<string> { others == 0 ? NationScreen.NoOtherNations : others + (others == 1 ? " other nation in the world." : " other nations in the world.") });
        }
    }
}
