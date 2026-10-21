using System.Text.Json;
using System;
using System.Collections.Generic;

namespace beyondnations {

    /**
    * Being a ruler: the laws a nation lives under, the wars and truces its
    * ruler has declared and agreed, and the player's line of rulers.
    *
    * Laws. The player's nation can set four laws (Laws): taxes, conscription,
    * open borders and rationing. A change is proclaimed and takes effect
    * LAW_DELAY_TICKS later, with a line in the chronicle. Every
    * LAW_ROUND_TICKS a law round runs and each law in force does what it
    * does: taxes move coin from members to the treasury and cost loyalty,
    * rationing hands back some hunger and costs morale, open borders bring
    * tolls, trade and thieves. Loyalty is each pawn's relationship with the
    * player, and a member whose loyalty sinks to DESERT_AT leaves the
    * nation. Conscription, open borders and taxes also reach into other
    * systems through the hooks below (Diplomacy's raids and sieges, pawns'
    * gathering, PopulationGrowth's immigrants).
    *
    * War and peace. A declared war holds two nations' relations down (they
    * cannot warm while it lasts); a peace, or a tribute to a stronger
    * enemy, buys a truce in which neither raids nor besieges the other.
    *
    * Succession. When the player dies as a member of a nation, an heir from
    * that nation takes their place (Succession) and the line goes on; with
    * no heir the run is over. The host reads consumeNotice() after each step
    * and shows the succession or game-over screen.
    *
    * A nation whose laws are all at their first setting, with no wars or
    * truces, is untouched by any of this, so a seeded world that never uses
    * it takes exactly the course it always did. Nothing here draws on
    * randomness.
    */
    public class Realm {
        /** Ten seconds at the default tick rate. */
        public const int LAW_ROUND_TICKS = 500;
        /** How long a proclaimed law takes to come in: thirty seconds. */
        public const int LAW_DELAY_TICKS = 1500;
        /** How long a peace or a tribute holds off raids and sieges: a minute. */
        public const int TRUCE_TICKS = 3000;
        /** A member whose relationship with the player sinks this low deserts. */
        public const int DESERT_AT = -15;
        /** Declaring war drops relations to at least this. */
        public const int WAR_SCORE = -60;
        /** A peace leaves relations at least this: Neutral, but only just. */
        public const int PEACE_SCORE = NationRelations.RIVAL_AT + 1;
        /** Allies of a nation the player declares war on think less of the player by this much. */
        public const int WAR_ON_ALLY_PENALTY = -15;

        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;
        private readonly EventProducer eventProducer;
        private readonly Player player;
        private readonly Diplomacy diplomacy;
        private readonly GameConfig gameConfig;
        private readonly TickCounter tickCounter;

        private int lawRound = 0;
        private List<NationLaws> laws = new List<NationLaws>();
        private HashSet<string> wars = new HashSet<string>();
        private Dictionary<string, int> truces = new Dictionary<string, int>();

        private List<Reign> lineage = new List<Reign>();
        private int reignStartTick = 0;
        private bool runOver = false;
        private SuccessionNotice notice = null;
        // whether the host has been handed the latest notice
        private bool noticeShown = false;

        public Realm(NationRepository nationRepository, EntityRepository entityRepository, EventProducer eventProducer, Player player,
                Diplomacy diplomacy, GameConfig gameConfig, TickCounter tickCounter) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
            this.eventProducer = eventProducer;
            this.player = player;
            this.diplomacy = diplomacy;
            this.gameConfig = gameConfig;
            this.tickCounter = tickCounter;
        }

        public Diplomacy getDiplomacy() {
            return diplomacy;
        }

        private int now() {
            return tickCounter.getTotalTicks();
        }

        // --- laws ---------------------------------------------------------------------------------------

        /** The nation's laws, or null if it has never had any set. */
        public NationLaws getLaws(Nation nation) {
            if (nation == null) {
                return null;
            }
            foreach (NationLaws entry in laws) {
                if (entry.getNationId().Equals(nation.getId())) {
                    return entry;
                }
            }
            return null;
        }

