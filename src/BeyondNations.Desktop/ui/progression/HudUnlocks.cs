using System;
using System.Collections.Generic;

namespace beyondnations.desktop.ui.progression {

    /**
    * Unlock as you go (the contextual HUD design, lane C), after Primordial
    * Rise's unlocks.js and FishE's progression: a new game shows only what
    * its first steps need, and every other part of the HUD and family of
    * command-bar buttons comes when the player's own play has just made it
    * useful, with one card saying what appeared and why.
    *
    * Presentation only. Nothing here changes what the simulation allows or
    * how it steps (the keys still work); the rules read an UnlockSnapshot,
    * taken with read-only calls. Nothing is saved either: unlocks are
    * worked out from the world. A rule holds when its natural condition
    * does, or when the player has been pointed at an objective that needs
    * it (the safety net, which also makes most unlocks come back on load,
    * since the objective count is saved). Announcing is this session's
    * memory: on load, everything that already holds counts as announced, as
    * FishE does for old saves. Within a session an unlock never re-locks.
    *
    * The table is Rules, in the order the unlocks usually come; PACING.md
    * ("Unlocks") has the order and times the pacing bot meets them in.
    */
    public sealed class HudUnlocks : IHudUnlocks {

        /** One row of the table. */
        public sealed class Rule {
            public readonly UnlockId id;
            public readonly string title;
            public readonly string text;
            public readonly string icon;
            /** First-arc objective ids whose hint points at what this unlocks: it must be unlocked by the time each is current. */
            public readonly string[] neededBy;
            /** When play has made it useful, without the safety net. */
            public readonly System.Func<UnlockSnapshot, bool> when;

            public Rule(UnlockId id, string title, string text, string icon, string[] neededBy, System.Func<UnlockSnapshot, bool> when) {
                this.id = id;
                this.title = title;
                this.text = text;
                this.icon = icon;
                this.neededBy = neededBy;
                this.when = when;
            }

            public UnlockCard card() {
                return new UnlockCard(id, title, text, icon);
            }

            /** The natural condition, or the player has been pointed at an objective that needs it. */
            public bool holds(UnlockSnapshot s) {
                if (when(s)) {
                    return true;
                }
                foreach (string objective in neededBy) {
                    if (s.objectivesReached.Contains(objective)) {
                        return true;
                    }
                }
                return false;
            }
        }

        private static readonly string[] None = new string[0];

        private static bool autumnOrWinter(Season season) {
            return season == Season.AUTUMN || season == Season.WINTER;
        }

        /** The table. Card text is one or two short sentences and names no key, so a phone player reads it as it is. */
        public static readonly Rule[] Rules = {
            new Rule(UnlockId.Nation, "Nations",
                "You have something to build with. Start a nation of your own, or join one.",
                "nation", new[] { "join-nation" },
                s => s.wood > 0 || s.stone > 0 || s.mealsEaten > 0 || s.inNation),
            new Rule(UnlockId.Crafting, "Crafting",
                "You carry enough wood and stone for a tool. An axe chops faster, a pickaxe digs faster.",
                "craft", None,
                s => s.canCraftTool || s.hasTool),
            new Rule(UnlockId.Founding, "Found a settlement",
                "You lead a nation. With enough wood, found its first settlement away from the others.",
                "settlement", new[] { "found-settlement", "second-settlement" },
                s => s.leads || s.settlements > 0),
            new Rule(UnlockId.Settlement, "Settlements",
                "A settlement is in reach. Step inside to meet the people who live there; once you have a home, you can travel back to it from anywhere.",
                "gate", new[] { "enter-settlement" },
                s => s.hasHome || s.inSettlement || s.nearSettlement || s.settlements > 0),
            new Rule(UnlockId.Era, "Eras",
                "Your nation has a home. Grow it through the eras, Camp to Kingdom; the Journal shows what the next one needs.",
                "era", new[] { "grow-nation" },
                s => s.settlements > 0 || s.era > Era.CAMP),
            new Rule(UnlockId.Settlers, "Settlers",
                "Your settlement draws settlers. Talk to them, give gifts, and invite them into your nation.",
                "people", new[] { "grow-nation" },
                s => s.inNation && (s.hasHome || s.settlements > 0)),
            new Rule(UnlockId.Building, "Building",
                "You stand in your own settlement. Build a market stall and homes here with wood and stone.",
                "build", new[] { "build-stall" },
                s => s.managesHere || s.nationStalls > 0 || s.nationBuildings > 0),
            new Rule(UnlockId.Market, "Market",
                "This settlement has a market stall. Buy food and sell your wood and stone here.",
                "market", new[] { "sell-resources" },
                s => s.marketHere || s.nationStalls > 0 || s.marketSales > 0),
            new Rule(UnlockId.Members, "Your people",
                "Someone has joined your nation. As its leader you can give them orders and make them merchants or traders.",
                "crown", None,
                s => s.leads && s.members >= 2),
            new Rule(UnlockId.Treasury, "Treasury",
                "Your settlement keeps coins and goods of its own. Collect them when you are inside.",
                "chest", None,
                s => s.treasuryToCollect),
            new Rule(UnlockId.Laws, "Laws",
                "Your nation is big enough to govern. Set its taxes, levies, borders and rations, and see your heir.",
                "scroll", None,
                s => s.leads && (s.era >= Era.VILLAGE || s.members >= 5)),
            new Rule(UnlockId.Caravans, "Caravans",
                "A caravan can carry your goods to other markets. Send one from a settlement with a storehouse.",
                "caravan", None,
                s => s.era >= Era.VILLAGE || s.caravanInReach || s.playerCaravans > 0),
            new Rule(UnlockId.Diplomacy, "Neighbours",
                "Another nation is close. Gift it, ally with it, or denounce it, before it comes to blows.",
                "flag", None,
                s => s.leads && (s.onForeignClaim || s.hostileNeighbour)),
            new Rule(UnlockId.Winter, "Winter is coming",
                "Trees go bare in winter. Watch the days left and lay in food for you and your people.",
                "snow", None,
                s => autumnOrWinter(s.season)),
            new Rule(UnlockId.Feeding, "Feed your people",
                "Your people need food to last. Give them yours when you are in your settlement.",
                "bread", None,
                s => s.hungryHere > 0 || (s.inNation && autumnOrWinter(s.season))),
            new Rule(UnlockId.Saplings, "Saplings",
                "You carry a sapling. Plant it out in the open near home and a tree grows there.",
                "sapling", None,
                // every tree drops one, so not with the first chop: once there is a home to plant near
                s => s.hasSapling && (s.hasHome || s.settlements > 0)),
        };

