using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.ui.boxes;

namespace beyondnations.desktop.ui.screens {

    /**
    * One settlement, on a screen of its own (#178): whose it is -- the
    * nation, and the member who holds it (#176) -- its people, its
    * buildings, its stores and its market, and what the player can do
    * with it.
    *
    * It opens when the player enters a settlement (SettlementScreens), from
    * the Settlement button on the command bar while inside one, and from
    * the Manage tab of the nations screen. Exit Settlement on it leaves the
    * settlement and goes back to the world; Back goes back to wherever it
    * was opened from, leaving the player where they stand. Like the other
    * panels it is laid over the world, which waits while it is open.
    *
    * The leader of the settlement's nation can give it to a member (pick
    * one with the arrows, then Give) and take it back; a member who holds
    * one can hand it back. Prev and Next step through the nation's other
    * settlements. Every button is big enough for a thumb, and no text
    * names a key.
    *
    * Standing inside one of their own, the player can build from the
    * screen itself (getBuildCommands: the command bar's Build buttons; the
    * build key works here too), and the two ways off it say what they do:
    * Exit Settlement walks out into the world, Stay Inside closes the
    * screen and leaves the player in the settlement with its buttons
    * (playtest 0.8.0 s01: "press B inside your settlement" did nothing
    * here, and Back versus Exit Settlement was never explained).
    *
    * The words come from getSections() and getActions(), which need no
    * window, so they are asserted in unit tests and only the drawing needs
    * a screen.
    */
    public class SettlementScreen {
        public const string BackLabel = "Back";
        /** Back, when the player is inside the settlement shown: they stay in it. */
        public const string StayInsideLabel = "Stay Inside";
        /** Take Back, when the player holds it personally: it goes back to being held as leader. */
        public const string HoldAsLeaderLabel = "Hold as Leader";
        /** Under the buttons while the player is inside, in place of the close hint. */
        public const string InsideHint = "Exit Settlement walks you out; Stay Inside keeps you here.";
        /** Over the Build buttons. */
        public const string BuildNote = "Build or feed residents with what you carry:";
        public const string ExitLabel = "Exit Settlement";
        public const string GiveLabel = "Give";
        public const string TakeBackLabel = "Take Back";
        public const string HandBackLabel = "Hand Back";
        public const string CloseHint = "(Press {Pause} to go back)";
        public const string NoSettlement = "There is no settlement to show.";

        private static readonly Vector4 HeadingColour = new Vector4(0.95f, 0.75f, 0.30f, 1f);

        // which settlement to show; set when the screen is asked for
        private static EntityId shownId;
        private static bool openRequested;
        // the member the Give button would give the settlement to, by index into getActions().candidates
        private int pick;
        private EntityId pickFor;
        private int firstLine;
        private float dragCarry;

        /** Asks the host to open the screen on this settlement (SettlementScreens picks it up before the next frame). */
        public static void requestOpen(EntityId settlementId) {
            shownId = settlementId;
            openRequested = true;
        }

        /** True once after requestOpen. */
        public static bool consumeOpenRequest() {
            bool requested = openRequested;
            openRequested = false;
            return requested;
        }

        public static EntityId getShownId() {
            return shownId;
        }

        /**
        * The settlement to show: the one asked for, while it stands;
        * otherwise the one the player is in, the nearest of their nation's,
        * or their home. Null when there is none.
        */
        public static Settlement resolve(Simulation simulation, EntityId wanted) {
            EntityRepository entities = simulation.getEntityRepository();
            Player player = simulation.getPlayer();
            Settlement settlement = standing(entities, wanted)
                ?? standing(entities, player.getCurrentSettlementId());
            if (settlement != null) {
                return settlement;
            }
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            if (own != null) {
                float best = float.MaxValue;
                foreach (Settlement candidate in simulation.getDiplomacy().getSettlements(own)) {
                    float distance = Territory.groundDistance(player.getPosition(), candidate.getPosition());
                    if (distance < best) {
                        best = distance;
                        settlement = candidate;
                    }
                }
            }
            return settlement ?? standing(entities, player.getHomeSettlementId());
        }

        private static Settlement standing(EntityRepository entities, EntityId id) {
            Settlement settlement = id == null ? null : entities.getEntity(id) as Settlement;
            return settlement == null || settlement.isMarkedForDeletion() ? null : settlement;
        }

        /** The settlements Prev and Next step through: the player's nation's, when this is one of them; otherwise this one alone. */
        public static List<Settlement> getBrowsable(Simulation simulation, Settlement settlement) {
            Nation own = simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());
            if (own != null && settlement.getNationId() == own.getId()) {
                return simulation.getDiplomacy().getSettlements(own);
            }
            return new List<Settlement> { settlement };
        }

        /** A heading and the lines under it. */
        public class Section {
            public readonly string heading;
            public readonly List<string> lines;

            public Section(string heading, List<string> lines) {
                this.heading = heading;
                this.lines = lines;
            }
        }

        /** "VILLAGE OF BLACKBURG", with "(2)" when the nation has two of that kind. */
        public static string getTitle(Simulation simulation, Settlement settlement) {
            Nation nation = simulation.getNationRepository().getNation(settlement.getNationId());
            string name = nation != null ? MembersPanel.describeSettlement(simulation, nation, settlement.getId()) : settlement.getDisplayName();
            return name.ToUpperInvariant();
        }