        public int getLevel(Nation nation, LawType law) {
            NationLaws entry = getLaws(nation);
            return entry == null ? 0 : entry.getLevel(law);
        }

        /**
        * Proclaims a change to one of the nation's laws, which takes effect
        * LAW_DELAY_TICKS from now. Choosing the setting already in force
        * withdraws any change still pending. Returns what the player is told.
        */
        public string proclaim(Nation nation, LawType law, int level) {
            if (level < 0 || level >= Laws.getNumLevels(law)) {
                return "There is no such setting of " + Laws.getName(law).ToLowerInvariant() + ".";
            }
            NationLaws entry = getLaws(nation);
            if (entry == null) {
                entry = new NationLaws(nation.getId());
                laws.Add(entry);
            }
            string lawName = Laws.getName(law);
            string levelName = Laws.getLevelName(law, level);
            if (entry.getLevel(law) == level) {
                if (entry.getPending(law) < 0) {
                    return lawName + " are already " + levelName + ".";
                }
                entry.clearPending(law);
                return "The change to " + lawName.ToLowerInvariant() + " is withdrawn; they stay " + levelName + ".";
            }
            entry.proclaim(law, level, now());
            return lawName + ": " + levelName + " proclaimed. It takes effect in " + (LAW_DELAY_TICKS / TickCounter.DefaultTicksPerSecond) + " seconds.";
        }

        /** Ticks until a pending change to the law comes in, or -1 if none is pending. */
        public int getTicksUntilEnacted(Nation nation, LawType law) {
            NationLaws entry = getLaws(nation);
            if (entry == null || entry.getPending(law) < 0) {
                return -1;
            }
            return Math.Max(0, entry.getProclaimedAt(law) + LAW_DELAY_TICKS - now());
        }

        public int getLawRound() {
            return lawRound;
        }

        public void update(int totalTicks) {
            if (totalTicks <= 0 || totalTicks % LAW_ROUND_TICKS != 0) {
                return;
            }
            lawRound++;
            expireTruces(totalTicks);
            foreach (NationLaws entry in new List<NationLaws>(laws)) {
                Nation nation = nationRepository.getNation(entry.getNationId());
                if (nation == null) {
                    continue;
                }
                // laws are the player's to keep: a nation the player no longer leads leaves them be,
                // and a change the player proclaimed there lapses with their rule, as a usurped ruler's does
                if (nation.getLeaderId() != player.getId()) {
                    clearAllPending(entry);
                    continue;
                }
                enactPending(nation, entry, totalTicks);
                applyLaws(nation, entry);
            }
        }

        private void enactPending(Nation nation, NationLaws entry, int totalTicks) {
            foreach (LawType law in Laws.All) {
                int pending = entry.getPending(law);
                if (pending < 0 || totalTicks - entry.getProclaimedAt(law) < LAW_DELAY_TICKS) {
                    continue;
                }
                int before = entry.getLevel(law);
                entry.setLevel(law, pending);
                entry.clearPending(law);
                string text = describeEnactment(nation, law, before, pending);
                player.getStatus().update(text);
                eventProducer.produceStatecraftEvent(text, nation, null, player.getId());
            }
        }

        /** Every change proclaimed and not yet in force is withdrawn; the laws in force stay. */
        private static void clearAllPending(NationLaws entry) {
            foreach (LawType law in Laws.All) {
                entry.clearPending(law);
            }
        }

        private static string describeEnactment(Nation nation, LawType law, int before, int after) {
            string n = nation.getName();
            switch (law) {
                case LawType.TAXES:
                    if (after == 0) {
                        return "Taxes were lifted in " + n + ".";
                    }
                    return (after > before ? "Taxes in " + n + " rose to " : "Taxes in " + n + " fell to ") + Laws.getLevelName(law, after) + ".";
                case LawType.CONSCRIPTION:
                    return after > 0 ? "Conscription came into force in " + n + ": its people drill to defend it." : n + " stood down its conscripts.";
                case LawType.OPEN_BORDERS:
                    return after > 0 ? n + " opened its borders to traders and newcomers." : n + " closed its borders.";
                case LawType.RATIONING:
                    return after > 0 ? "Rationing began in " + n + "." : "Rationing ended in " + n + ".";
                default:
                    return n + " changed its laws.";
            }
        }