        /** How often, in ticks, update looks again (half a second at 50 ticks a second). */
        public const int CheckEveryTicks = 25;

        private readonly HashSet<UnlockId> granted = new HashSet<UnlockId>();
        private readonly Queue<UnlockCard> cards = new Queue<UnlockCard>();
        private readonly bool everything;
        private int lastCheckTick = int.MinValue;

        public HudUnlocks() : this(false) {
        }

        private HudUnlocks(bool everything) {
            this.everything = everything;
            granted.Add(UnlockId.Basics);
        }

        /** Everything unlocked, nothing announced: --all-unlocked, captures and tests. */
        public static HudUnlocks allUnlocked() {
            return new HudUnlocks(true);
        }

        public static Rule ruleFor(UnlockId id) {
            foreach (Rule rule in Rules) {
                if (rule.id == id) {
                    return rule;
                }
            }
            return null;
        }

        /** A loaded world younger than this, still in the Camp era, keeps the unlock-as-you-go ramp. */
        public const int YoungWorldSeconds = 600;

        /**
        * Whether a loaded world is still young: in the Camp era and under
        * YoungWorldSeconds of game time. A young world re-derives its unlocks
        * from the world (most come straight back, already announced) so a new
        * player who saves and reloads keeps the gentle ramp; an older world
        * gets everything. A young save from before the contextual HUD also
        * gets the re-derived ramp, which is safe: commandUnlocked fails open.
        */
        public static bool isYoungWorld(Era era, int totalTicks, int ticksPerSecond) {
            return era == Era.CAMP && totalTicks < YoungWorldSeconds * Math.Max(1, ticksPerSecond);
        }

        public static bool isYoungWorld(Simulation simulation) {
            return simulation != null && isYoungWorld(simulation.getEraTracker().getEra(),
                simulation.getTickCounter().getTotalTicks(), simulation.getTickCounter().getTicksPerSecond());
        }

        /**
        * A world was started or loaded. A new world starts bare; a loaded one
        * starts with everything that holds now, already announced.
        */
        public void begin(Simulation simulation, bool newWorld) {
            granted.Clear();
            granted.Add(UnlockId.Basics);
            cards.Clear();
            lastCheckTick = int.MinValue;
            if (!newWorld && simulation != null) {
                grantSilently(UnlockSnapshot.of(simulation));
            }
        }

        /** Every unlock that holds in this snapshot, with no card. */
        public void grantSilently(UnlockSnapshot snapshot) {
            foreach (Rule rule in Rules) {
                if (rule.holds(snapshot)) {
                    granted.Add(rule.id);
                }
            }
        }

        /** Call every frame; it looks at the world every CheckEveryTicks. */
        public void update(Simulation simulation) {
            if (everything || simulation == null || granted.Count == Rules.Length + 1) {
                return;
            }
            int tick = simulation.getTickCounter().getTotalTicks();
            if (lastCheckTick != int.MinValue && tick >= lastCheckTick && tick - lastCheckTick < CheckEveryTicks) {
                return;
            }
            lastCheckTick = tick;
            observe(UnlockSnapshot.of(simulation));
        }