        /** Who it belongs to, its people and buildings, its stores and its market. */
        public static List<Section> getSections(Simulation simulation, Settlement settlement) {
            List<Section> sections = new List<Section>();
            sections.Add(new Section("Ownership", describeOwnership(simulation, settlement)));
            sections.Add(new Section("People", describePeople(simulation, settlement)));
            sections.Add(new Section("Buildings", describeBuildings(settlement, simulation.getPlayer().getInventory())));
            Nation own = simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());
            bool ours = own != null && settlement.getNationId() == own.getId();
            sections.Add(new Section("Stores", ours ? describeStores(settlement)
                : new List<string> { "Only its own people see what " + settlement.getNationName() + " keeps here." }));
            sections.Add(new Section("Market", describeMarket(simulation, settlement)));
            return sections;
        }

        /**
        * "Nation: Blackburg (your nation)." and "Held by: you, as its
        * leader." -- or by the member it was given to, or a foreign leader.
        */
        public static List<string> describeOwnership(Simulation simulation, Settlement settlement) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            NationRepository nations = simulation.getNationRepository();
            Nation nation = nations.getNation(settlement.getNationId());
            Nation own = nations.getNation(player.getNationId());
            if (nation == null) {
                lines.Add("Nation: none. It has been abandoned.");
                return lines;
            }
            string standing;
            if (nation == own) {
                standing = "your nation";
            } else if (own == null) {
                standing = "you belong to no nation";
            } else {
                standing = nations.getRelations().describe(own, nation) + (simulation.getRealm().isAtWar(own, nation) ? ", at war with you" : "");
            }
            lines.Add("Nation: " + nation.getName() + " (" + standing + ").");

            EntityId holder = SettlementOwnership.holderOf(settlement, nation);
            bool granted = SettlementOwnership.isGranted(settlement, nation);
            string who = holder == player.getId() ? "you" : nameOf(simulation, holder);
            if (granted) {
                lines.Add("Held by: " + who + ", given it by the leader of " + nation.getName() + ".");
            } else if (settlement.getOwnerId() != null && holder == settlement.getOwnerId()) {
                lines.Add("Held by: " + who + ", as leader, and kept if the rule passes on.");
            } else {
                lines.Add("Held by: " + who + ", as leader of " + nation.getName() + ".");
            }
            if (settlement.getSiegeText() != null) {
                lines.Add(settlement.getSiegeText() + "!");
            }
            return lines;
        }

        private static string nameOf(Simulation simulation, EntityId id) {
            Entity entity = id == null ? null : simulation.getEntityRepository().getEntity(id);
            return entity == null ? "no one" : entity.getName();
        }

        /**
        * "Residents: 6 of 8; 3 here now." -- counting the player when they
        * are inside, so it never reads "0 here now" beside "You are inside
        * it" (playtest 0.8.0 s01, s07) -- and where it lies and whether it
        * is home. Home is where the player lives, which is not who holds it
        * (s07: "held by Mia Johnson ... your home" read as a contradiction).
        */
        public static List<string> describePeople(Simulation simulation, Settlement settlement) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            bool inside = player.getCurrentSettlementId() == settlement.getId();
            int here = settlement.getCurrentlyPresentEntitiesCount()
                + (inside && !settlement.getCurrentlyPresentEntities().Contains(player.getId()) ? 1 : 0);
            lines.Add("Residents: " + settlement.getResidentCount() + " of " + settlement.getResidentCap() + "; " + here + " here now"
                + (inside ? (here == 1 ? ", you alone." : ", you among them.") : "."));
            string where;
            if (inside) {
                where = "You are inside it";
            } else {
                int distance = (int) Territory.groundDistance(player.getPosition(), settlement.getPosition());
                where = distance + " paces " + Compass.describe(player.getPosition(), settlement.getPosition());
            }
            string homeText = ".";
            if (settlement.getId() == player.getHomeSettlementId()) {
                Nation nation = simulation.getNationRepository().getNation(settlement.getNationId());
                EntityId holder = nation == null ? null : SettlementOwnership.holderOf(settlement, nation);
                homeText = holder != null && holder != player.getId()
                    ? ". You live here, though " + nameOf(simulation, holder) + " holds it."
                    : ". You live here: it is your home.";
            }
            lines.Add(where + homeText);
            return lines;
        }

        /**
        * The command bar's Build buttons (WorldCommandBar, CommandGroup.Build),
        * offered on the screen while the player stands inside this
        * settlement: Build Stall and a button per building it has room for,
        * disabled with the reason while the player cannot pay. Empty
        * anywhere else, or where the player may not build.
        */
        public static List<WorldCommand> getBuildCommands(Simulation simulation, Settlement settlement) {
            return getBuildCommands(simulation, settlement, false);
        }

        /**
        * The same, and with payWithCoins the building buttons buy instead
        * (getBuyCommand): "Buy House (1,980)", paying coins for the wood and
        * stone the player is short of. Either way the last button is the
        * Pay with Coins / Pay with Materials toggle (PayToggleLabel), while
        * any building has room here.
        */
        public static List<WorldCommand> getBuildCommands(Simulation simulation, Settlement settlement, bool payWithCoins) {
            List<WorldCommand> build = new List<WorldCommand>();
            if (settlement == null || simulation.getPlayer().getCurrentSettlementId() != settlement.getId()) {
                return build;
            }
            bool anyBuilding = false;
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                // and Feed Residents, which the hunger and winter banners name: the
                // screen covers the command bar, and in the winter2 playtest a ruler
                // inside the camp with 22 apples and a starving member found only
                // Build and Exit here (#497 follow-up)
                if (command.getGroup() == CommandGroup.Build || isFeedResidents(command)) {
                    BuildingType? type = buildingOf(command);
                    if (type != null) {
                        anyBuilding = true;
                        if (payWithCoins) {
                            build.Add(getBuyCommand(simulation, settlement, type.Value));
                            continue;
                        }
                    }
                    build.Add(command);
                }
            }
            // upgrades are bought with coins too: with the toggle on Pay with Coins,
            // and on their own once there is nothing left to build here
            if (payWithCoins || !anyBuilding) {
                build.AddRange(getUpgradeCommands(simulation, settlement));
            }
            if (anyBuilding) {
                build.Add(new WorldCommand(payToggleLabel(payWithCoins), CommandGroup.Build, () => SettlementScreen.payWithCoins = !payWithCoins)
                    .setTooltip(payWithCoins ? PayWithMaterialsTooltip : PayWithCoinsTooltip));
            }
            return build;
        }

        /** "Upgrade House (1,500)": the coins for the next level of the lowest house here. */
        public static string upgradeLabel(BuildingType type, int price) {
            return "Upgrade " + BuildingCatalog.getDisplayName(type) + " (" + BuildingPurchase.formatCoins(price) + ")";
        }

        /** Whether this is an Upgrade button (getUpgradeCommands), which asks for a second tap as Buy does. */
        public static bool isUpgrade(WorldCommand command) {
            return command.getLabel().StartsWith("Upgrade ");
        }

        /** Whether this button spends coins on a building and so asks for a second tap: Buy or Upgrade. */
        public static bool isPurchase(WorldCommand command) {
            return isBuy(command) || isUpgrade(command);
        }

        /**
        * The Upgrade buttons (BuildingUpgrades, UpgradeBuildingCommand): one
        * for each type the settlement has a building of that can still go up
        * a level, greyed with the reason while the purse falls short or the
        * level waits for the Kingdom era. None before the Town era, outside
        * the player's own nation, or once every building is fully upgraded.
        */
        public static List<WorldCommand> getUpgradeCommands(Simulation simulation, Settlement settlement) {
            List<WorldCommand> upgrades = new List<WorldCommand>();
            Player player = simulation.getPlayer();
            if (settlement == null || player.getNationId() == null || settlement.getNationId() != player.getNationId()
                    || player.getCurrentSettlementId() != settlement.getId() || !BuildingUpgrades.areUpgradesUnlocked(settlement)) {
                return upgrades;
            }
            foreach (BuildingType type in WorldCommandBar.BuildOrder) {
                Building next = BuildingUpgrades.isUpgradable(type) ? BuildingUpgrades.findNext(settlement, type) : null;
                if (next == null) {
                    continue;
                }
                int level = next.getLevel() + 1;
                BuildingType chosen = type;
                string whyNot = BuildingUpgrades.describeWhyNot(settlement, player.getInventory(), type);
                upgrades.Add(new WorldCommand(upgradeLabel(type, BuildingUpgrades.getPrice(type, level)), CommandGroup.Build, () =>
                    new UpgradeBuildingCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player, chosen))
                    .setEnabled(whyNot == null, whyNot)
                    // what it does, after why not while greyed (a hover shows the tooltip in place of the reason)
                    .setTooltip((whyNot == null ? "" : whyNot + " ") + "Level " + level + ": " + BuildingUpgrades.describeEffect(type, level) + ".")
                    .requireConfirm());
            }
            return upgrades;
        }

        /** The toggle that turns the building buttons from Build into Buy and back. */
        public const string PayWithCoinsLabel = "Pay with Coins";
        public const string PayWithMaterialsLabel = "Pay with Materials";
        public const string PayWithCoinsTooltip = "Buy buildings with coins: they pay for the wood and stone you are short of.";
        public const string PayWithMaterialsTooltip = "Build with the wood and stone you carry and the stockpile.";

        /** Whether the screen's building buttons buy with coins (the toggle). Remembered while the game runs. */
        private static bool payWithCoins;

        public static bool isPayingWithCoins() {
            return payWithCoins;
        }

        public static void setPayingWithCoins(bool value) {
            payWithCoins = value;
        }

        public static string payToggleLabel(bool payWithCoins) {
            return payWithCoins ? PayWithMaterialsLabel : PayWithCoinsLabel;
        }

        /** Whether this is the screen's Pay with Coins / Pay with Materials toggle. */
        public static bool isPayToggle(WorldCommand command) {
            return command.getLabel() == PayWithCoinsLabel || command.getLabel() == PayWithMaterialsLabel;
        }

        /** Whether this is a Buy button (getBuyCommand), which asks for a second tap. */
        public static bool isBuy(WorldCommand command) {
            return command.getLabel().StartsWith("Buy ");
        }

        /** The building a command bar "Build House" button builds, or null for any other button. */
        private static BuildingType? buildingOf(WorldCommand command) {
            foreach (BuildingType type in WorldCommandBar.BuildOrder) {
                if (command.getLabel() == WorldCommandBar.buildLabel(type)) {
                    return type;
                }
            }
            return null;
        }

        /** "Buy House (1,980)": the coins for what the player and the stockpile are short of. */
        public static string buyLabel(BuildingType type, int price) {
            return "Buy " + BuildingCatalog.getDisplayName(type) + " (" + BuildingPurchase.formatCoins(price) + ")";
        }

        /**
        * Buys a building with coins (BuyBuildingCommand), greyed with the
        * reason while the purse falls short or the materials already cover it.
        */
        public static WorldCommand getBuyCommand(Simulation simulation, Settlement settlement, BuildingType type) {
            Player player = simulation.getPlayer();
            Inventory carried = player.getInventory();
            int price = BuildingPurchase.getCoinPrice(settlement, carried, type);
            string whyNot = BuildingPurchase.describeWhyNot(settlement, carried, type);
            return new WorldCommand(buyLabel(type, price), CommandGroup.Build, () =>
                new BuyBuildingCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player, type))
                .setEnabled(whyNot == null, whyNot)
                .setTooltip(whyNot == null ? "Pays " + BuildingPurchase.describeShortfall(settlement, carried, type) + "." : null)
                .requireConfirm();
        }

        /** "Buy with coins (you have 5,210):" */
        public static string buyNote(Player player) {
            return "Buy with coins (you have " + coinsOf(player) + "):";
        }

        private static string coinsOf(Player player) {
            return BuildingPurchase.formatCoins(player.getInventory().getNumItems(ItemType.COIN));
        }

        /** What an armed Buy button says: "Buy House (1,980)" becomes "Confirm: 1,980 coins". */
        public static string confirmLabel(string buyLabel) {
            int open = buyLabel.LastIndexOf('(');
            if (open < 0 || !buyLabel.EndsWith(")")) {
                return "Confirm";
            }
            return "Confirm: " + buyLabel.Substring(open + 1, buyLabel.Length - open - 2) + " coins";
        }

        /** How long an armed Buy button waits for its second tap. */
        public const double BuyArmSeconds = 4.0;

        /** The Buy button waiting for its second tap, and since when. */
        private string armedBuy;
        private double armedAt;

        /** Whether this is the command bar's Feed Residents button. */
        public static bool isFeedResidents(WorldCommand command) {
            return command.getLabel().StartsWith("Feed Residents");
        }

        /** The build-stall key on this screen does what it does in the world (the objective's hint names it). */
        public static void buildStallKey(Simulation simulation) {
            new BuildStallCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                .execute(simulation.getPlayer());
        }

        private static readonly beyondnations.desktop.input.BuildKey buildKey = new beyondnations.desktop.input.BuildKey();
        private static readonly System.Diagnostics.Stopwatch keyClock = System.Diagnostics.Stopwatch.StartNew();

        /**
        * The keys that act on this screen, as in the world: the build-stall
        * key, and the build key (twice, as in the world). The hosts call it
        * while the screen is open.
        */
        public static void readKeys(Simulation simulation, beyondnations.desktop.input.InputService input) {
            if (simulation == null) {
                return;
            }
            double now = keyClock.Elapsed.TotalSeconds;
            buildKey.update(simulation.getPlayer(), now);
            if (input.wasPressedThisFrame(beyondnations.desktop.input.KeyBindings.BuildStall)) {
                buildStallKey(simulation);
            }
            if (input.wasPressedThisFrame(beyondnations.desktop.input.KeyBindings.BuildBuilding)) {
                buildKey.press(simulation, now);
            }
        }

        /** Exit Settlement on the screen: out into the world. */
        public static void exit(Simulation simulation) {
            new ExitSettlementCommand(simulation.getEntityRepository()).execute(simulation.getPlayer());
        }

        /** "Town, 5 of 8 buildings: 2 houses, 2 farms, 1 storehouse." and what comes next. */
        public static List<string> describeBuildings(Settlement settlement) {
            return describeBuildings(settlement, null);
        }

        /**
        * describeBuildings, with the next building's price for what the
        * player is short of beside its full price when they differ: the
        * text read "or 2,160 coins" while the button read "Buy Farm (1,764)"
        * and nothing said why (playtest w1): "or 2,160 coins (1,764 with
        * what you have)". carried is the player's pack, or null.
        */
        public static List<string> describeBuildings(Settlement settlement, Inventory carried) {
            List<string> lines = new List<string>();
            string tier = SettlementTiers.getDisplayName(settlement.getTier());
            int count = settlement.getNumBuildings();
            if (count == 0) {
                lines.Add(tier + ", no buildings yet.");
            } else {
                List<string> parts = new List<string>();
                foreach (BuildingType type in System.Enum.GetValues<BuildingType>()) {
                    int n = settlement.getNumBuildings(type);
                    if (n > 0) {
                        string name = BuildingCatalog.getDisplayName(type).ToLowerInvariant();
                        parts.Add(n + " " + name + (n == 1 ? "" : "s"));
                    }
                }
                lines.Add(tier + ", " + count + " of " + BuildingCatalog.MAX_BUILDINGS + " buildings: " + string.Join(", ", parts) + ".");
            }
            BuildingType? next = BuildingPlanner.suggestNext(settlement);
            if (next != null) {
                int full = BuildingPurchase.getFullCoinPrice(settlement, next.Value);
                int yours = carried == null ? full : BuildingPurchase.getCoinPrice(settlement, carried, next.Value);
                string withWhatYouHave = yours > 0 && yours < full ? " (" + BuildingPurchase.formatCoins(yours) + " with what you have)" : "";
                lines.Add("Next: " + BuildingCatalog.getDisplayName(next.Value) + ", " + BuildingCatalog.describeCost(next.Value)
                    + ", or " + BuildingPurchase.formatCoins(full) + " coins" + withWhatYouHave + ".");
            }
            lines.AddRange(describeUpgrades(settlement));
            return lines;
        }

        /**
        * What the next upgrade of each type does and costs: "Upgrade a House
        * to level 1: room for 6 residents (was 4), 1,500 coins." -- the
        * button's hover says it too, but a phone has no hover. Before the
        * Town era, once the settlement is full: "Upgrades come with the
        * Town era." Nothing for a settlement with no upgradable building.
        */
        public static List<string> describeUpgrades(Settlement settlement) {
            List<string> lines = new List<string>();
            bool anyUpgradable = false;
            foreach (BuildingType type in System.Enum.GetValues<BuildingType>()) {
                if (BuildingUpgrades.isUpgradable(type) && settlement.getNumBuildings(type) > 0) {
                    anyUpgradable = true;
                }
            }
            if (!anyUpgradable) {
                return lines;
            }
            if (!BuildingUpgrades.areUpgradesUnlocked(settlement)) {
                if (BuildingPlanner.suggestNext(settlement) == null) {
                    lines.Add("Upgrades come with the " + Eras.getDisplayName(BuildingUpgrades.eraFor(1)) + " era.");
                }
                return lines;
            }
            int levels = settlement.getNumUpgrades();
            if (levels > 0) {
                lines.Add("Upgrades bought here: " + levels + ".");
            }
            foreach (BuildingType type in System.Enum.GetValues<BuildingType>()) {
                Building next = BuildingUpgrades.isUpgradable(type) ? BuildingUpgrades.findNext(settlement, type) : null;
                if (next == null) {
                    continue;
                }
                int level = next.getLevel() + 1;
                lines.Add("Upgrade a " + BuildingCatalog.getDisplayName(type) + " to level " + level + ": "
                    + BuildingUpgrades.describeEffect(type, level) + ", " + BuildingPurchase.formatCoins(BuildingUpgrades.getPrice(type, level)) + " coins"
                    + (BuildingUpgrades.isLevelUnlocked(settlement, level) ? "." : ", in the " + Eras.getDisplayName(BuildingUpgrades.eraFor(level)) + " era."));
            }
            return lines;
        }

        /** "Funds: 30 coins." and "Stockpile: 40 wood, 25 stone." */
        public static List<string> describeStores(Settlement settlement) {
            List<string> lines = new List<string>();
            Inventory inventory = settlement.getInventory();
            lines.Add("Funds: " + inventory.getNumItems(ItemType.COIN) + " coins.");
            List<string> goods = new List<string>();
            foreach (ItemType type in System.Enum.GetValues<ItemType>()) {
                int n = type == ItemType.COIN ? 0 : inventory.getNumItems(type);
                if (n > 0) {
                    goods.Add(ItemNames.describeQuantity(type, n));
                }
            }
            lines.Add(goods.Count == 0 ? "Stockpile: empty." : "Stockpile: " + string.Join(", ", goods) + ".");
            return lines;
        }

        /** "4 of 4 stalls, 1 for sale; merchants: Ann, Bob +1." and the staple prices. */
        public static List<string> describeMarket(Simulation simulation, Settlement settlement) {
            List<string> lines = new List<string>();
            Market market = settlement.getMarket();
            List<string> merchants = new List<string>();
            foreach (Stall stall in market.getStalls()) {
                if (stall.getOwnerId() != null) {
                    merchants.Add(stall.getOwnerId() == simulation.getPlayer().getId() ? "you" : nameOf(simulation, stall.getOwnerId()));
                }
            }
            string stalls = market.getNumStalls() + " of " + market.getMaxNumStalls() + " stalls, " + market.getNumStallsForSale() + " for sale";
            if (merchants.Count == 0) {
                lines.Add(stalls + "; no merchants.");
            } else {
                int shown = System.Math.Min(3, merchants.Count);
                lines.Add(stalls + "; merchants: " + string.Join(", ", merchants.GetRange(0, shown))
                    + (merchants.Count > shown ? " +" + (merchants.Count - shown) : "") + ".");
            }
            MarketInfoBox prices = new MarketInfoBox(market, simulation.getEntityRepository());
            List<string> priced = new List<string>();
            foreach (ItemType type in prices.getListedGoods()) {
                priced.Add(prices.describePrice(type));
            }
            lines.Add("Prices (buy/sell): " + string.Join(", ", priced) + ".");
            return lines;
        }

        /** What the player can do with the settlement, and the members it can be given to. */
        public class Actions {
            /** The player leads its nation: Give and, when given away, Take Back. */
            public readonly bool canGive;
            public readonly bool canTakeBack;
            /** The player holds it as a member, not as leader: Hand Back. */
            public readonly bool canHandBack;
            /** The player is inside it: Exit Settlement. */
            public readonly bool canExit;
            /** Who Give can give it to: the player first, then the members oldest first; never the one holding it. */
            public readonly List<Entity> candidates;
            /** A line saying what the player can and cannot do here. */
            public readonly string note;

            public Actions(bool canGive, bool canTakeBack, bool canHandBack, bool canExit, List<Entity> candidates, string note) {
                this.canGive = canGive;
                this.canTakeBack = canTakeBack;
                this.canHandBack = canHandBack;
                this.canExit = canExit;
                this.candidates = candidates;
                this.note = note;
            }
        }

        public static Actions getActions(Simulation simulation, Settlement settlement) {
            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            bool ours = own != null && settlement.getNationId() == own.getId();
            bool leads = ours && own.getLeaderId() == player.getId();
            bool inside = player.getCurrentSettlementId() == settlement.getId();
            List<Entity> candidates = new List<Entity>();
            string note;
            if (leads) {
                if (settlement.getOwnerId() != player.getId()) {
                    candidates.Add(player);
                }
                foreach (Pawn pawn in TransferLeadershipCommand.candidates(own, simulation.getEntityRepository())) {
                    if (pawn.getId() != settlement.getOwnerId()) {
                        candidates.Add(pawn);
                    }
                }
                note = SettlementOwnership.isGranted(settlement, own)
                    ? "Its holder manages its funds, stockpile and stalls. Take it back to manage them yourself."
                    : "Give it to one of your people and its funds, stockpile and stalls are theirs to manage. Give it to yourself to keep it if the rule passes on.";
            } else if (ours && settlement.getOwnerId() == player.getId()) {
                note = "It is yours to manage: its funds, stockpile and stalls. Hand it back to give it to the leader.";
            } else if (ours) {
                note = "The leader of " + own.getName() + " decides who holds it.";
            } else {
                note = "It is not your nation's.";
            }
            bool granted = ours && settlement.getOwnerId() != null;
            // Giving away the nation's only settlement is no first step: the
            // first camp opened on "Give to: you" (playtest 0.8.0 s01). It is
            // offered once there is a second, or while this one is given away
            // (so it can be given on or taken back), and to someone besides
            // the player.
            bool someoneElse = candidates.Exists(c => c != player);
            bool offerGive = leads && someoneElse && (granted || own.getNumberOfSettlements() > 1);
            if (leads && !offerGive && !granted) {
                note = "You hold it as leader. With a second settlement, you can give one to a member.";
            }
            return new Actions(offerGive, leads && granted, !leads && ours && settlement.getOwnerId() == player.getId(),
                inside, candidates, note);
        }

        /**
        * "Give to: Ada Smith", or "Keep it yourself": giving it to the
        * player keeps it theirs if the rule passes on, which looked like
        * Take Back when it read "Give to: you" (playtest 0.8.0 s07).
        */
        public static string describePick(Entity candidate, Player player) {
            return candidate == player ? "Keep it yourself" : "Give to: " + candidate.getName();
        }

        /** Take Back, or Hold as Leader when the player holds it personally: either way it is the leader's again. */
        public static string takeBackLabel(Simulation simulation, Settlement settlement) {
            return settlement.getOwnerId() == simulation.getPlayer().getId() ? HoldAsLeaderLabel : TakeBackLabel;
        }

        /** Index i of a list of count, wrapped round both ends. */
        public static int wrap(int i, int count) {
            return count <= 0 ? 0 : ((i % count) + count) % count;
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            Player player = simulation.getPlayer();
            Settlement settlement = resolve(simulation, shownId);
            float lineHeight = System.MathF.Max(UiScale.px(26f), height / 22f);
            float buttonHeight = System.MathF.Max(lineHeight - 4f, UiScale.px(TouchLayout.isActive() ? TouchLayout.MinTarget : 30f));
            float gap = UiScale.px(8f);
            float left = System.MathF.Max(width * 0.04f, UiScale.px(16f));
            float y = System.MathF.Max(System.MathF.Max(height / 18f, UiScale.px(12f)), Overlay.touchCloseBottom());

            Overlay.beginFullScreen("##settlement-screen", width, height);
            // dimmed and darker again: the world's floating labels and the well showed through the text (playtest 0.8.0 s07)
            Overlay.panelBackground(width, height);
            UiAction action = UiAction.None;
            System.Action pending = null;

            if (settlement == null) {
                Overlay.textCentered(NoSettlement, height / 30f, height / 2f, width);
                if (Overlay.buttonAt(BackLabel + "##settlement-back", (width - UiScale.px(160f)) / 2f, height / 2f + lineHeight, UiScale.px(160f), buttonHeight)) {
                    action = UiAction.CloseSettlement;
                }
                Overlay.end();
                return Overlay.touchCloseButton("settlement", width) ? UiAction.CloseSettlement : action;
            }
            if (settlement.getId() != shownId) {
                shownId = settlement.getId();
            }

            bool narrow = width < UiScale.px(600f);
            // the title, with Prev and Next either side of it when the nation has more than one
            List<Settlement> browsable = getBrowsable(simulation, settlement);
            int index = System.Math.Max(0, browsable.IndexOf(settlement));
            float arrowWidth = System.MathF.Min(UiScale.px(90f), width / 5f);
            if (browsable.Count > 1) {
                if (Overlay.buttonAt("Prev##settlement-prev", left, y, arrowWidth, buttonHeight)) {
                    shownId = browsable[wrap(index - 1, browsable.Count)].getId();
                    firstLine = 0;
                }
                if (Overlay.buttonAt("Next##settlement-next", width - left - arrowWidth, y, arrowWidth, buttonHeight)) {
                    shownId = browsable[wrap(index + 1, browsable.Count)].getId();
                    firstLine = 0;
                }
            }
            if (narrow) {
                // a phone: the arrows have the row to themselves, the name goes under them
                y += buttonHeight + gap;
                y = NationScreen.drawWrappedCentred(getTitle(simulation, settlement), height / 30f, y, width, left, lineHeight * 0.75f, 2);
            } else {
                float titleWidth = width - (browsable.Count > 1 ? (arrowWidth + gap) * 2f : 0f);
                NationScreen.drawWrappedCentred(getTitle(simulation, settlement), height / 26f, y + (buttonHeight - lineHeight * 0.8f) * 0.5f,
                    width, (width - titleWidth) / 2f + left * 0.5f, lineHeight * 0.8f, 1);
                y += buttonHeight + gap;
            }
            if (browsable.Count > 1) {
                Overlay.textCentered((index + 1) + " of " + browsable.Count + " settlements of your nation", height / 40f, y, width);
                y += lineHeight * 0.6f;
            }

            // the action strip sits at the bottom, above the status line and the hint
            Actions actions = getActions(simulation, settlement);
            // inside, the hint says what Exit Settlement and Stay Inside do, which takes more than a line on a phone
            int hintLines = actions.canExit && narrow ? 3 : 1;
            float hintStep = lineHeight * 0.62f;
            float hintTop = height - lineHeight * 0.6f - hintStep * hintLines;
            float statusTop = hintTop - lineHeight * 1.5f;
            // the Build buttons, while the player stands inside one of their own (s01)
            List<WorldCommand> build = getBuildCommands(simulation, settlement, payWithCoins);
            // the Pay with Coins toggle sits in the note's row, not in the grid, so it adds no row on a phone
            WorldCommand payToggle = build.Find(isPayToggle);
            if (payToggle != null) {
                build.Remove(payToggle);
            }
            int perRow = narrow ? 2 : System.Math.Max(1, System.Math.Min(build.Count, 4));
            int buildRows = (build.Count + perRow - 1) / perRow;
            float noteRow = payToggle != null ? buttonHeight : hintStep;
            float buildHeight = build.Count == 0 ? 0f : noteRow + gap * 2f + (buttonHeight + gap) * buildRows;
            // on a phone, Give and Take Back go under the picker, and the note may take three lines
            bool stackGive = narrow && actions.canGive;
            int actionRows = 1 + (actions.canGive || actions.canTakeBack || actions.canHandBack ? 1 : 0) + (stackGive ? 1 : 0);
            int noteLines = narrow ? 3 : 2;
            float noteHeight = lineHeight * 0.62f * noteLines + gap;
            float actionsTop = statusTop - (buttonHeight + gap) * actionRows - noteHeight - buildHeight - gap;

            // the sections: two columns on a wide screen, one on a phone; drag or wheel to scroll
            bool twoColumns = width >= UiScale.px(860f);
            float columnGap = UiScale.px(24f);
            float columnWidth = twoColumns ? (width - left * 2f - columnGap) / 2f : width - left * 2f;
            float headingSize = Overlay.atLeastAtlasFontSize(height / 32f);
            float textSize = Overlay.atLeastAtlasFontSize(height / 36f);
            float textScale = Overlay.fontScaleFor(textSize);
            float headingStep = lineHeight * 0.8f;
            float textStep = lineHeight * 0.68f;
            List<Section> sections = getSections(simulation, settlement);
            // each wrapped row, and whether it carries on the line above it (kept in one column)
            List<List<(string text, bool continues)>> wrapped = new List<List<(string text, bool continues)>>();
            foreach (Section section in sections) {
                List<(string text, bool continues)> lines = new List<(string text, bool continues)>();
                foreach (string line in section.lines) {
                    List<string> parts = TextWrap.wrap(line, columnWidth, text => ImGui.CalcTextSize(text).X * textScale);
                    for (int j = 0; j < parts.Count; j++) {
                        lines.Add((parts[j], j > 0));
                    }
                }
                wrapped.Add(lines);
            }
            float top = y + gap;
            float bottom = actionsTop - gap;
            // the rows flow down the first column and on into the second, as a newspaper's do,
            // so a long Buildings section carries over instead of being cut off; scrolled by whole rows
            List<bool> headings = new List<bool>();
            List<bool> continues = new List<bool>();
            List<string> rows = new List<string>();
            for (int i = 0; i < sections.Count; i++) {
                headings.Add(true);
                continues.Add(false);
                rows.Add(sections[i].heading);
                for (int j = 0; j < wrapped[i].Count; j++) {
                    headings.Add(false);
                    continues.Add(wrapped[i][j].continues);
                    rows.Add(wrapped[i][j].text);
                }
            }
            int columns = twoColumns ? 2 : 1;
            float sectionGap = twoColumns ? gap : 0f;
            // the last row is kept for "Scroll for more" when it does not all fit
            float flowBottom = bottom;
            if (layoutRows(headings, continues, 0, bottom - top, columns, headingStep, textStep, sectionGap).Count < rows.Count) {
                flowBottom -= textStep;
            }
            float flowHeight = flowBottom - top;
            int lastFirst = 0;
            while (lastFirst < rows.Count - 1 && layoutRows(headings, continues, lastFirst, flowHeight, columns, headingStep, textStep, sectionGap).Count < rows.Count - lastFirst) {
                lastFirst++;
            }
            firstLine = System.Math.Clamp(firstLine + readScroll(top, flowBottom, textStep), 0, lastFirst);
            List<RowPlace> placed = layoutSections(headings, continues, firstLine, flowHeight, columns, headingStep, textStep, sectionGap);
            foreach (RowPlace place in placed) {
                float x = left + (columnWidth + columnGap) * place.column;
                if (headings[place.row]) {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(HeadingColour));
                    Overlay.textAtScaled(rows[place.row], headingSize, x, top + place.y);
                    ImGui.PopStyleColor();
                } else {
                    Overlay.textAtScaled(rows[place.row], textSize, x, top + place.y);
                }
            }
            if (firstLine + placed.Count < rows.Count) {
                Overlay.textCentered(TouchLayout.isActive() ? "Drag for more" : "Scroll for more", height / 42f, flowBottom + textStep * 0.4f, width);
            }

            float rowWidth = width - left * 2f;
            float buildY = actionsTop;
            if (build.Count > 0) {
                string note = payWithCoins || (payToggle == null && build.Exists(isUpgrade)) ? buyNote(player) : BuildNote;
                if (payToggle == null) {
                    Overlay.textCentered(note, height / 40f, buildY, width);
                } else {
                    // a phone: the toggle alone, full width, with the purse on it;
                    // wider: the note, and the toggle at the row's end
                    float toggleWidth = narrow ? rowWidth : System.MathF.Min(UiScale.px(220f), rowWidth / 4f);
                    string toggleText = narrow ? payToggle.getLabel() + " (" + coinsOf(player) + " coins)" : payToggle.getLabel();
                    if (!narrow) {
                        Overlay.textCentered(note, height / 40f, buildY + (buttonHeight - hintStep) * 0.5f, width);
                    }
                    if (Overlay.buttonAt(fitLabel(toggleText, toggleWidth) + "##settlement-pay", left + rowWidth - toggleWidth, buildY, toggleWidth, buttonHeight)) {
                        WorldCommand chosen = payToggle;
                        armedBuy = null;
                        pending = () => chosen.execute();
                    }
                    if (!TouchLayout.isActive() && ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(payToggle.getTooltip());
                        ImGui.EndTooltip();
                    }
                }
                buildY += noteRow + gap;
                float each = (rowWidth - gap * (perRow - 1)) / perRow;
                double now = keyClock.Elapsed.TotalSeconds;
                if (armedBuy != null && now - armedAt > BuyArmSeconds) {
                    armedBuy = null;
                }
                for (int i = 0; i < build.Count; i++) {
                    WorldCommand command = build[i];
                    float bx = left + (each + gap) * (i % perRow);
                    float by = buildY + (buttonHeight + gap) * (i / perRow);
                    if (!command.isEnabled()) {
                        ImGui.BeginDisabled();
                    }
                    // a purchase of thousands takes a second tap: the first arms it and shows the price
                    bool armed = isPurchase(command) && command.isEnabled() && command.getLabel() == armedBuy;
                    string shown = armed ? confirmLabel(command.getLabel()) : command.getLabel();
                    bool pressed = Overlay.buttonAt(fitLabel(shown, each) + "##settlement-build-" + i, bx, by, each, buttonHeight);
                    // what a Buy pays for, or why it is greyed (no hover on a touch screen: a tap says why, below)
                    string hover = isPurchase(command) ? command.getTooltip() : null;
                    if (hover != null && !TouchLayout.isActive() && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(hover);
                        ImGui.EndTooltip();
                    }
                    if (!command.isEnabled()) {
                        ImGui.EndDisabled();
                        // no hover on a touch screen: a tap on a greyed button says what is missing (#444)
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                            WorldHud.explainDisabled(command, player.getStatus());
                        }
                    } else if (pressed && isPurchase(command) && !armed) {
                        armedBuy = command.getLabel();
                        armedAt = now;
                    } else if (pressed) {
                        // one press: the screen is still and nothing slides under the pointer, as on the bar
                        WorldCommand chosen = command;
                        armedBuy = null;
                        pending = () => chosen.execute();
                    }
                }
                buildY += (buttonHeight + gap) * buildRows + gap;
            }

            // the note, then the buttons
            NationScreen.drawWrappedCentred(actions.note, height / 38f, buildY, width, left, lineHeight * 0.62f, noteLines);
            float rowY = buildY + noteHeight;
            if (actions.canGive || actions.canTakeBack || actions.canHandBack) {
                if (actions.canGive) {
                    if (pickFor != settlement.getId()) {
                        pickFor = settlement.getId();
                        pick = 0;
                    }
                    pick = wrap(pick, actions.candidates.Count);
                    Entity candidate = actions.candidates[pick];
                    float small = System.MathF.Min(UiScale.px(56f), rowWidth / 8f);
                    float give = stackGive ? (actions.canTakeBack ? (rowWidth - gap) / 2f : rowWidth) : System.MathF.Min(UiScale.px(110f), rowWidth / 5f);
                    float take = actions.canTakeBack ? (stackGive ? give : System.MathF.Min(UiScale.px(130f), rowWidth / 4f)) : 0f;
                    float label = stackGive ? rowWidth - small * 2f - gap * 2f
                        : rowWidth - small * 2f - give - take - gap * (actions.canTakeBack ? 4f : 3f);
                    float x = left;
                    if (Overlay.buttonAt("<##give-prev", x, rowY, small, buttonHeight)) {
                        pick = wrap(pick - 1, actions.candidates.Count);
                    }
                    x += small + gap;
                    ImGui.BeginDisabled();
                    Overlay.buttonAt(fitLabel(describePick(candidate, player), label) + "##give-who", x, rowY, label, buttonHeight);
                    ImGui.EndDisabled();
                    x += label + gap;
                    if (Overlay.buttonAt(">##give-next", x, rowY, small, buttonHeight)) {
                        pick = wrap(pick + 1, actions.candidates.Count);
                    }
                    x += small + gap;
                    if (stackGive) {
                        x = left;
                        rowY += buttonHeight + gap;
                    }
                    if (Overlay.buttonAt(GiveLabel + "##give", x, rowY, give, buttonHeight)) {
                        Settlement target = settlement;
                        EntityId to = candidate.getId();
                        pending = () => new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository()).grant(player, target, to);
                    }
                    x += give + gap;
                    if (actions.canTakeBack && Overlay.buttonAt(fitLabel(takeBackLabel(simulation, settlement), take) + "##take-back", x, rowY, take, buttonHeight)) {
                        Settlement target = settlement;
                        pending = () => new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository()).takeBack(player, target);
                    }
                } else {
                    float w = System.MathF.Min(UiScale.px(180f), rowWidth);
                    string text = actions.canTakeBack ? takeBackLabel(simulation, settlement) : HandBackLabel;
                    if (Overlay.buttonAt(text + "##hand-back", (width - w) / 2f, rowY, w, buttonHeight)) {
                        Settlement target = settlement;
                        pending = () => new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository()).takeBack(player, target);
                    }
                }
                rowY += buttonHeight + gap;
            }
            // Exit Settlement (when inside it) and Back, side by side
            float half = System.MathF.Min(UiScale.px(200f), (rowWidth - gap) / 2f);
            float pairX = (width - (actions.canExit ? half * 2f + gap : half)) / 2f;
            if (actions.canExit) {
                if (Overlay.buttonAt(ExitLabel + "##settlement-exit", pairX, rowY, half, buttonHeight)) {
                    pending = () => exit(simulation);
                    action = UiAction.ResumeWorld;
                }
                pairX += half + gap;
            }
            // inside, Back leaves the player in the settlement: it says so
            if (Overlay.buttonAt((actions.canExit ? StayInsideLabel : BackLabel) + "##settlement-back", pairX, rowY, half, buttonHeight)) {
                action = UiAction.CloseSettlement;
            }

            string status = ObjectiveKeyNames.resolve(player.getStatus().getStatus());
            if (!string.IsNullOrEmpty(status)) {
                NationScreen.drawWrappedCentred(status, height / 36f, statusTop, width, left, lineHeight * 0.65f, 2);
            }
            if (actions.canExit) {
                NationScreen.drawWrappedCentred(InsideHint, height / 38f, hintTop, width, left, hintStep, hintLines);
            } else {
                Overlay.textCentered(ObjectiveKeyNames.closeHint(CloseHint), height / 36f, hintTop, width);
            }
            Overlay.end();
            if (Overlay.touchCloseButton("settlement", width)) {
                action = UiAction.CloseSettlement;
            }
            pending?.Invoke();
            return action;
        }

        /** Where a row of the sections goes: its index, its column, and its top below the sections' top. */
        public readonly struct RowPlace {
            public readonly int row;
            public readonly int column;
            public readonly float y;

            public RowPlace(int row, int column, float y) {
                this.row = row;
                this.column = column;
                this.y = y;
            }
        }

        /**
        * Lays the rows from first on down the columns, each height tall: a
        * row that does not fit starts the next column, a line wrapped over
        * several rows (continues: the row carries on the one above) stays in
        * one column, a heading never ends a column without its first line,
        * and a heading below another row has sectionGap above it. Returns
        * the rows placed, in order; those left over need a scroll. With
        * keepSections a column breaks only between sections, and a section
        * taller than a column ends the layout there. Measured only, so a
        * test can check it.
        */
        public static List<RowPlace> layoutRows(IList<bool> headings, IList<bool> continues, int first, float height, int columns,
                float headingStep, float textStep, float sectionGap, bool keepSections = false) {
            List<RowPlace> placed = new List<RowPlace>();
            int column = 0;
            float y = 0f;
            for (int i = first; i < headings.Count; i++) {
                bool heading = headings[i];
                float lead = heading && y > 0f ? sectionGap : 0f;
                float step = heading ? headingStep : textStep;
                // a heading keeps the first line under it, and a line its wrapped rows, unless they fill a column
                int start = heading ? i + 1 : i;
                int end = start;
                if (start < headings.Count && !headings[start] && (heading || !continues[i])) {
                    end = start + 1;
                    while (end < headings.Count && !headings[end] && continues[end]) {
                        end++;
                    }
                }
                if (heading && keepSections) {
                    // the whole section, so a column breaks only between sections
                    end = i + 1;
                    while (end < headings.Count && !headings[end]) {
                        end++;
                    }
                }
                float need = (heading ? step : 0f) + textStep * (end - start);
                if (keepSections && heading && need > height) {
                    // a section taller than a column: not kept whole, so the rest is not placed
                    break;
                }
                if (need > height || need == 0f) {
                    need = step;
                }
                if (y + lead + need > height) {
                    column++;
                    y = 0f;
                    lead = 0f;
                    if (column >= columns || need > height) {
                        break;
                    }
                }
                placed.Add(new RowPlace(i, column, y + lead));
                y += lead + step;
            }
            return placed;
        }

        /**
        * layoutRows, balanced: from the top, when every section fits whole in
        * a column, the columns are made as even as they can be with each
        * section kept whole (Ownership, People and Buildings on the left,
        * Stores and Market on the right, as a rule). When they do not, or
        * once scrolled, the rows flow on from one column into the next.
        */
        public static List<RowPlace> layoutSections(IList<bool> headings, IList<bool> continues, int first, float height, int columns,
                float headingStep, float textStep, float sectionGap) {
            if (first == 0 && columns > 1
                    && layoutRows(headings, continues, 0, height, columns, headingStep, textStep, sectionGap, true).Count == headings.Count) {
                // the shortest column height that still holds them all
                float low = 0f;
                float high = height;
                for (int i = 0; i < 24; i++) {
                    float mid = (low + high) / 2f;
                    if (layoutRows(headings, continues, 0, mid, columns, headingStep, textStep, sectionGap, true).Count == headings.Count) {
                        high = mid;
                    } else {
                        low = mid;
                    }
                }
                return layoutRows(headings, continues, 0, high, columns, headingStep, textStep, sectionGap, true);
            }
            return layoutRows(headings, continues, first, height, columns, headingStep, textStep, sectionGap);
        }

        /** The label cut with "..." to fit the width, as the command bar cuts a long name. */
        private static string fitLabel(string text, float width) {
            float padding = ImGui.GetStyle().FramePadding.X * 2f;
            if (ImGui.CalcTextSize(text).X + padding <= width) {
                return text;
            }
            string cut = text;
            while (cut.Length > 4 && ImGui.CalcTextSize(cut + "...").X + padding > width) {
                cut = cut.Substring(0, cut.Length - 1);
            }
            return cut + "...";
        }

        private int readScroll(float top, float bottom, float rowHeight) {
            ImGuiIOPtr io = ImGui.GetIO();
            int move = 0;
            if (io.MouseWheel != 0f) {
                move += io.MouseWheel > 0f ? -1 : 1;
            }
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && io.MousePos.Y >= top && io.MousePos.Y <= bottom) {
                dragCarry -= io.MouseDelta.Y;
                while (dragCarry >= rowHeight) {
                    move++;
                    dragCarry -= rowHeight;
                }
                while (dragCarry <= -rowHeight) {
                    move--;
                    dragCarry += rowHeight;
                }
            } else {
                dragCarry = 0f;
            }
            return move;
        }
    }
}