        /** Member pawns of the nation that are still in the world, in membership order. */
        private List<Pawn> memberPawns(Nation nation) {
            List<Pawn> pawns = new List<Pawn>();
            foreach (EntityId id in nation.getMemberIds()) {
                Pawn pawn = entityRepository.getEntity(id) as Pawn;
                if (pawn != null && !pawn.isMarkedForDeletion()) {
                    pawns.Add(pawn);
                }
            }
            return pawns;
        }

        private void applyLaws(Nation nation, NationLaws entry) {
            List<Pawn> members = memberPawns(nation);
            List<Settlement> settlements = diplomacy.getSettlements(nation);
            int loyaltyLost = 0;

            int taxLevel = entry.getLevel(LawType.TAXES);
            if (taxLevel > 0) {
                int collected = 0;
                if (settlements.Count > 0) {
                    foreach (Pawn pawn in members) {
                        int pay = Math.Min(Laws.TAX_COINS[taxLevel], pawn.getInventory().getNumItems(ItemType.COIN));
                        if (pay > 0) {
                            pawn.getInventory().removeItem(ItemType.COIN, pay);
                            treasuryFor(pawn, settlements).addFunds(pay);
                            collected += pay;
                        }
                    }
                }
                entry.recordTax(collected);
                if (lawRound % Laws.TAX_LOYALTY_EVERY[taxLevel] == 0) {
                    loyaltyLost++;
                }
            }

            if (entry.isOn(LawType.RATIONING)) {
                foreach (Pawn pawn in members) {
                    float saved = pawn.getMetabolism() * LAW_ROUND_TICKS * Laws.RATIONING_SAVED_PERCENT / 100f;
                    pawn.setEnergy(Math.Min(100f, pawn.getEnergy() + saved));
                }
                if (lawRound % Laws.RATIONING_LOYALTY_EVERY == 0) {
                    loyaltyLost++;
                }
            }

            if (entry.isOn(LawType.OPEN_BORDERS)) {
                foreach (Settlement settlement in settlements) {
                    settlement.addFunds(Laws.OPEN_BORDERS_TOLL);
                }
                if (lawRound % Laws.OPEN_BORDERS_THEFT_EVERY == 0) {
                    steal(nation, settlements);
                }
                if (lawRound % Laws.OPEN_BORDERS_TRADE_EVERY == 0) {
                    foreach (Nation other in nationRepository.getNations()) {
                        if (other != nation && !isAtWar(nation, other)) {
                            diplomacy.change(nation, other, Diplomacy.TRADE);
                        }
                    }
                }
            }

            if (loyaltyLost > 0) {
                foreach (Pawn pawn in members) {
                    pawn.decreaseRelationship(player, loyaltyLost);
                    player.decreaseRelationship(pawn, loyaltyLost);
                }
                foreach (Pawn pawn in members) {
                    if (!pawn.getId().Equals(nation.getLeaderId()) && PawnRelations.getRelationship(pawn, player) <= DESERT_AT) {
                        desert(pawn, nation, entry);
                    }
                }
            }
        }

        /** Taxes go to the member's home settlement when it is one of the nation's, otherwise to its first. */
        private static Settlement treasuryFor(Pawn pawn, List<Settlement> settlements) {
            foreach (Settlement settlement in settlements) {
                if (settlement.getId().Equals(pawn.getHomeSettlementId())) {
                    return settlement;
                }
            }
            return settlements[0];
        }

        private void steal(Nation nation, List<Settlement> settlements) {
            int coins = 0;
            int wood = 0;
            foreach (Settlement settlement in settlements) {
                int takeCoins = Math.Min(Laws.OPEN_BORDERS_THEFT_AMOUNT, settlement.getFunds());
                if (takeCoins > 0) {
                    settlement.removeFunds(takeCoins);
                    coins += takeCoins;
                }
                int takeWood = Math.Min(Laws.OPEN_BORDERS_THEFT_AMOUNT, settlement.getInventory().getNumItems(ItemType.WOOD));
                if (takeWood > 0) {
                    settlement.getInventory().removeItem(ItemType.WOOD, takeWood);
                    wood += takeWood;
                }
            }
            if (coins + wood > 0) {
                player.getStatus().update("Thieves slipped through the open borders of " + nation.getName() + " and took "
                    + coins + " coins and " + wood + " wood.");
            }
        }