        /** Grants, with a card each, every unlock that holds now and had not; returns them in table order. */
        public List<UnlockId> observe(UnlockSnapshot snapshot) {
            List<UnlockId> fresh = new List<UnlockId>();
            if (everything) {
                return fresh;
            }
            foreach (Rule rule in Rules) {
                if (!granted.Contains(rule.id) && rule.holds(snapshot)) {
                    granted.Add(rule.id);
                    cards.Enqueue(rule.card());
                    fresh.Add(rule.id);
                }
            }
            return fresh;
        }

        public bool isUnlocked(UnlockId id) {
            return everything || granted.Contains(id);
        }

        public bool isEverythingUnlocked() {
            return everything || granted.Count == Rules.Length + 1;
        }

        public bool commandUnlocked(string label) {
            UnlockId? id = unlockForCommand(label);
            return id == null || isUnlocked(id.Value);
        }

        public IEnumerable<UnlockCard> drainNewCards() {
            List<UnlockCard> drained = new List<UnlockCard>(cards);
            cards.Clear();
            return drained;
        }

        // --- the command bar ---

        // the labels as WorldCommandBar, CaravanCommands and MarketTradePanel make them;
        // TestHudUnlocks holds them to those classes' constants
        public const string SettlementLabel = "Settlement";
        public const string TradeAtMarketLabel = "Trade at Market";
        public const string SendCaravanLabel = "Send Caravan";

        private static readonly string[] NationPrefixes = { "Create Nation", "Join Nation", "Leave Nation" };
        private static readonly string[] SettlementPrefixes = { "Enter Settlement", "Exit Settlement", "Teleport Home" };
        private static readonly string[] MarketPrefixes = { "Purchase Food", "Sell Resources", TradeAtMarketLabel, "Purchase Stall", "Transfer Items", "Collect Coins", "Collect Food" };
        private static readonly string[] TreasuryPrefixes = { "Withdraw Funds", "Collect Stockpile" };
        private static readonly string[] MembersPrefixes = { "Order ", "Appoint " };
        private static readonly string[] CaravanPrefixes = { SendCaravanLabel, "Sell to ", "Escort ", "Raid " };
        private static readonly string[] DiplomacyPrefixes = { "Propose Alliance to ", "Offer Peace to ", "Denounce ", "Declare War on " };

        /**
        * Which unlock a command-bar button belongs to, by its label, or null
        * for one that is never locked (Eat, Deal with Bandits, and any label
        * this does not know, so a new button is never hidden by mistake).
        */
        public static UnlockId? unlockForCommand(string label) {
            if (string.IsNullOrEmpty(label)) {
                return null;
            }
            if (startsWithAny(label, NationPrefixes)) return UnlockId.Nation;
            if (label.StartsWith("Found Settlement")) return UnlockId.Founding;
            if (label == SettlementLabel || startsWithAny(label, SettlementPrefixes)) return UnlockId.Settlement;
            if (label.StartsWith("Build ")) return UnlockId.Building;
            if (startsWithAny(label, MarketPrefixes)) return UnlockId.Market;
            if (startsWithAny(label, TreasuryPrefixes)) return UnlockId.Treasury;
            if (label.StartsWith("Feed Residents")) return UnlockId.Feeding;
            if (label.StartsWith("Plant Sapling")) return UnlockId.Saplings;
            // "Gift Rivermark 50 Coins" is a gift to a nation; "Gift Ada" to a pawn
            if (label.StartsWith("Gift ") && label.EndsWith(" Coins")) return UnlockId.Diplomacy;
            if (startsWithAny(label, DiplomacyPrefixes)) return UnlockId.Diplomacy;
            if (label.StartsWith("Talk to ") || label.StartsWith("Gift ") || label.StartsWith("Invite ")) return UnlockId.Settlers;
            if (startsWithAny(label, MembersPrefixes)) return UnlockId.Members;
            if (label.StartsWith("Craft ")) return UnlockId.Crafting;
            // "Buy Axe (12)" is the crafting shortcut; "Buy Wood (3)" is a caravan's goods
            if (label.StartsWith("Buy " + RecipeBook.Axe.getName() + " (") || label.StartsWith("Buy " + RecipeBook.Pickaxe.getName() + " (")) return UnlockId.Crafting;
            if (label.StartsWith("Buy ") || startsWithAny(label, CaravanPrefixes)) return UnlockId.Caravans;
            return null;
        }

        private static bool startsWithAny(string label, string[] prefixes) {
            foreach (string prefix in prefixes) {
                if (label.StartsWith(prefix, System.StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }
    }
}