        /**
        * A member who has had enough of the ruler's laws leaves the nation:
        * it gives up any stall, its home and its order, and goes its own way.
        */
        private void desert(Pawn pawn, Nation nation, NationLaws entry) {
            foreach (Settlement settlement in diplomacy.getSettlements(nation)) {
                foreach (Stall stall in settlement.getMarket().getStalls()) {
                    if (stall.getOwnerId() == pawn.getId()) {
                        stall.setOwnerId(null);
                    }
                }
            }
            nation.removeMember(pawn.getId());
            pawn.setNationId(null);
            pawn.setHomeSettlementId(null);
            pawn.setOrder(null);
            pawn.setColor(Rgba.White);
            entry.recordDeserter();
            string text = pawn.getName() + " deserted " + nation.getName() + ", unable to bear its laws.";
            player.getStatus().update(text);
            eventProducer.produceStatecraftEvent(text, nation, null, pawn.getId());
        }

        // --- claims to rule ----------------------------------------------------------------------------

        /**
        * Whether a claimant can take the rule of a nation from its leader by
        * force: null when the claim can stand, otherwise why it cannot. The
        * player's own rule never passes this way -- it goes only to an heir,
        * when the player dies (Succession) -- so a nation the player leads
        * cannot be usurped. Nor can a claimant who is no longer a member of
        * the nation, is dead or dying, or already leads it.
        */
        public string whyClaimFails(Nation nation, Pawn claimant) {
            if (nation == null || nationRepository.getNation(nation.getId()) == null) {
                return "the nation is no more";
            }
            if (claimant == null || claimant.isMarkedForDeletion() || claimant.getEnergy() <= 0 || !nation.isMember(claimant.getId())) {
                return "the claimant is no longer of " + nation.getName();
            }
            if (claimant.getId().Equals(nation.getLeaderId())) {
                return "the claimant already leads " + nation.getName();
            }
            if (nation.getLeaderId() != null && nation.getLeaderId().Equals(player.getId())) {
                return "the rule of " + nation.getName() + " passes only to an heir";
            }
            return null;
        }

        /**
        * A claimant takes the rule of a nation by force instead of by
        * succession. The deposed leader stays on as a serf and the chronicle
        * records the change. The nation's laws stay as they are -- they
        * belong to the nation, as they do when an heir succeeds -- but any
        * change the deposed ruler proclaimed that has not yet come in dies
        * with their rule. Returns false, changing nothing, when the claim
        * cannot stand (whyClaimFails).
        */
        public bool usurp(Nation nation, Pawn claimant) {
            if (whyClaimFails(nation, claimant) != null) {
                return false;
            }
            EntityId previous = nation.getLeaderId();
            if (previous != null && nation.isMember(previous)) {
                nation.setRole(previous, NationRole.SERF);
            }
            nation.setLeaderId(claimant.getId());
            nation.setRole(claimant.getId(), NationRole.LEADER);
            NationLaws entry = getLaws(nation);
            if (entry != null) {
                clearAllPending(entry);
            }
            eventProducer.produceLeadershipChangeEvent(nation, previous, claimant.getId());
            return true;
        }

        /**
        * A member is driven out of the nation: it gives up any stall, its
        * home and its order, loses the nation's colour and goes its own way.
        */
        public void expel(Pawn pawn, Nation nation) {
            foreach (Settlement settlement in diplomacy.getSettlements(nation)) {
                foreach (Stall stall in settlement.getMarket().getStalls()) {
                    if (stall.getOwnerId() != null && stall.getOwnerId().Equals(pawn.getId())) {
                        stall.setOwnerId(null);
                    }
                }
            }
            nation.removeMember(pawn.getId());
            pawn.setNationId(null);
            pawn.setHomeSettlementId(null);
            pawn.setOrder(null);
            pawn.setColor(Rgba.White);
            eventProducer.produceNationLeaveEvent(nation, pawn.getId());
        }

        /**
        * Changes two nations' relations by the realm's rules, however the
        * change comes about: nations at war cannot warm toward each other,
        * and nations under a truce cannot grate. Returns the change made.
        */
        public int changeRelations(Nation a, Nation b, int delta) {
            int bent = adjustDrift(a, b, delta);
            if (bent != 0) {
                diplomacy.change(a, b, bent);
            }
            return bent;
        }

        // --- hooks for other systems --------------------------------------------------------------------

        /**
        * How long a settlement waits between immigrants, given how long it
        * would wait with no laws; -1 when none will come. Taxes put
        * newcomers off; open borders draw them.
        */
        public int immigrationCooldown(Settlement settlement, int baseTicks) {
            Nation nation = nationRepository.getNation(settlement.getNationId());
            NationLaws entry = getLaws(nation);
            if (entry == null || !nation.getLeaderId().Equals(player.getId())) {
                return baseTicks;
            }
            int percent = Laws.TAX_IMMIGRATION_PERCENT[entry.getLevel(LawType.TAXES)];
            if (percent < 0) {
                return -1;
            }
            if (entry.isOn(LawType.OPEN_BORDERS)) {
                percent = percent * Laws.OPEN_BORDERS_IMMIGRATION_PERCENT / 100;
            }
            return baseTicks * percent / 100;
        }

        /** How many times the usual loot a raid on this nation carries off. */
        public int raidLootMultiplier(Nation victim) {
            return isLawInForce(victim, LawType.OPEN_BORDERS) ? Laws.OPEN_BORDERS_RAID_MULTIPLIER : 1;
        }

        /** Conscripts the nation fields in its defence: one for every CONSCRIPT_PER_MEMBERS pawns, rounded up. */
        public int getConscripts(Nation nation) {
            if (!isLawInForce(nation, LawType.CONSCRIPTION)) {
                return 0;
            }
            int pawns = memberPawns(nation).Count;
            return (pawns + Laws.CONSCRIPT_PER_MEMBERS - 1) / Laws.CONSCRIPT_PER_MEMBERS;
        }

        /**
        * Called after a pawn gathers: a conscript loses part of what it just
        * took from a tree or rock to the hours it spends at drill.
        */
        public void afterGather(Pawn pawn, int woodBefore, int stoneBefore) {
            if (pawn.getNationId() == null || !isLawInForce(nationRepository.getNation(pawn.getNationId()), LawType.CONSCRIPTION)) {
                return;
            }
            Inventory inventory = pawn.getInventory();
            int woodLost = Math.Max(0, inventory.getNumItems(ItemType.WOOD) - woodBefore) * Laws.CONSCRIPT_GATHER_LOSS_PERCENT / 100;
            int stoneLost = Math.Max(0, inventory.getNumItems(ItemType.STONE) - stoneBefore) * Laws.CONSCRIPT_GATHER_LOSS_PERCENT / 100;
            if (woodLost > 0) {
                inventory.removeItem(ItemType.WOOD, woodLost);
            }
            if (stoneLost > 0) {
                inventory.removeItem(ItemType.STONE, stoneLost);
            }
        }

        private bool isLawInForce(Nation nation, LawType law) {
            if (nation == null || !nation.getLeaderId().Equals(player.getId())) {
                return false;
            }
            NationLaws entry = getLaws(nation);
            return entry != null && entry.isOn(law);
        }

        /**
        * How a diplomacy round's drift between two nations is bent by war and
        * truce: nations at war cannot warm, and nations under a truce cannot
        * grate on each other.
        */
        public int adjustDrift(Nation a, Nation b, int delta) {
            if (isAtWar(a, b)) {
                return Math.Min(delta, 0);
            }
            if (hasTruce(a, b)) {
                return Math.Max(delta, 0);
            }
            return delta;
        }

        // --- war and peace ------------------------------------------------------------------------------

        private static string pairKey(Nation a, Nation b) {
            string x = a.getId().ToString();
            string y = b.getId().ToString();
            return string.CompareOrdinal(x, y) <= 0 ? x + "|" + y : y + "|" + x;
        }

        public bool isAtWar(Nation a, Nation b) {
            return a != null && b != null && a != b && wars.Contains(pairKey(a, b));
        }

        public bool hasTruce(Nation a, Nation b) {
            int until;
            return a != null && b != null && a != b && truces.TryGetValue(pairKey(a, b), out until) && until > now();
        }

        /** Ticks left on the truce between two nations, or 0. */
        public int getTruceTicksLeft(Nation a, Nation b) {
            int until;
            if (a == null || b == null || !truces.TryGetValue(pairKey(a, b), out until)) {
                return 0;
            }
            return Math.Max(0, until - now());
        }

        /**
        * Declares war: any alliance is broken, relations fall to at least
        * WAR_SCORE (Hostile, so raids and sieges follow wherever the two are
        * neighbours) and stay down while it lasts, and the enemy's allies
        * think less of the aggressor.
        */
        public void declareWar(Nation own, Nation enemy) {
            NationRelations relations = nationRepository.getRelations();
            truces.Remove(pairKey(own, enemy));
            wars.Add(pairKey(own, enemy));
            relations.breakAlliance(own, enemy);
            relations.markDenounced(own, enemy);
            int score = relations.getScore(own, enemy);
            if (score > WAR_SCORE) {
                diplomacy.change(own, enemy, WAR_SCORE - score);
            }
            foreach (Nation ally in nationRepository.getNations()) {
                if (ally != own && ally != enemy && relations.isAllied(ally, enemy)) {
                    diplomacy.change(own, ally, WAR_ON_ALLY_PENALTY);
                }
            }
            eventProducer.produceStatecraftEvent(own.getName() + " declared war on " + enemy.getName() + ".", own, enemy, player.getId());
        }

        /** Ends a war (or a feud) with a truce, and brings relations up to at least PEACE_SCORE. */
        public void makePeace(Nation own, Nation enemy) {
            bool wasAtWar = wars.Remove(pairKey(own, enemy));
            grantTruce(own, enemy);
            int score = nationRepository.getRelations().getScore(own, enemy);
            if (score < PEACE_SCORE) {
                diplomacy.change(own, enemy, PEACE_SCORE - score);
            }
            eventProducer.produceStatecraftEvent(own.getName() + " and " + enemy.getName() + " made peace"
                + (wasAtWar ? ", ending their war." : "."), own, enemy, player.getId());
        }

        /** No raids or sieges between the two for TRUCE_TICKS. */
        public void grantTruce(Nation a, Nation b) {
            truces[pairKey(a, b)] = now() + TRUCE_TICKS;
        }

        private void expireTruces(int totalTicks) {
            List<string> ended = new List<string>();
            foreach (KeyValuePair<string, int> truce in truces) {
                if (truce.Value <= totalTicks) {
                    ended.Add(truce.Key);
                }
            }
            foreach (string key in ended) {
                truces.Remove(key);
            }
        }

        // --- succession ---------------------------------------------------------------------------------

        /**
        * The player has died. A wanderer with no nation rises again as before
        * (false: the caller respawns them). A member of a nation is
        * succeeded by an heir from it, or, with none, the run ends; either
        * way the host is handed a notice to show (true).
        */
        public bool onPlayerDeath() {
            if (runOver) {
                player.setEnergy(0);
                return true;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation == null) {
                return false;
            }
            int tick = now();
            string ruler = getRulerName();
            string reason;
            Pawn heir = Succession.chooseHeir(nation, player, entityRepository, out reason);
            lineage.Add(new Reign(ruler, nation.getName(), reignStartTick, tick, "hunger"));

            if (heir == null) {
                runOver = true;
                player.setEnergy(0);
                player.setVelocity(System.Numerics.Vector3.Zero);
                notice = new SuccessionNotice(ruler, null, nation.getName(), tick - reignStartTick, reason, true);
                noticeShown = false;
                string ended = "You died of hunger, and with no heir in " + nation.getName() + " your line ended.";
                player.getStatus().update(ended);
                eventProducer.produceStatecraftEvent(ended, nation, null, player.getId());
                return true;
            }

            bool ruled = nation.getLeaderId().Equals(player.getId());
            string heirName = heir.getName();
            Succession.crown(heir, nation, player, entityRepository, diplomacy, gameConfig);
            reignStartTick = tick;
            notice = new SuccessionNotice(ruler, heirName, nation.getName(), tick - lineage[lineage.Count - 1].getStartTick(), reason, false);
            noticeShown = false;
            string text = "You died of hunger. " + heirName + ", " + reason + ", " + (ruled ? "took up the rule of " : "took your place in ")
                + nation.getName() + ".";
            player.getStatus().update("You died. You go on as " + heirName + ", " + reason + ".");
            eventProducer.produceStatecraftEvent(text, nation, null, player.getId());
            return true;
        }

        /** The name the current ruler goes by; the first is "the founder" until an heir brings a name. */
        public string getRulerName() {
            return player.getName() == "Player" ? "The founder" : player.getName();
        }

        public IReadOnlyList<Reign> getLineage() {
            return lineage;
        }

        public int getReignStartTick() {
            return reignStartTick;
        }

        /** True once the player has died with no heir: the run is over and the world is not saved again. */
        public bool isRunOver() {
            return runOver;
        }

        /** The latest succession (or end of the line), for the succession screen; null before the first. */
        public SuccessionNotice getNotice() {
            return notice;
        }

        /**
        * The succession that has just happened, once; null otherwise. The end
        * of the line is handed over every time, so a world whose line has
        * ended always comes back to the game-over screen.
        */
        public SuccessionNotice consumeNotice() {
            if (notice == null || (noticeShown && !notice.isLineEnded())) {
                return null;
            }
            noticeShown = true;
            return notice;
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("lawRound", lawRound);
            writer.beginArray("laws");
            foreach (NationLaws entry in laws) {
                writer.beginObject();
                entry.save(writer);
                writer.endObject();
            }
            writer.endArray();
            writer.writeStringList("wars", wars);
            writer.beginArray("truces");
            foreach (KeyValuePair<string, int> truce in truces) {
                writer.beginArray();
                writer.writeValue(truce.Key);
                writer.writeValue(truce.Value);
                writer.endArray();
            }
            writer.endArray();
            writer.beginArray("lineage");
            foreach (Reign reign in lineage) {
                writer.beginObject();
                reign.save(writer);
                writer.endObject();
            }
            writer.endArray();
            writer.write("reignStartTick", reignStartTick);
            writer.write("runOver", runOver);
            writer.write("noticeShown", noticeShown);
            if (notice == null) {
                writer.getJson().WriteNull("notice");
            }
            else {
                writer.beginObject("notice");
                notice.save(writer);
                writer.endObject();
            }
        }

        internal void load(JsonElement element, SaveReader reader) {
            lawRound = SaveReader.getInt(element, "lawRound");
            laws.Clear();
            foreach (JsonElement entry in SaveReader.property(element, "laws").EnumerateArray()) {
                laws.Add(NationLaws.load(entry, reader));
            }
            wars.Clear();
            wars.UnionWith(SaveReader.getStringList(element, "wars"));
            truces.Clear();
            foreach (JsonElement entry in SaveReader.property(element, "truces").EnumerateArray()) {
                truces[entry[0].GetString()] = entry[1].GetInt32();
            }
            lineage.Clear();
            foreach (JsonElement entry in SaveReader.property(element, "lineage").EnumerateArray()) {
                lineage.Add(Reign.load(entry));
            }
            reignStartTick = SaveReader.getInt(element, "reignStartTick");
            runOver = SaveReader.getBool(element, "runOver");
            noticeShown = SaveReader.getBool(element, "noticeShown");
            JsonElement saved = SaveReader.property(element, "notice");
            notice = saved.ValueKind == JsonValueKind.Null ? null : SuccessionNotice.load(saved);
        }
    }
}
