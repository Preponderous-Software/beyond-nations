using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * Nations living next to each other: how their relations drift, what
    * pawn-led nations decide to do about it, and what hostility costs.
    *
    * Every ROUND_TICKS (five seconds at the default rate) a round is run:
    *
    * 1. Claims are redrawn (Territory) and every pair's score drifts. Two
    *    nations with settlements within neighbour distance rub against each
    *    other (-1 a round, -2 where their borders touch); allies warm (+1);
    *    two nations that are both rivals of the same third nation draw
    *    together (+1 per shared enemy). With nothing pushing it, a score eases
    *    one point toward zero every few rounds. While the player's nation is
    *    sheltered (EraRules.isSheltered), friction alone stops at
    *    SHELTERED_FRICTION_FLOOR (Rival), and whenever a change of named state
    *    involves it, the player is told why (RelationCause).
    * 2. Each nation led by a pawn makes its choices: it agrees an alliance
    *    with another pawn-led nation once relations reach +40, pays tribute
    *    (20 coins from its treasury) to a hostile neighbour at least twice its
    *    strength, and denounces a weaker nation once relations sink to -40.
    *    A nation led by the player makes no choices; the player does.
    * 3. Hostility has stakes. Between two hostile neighbours the stronger
    *    raids the weaker's nearest settlement every few rounds (coins, wood
    *    and stone from its treasury and stockpile) -- a sheltered player's
    *    nation at most once every SHELTERED_RAID_EVERY_ROUNDS, and a nation
    *    the player leads only a nation it is at war with -- and if it is at least
    *    twice as strong (and no mere band: a strength of four or more) and
    *    its own nearest settlement is of at least the
    *    same tier, it lays siege to that settlement. A siege kept up for
    *    twelve rounds in a row (a minute) takes the settlement. Towns, behind
    *    their palisades, cannot be besieged, nor can the player's nation
    *    before the Town era or, after it, until the player has had warning
    *    (EraRules.isSheltered, EraRules.SiegeWarningTicks). A nation that loses its last
    *    settlement is disbanded and its members become nationless.
    *
    * A nation's strength is its members plus the members of its allies, so
    * an alliance is protection.
    *
    * Nothing here draws on randomness: the same world takes the same course.
    */
    public class Diplomacy {
        public const int ROUND_TICKS = 250;

        public const int NEIGHBOUR_FRICTION = -1;
        public const int TOUCHING_FRICTION = -2;
        public const int ALLIANCE_WARMTH = 1;
        public const int SHARED_ENEMY = 1;
        public const int DRIFT_EVERY_ROUNDS = 6;

        public const int FOUNDING_NEAR_BORDER = -10;
        public const int TRADE = 1;
        public const int POACHING = -15;
        public const int DENOUNCE = -25;
        public const int AI_DENOUNCE = -10;

        public const int AI_ALLIANCE_AT = 40;
        public const int AI_DENOUNCE_AT = -40;
        public const int AI_TRIBUTE_COINS = 20;
        public const int PLAYER_GIFT_COINS = 25;

        public const int RAID_EVERY_ROUNDS = 4;
        public const int RAID_COINS = 5;
        public const int RAID_GOODS = 5;
        // 12 rounds, a minute: at 6 (thirty seconds) a besieged player had
        // barely read the warning before the village fell (playtest 0.6.1 s01,
        // #396). Saved progress is a plain round count, so a siege in an older
        // save simply has longer to run.
        public const int SIEGE_ROUNDS_TO_CAPTURE = 12;
        public const int MIN_SIEGE_STRENGTH = 4;

        /**
        * While the player's nation is sheltered (EraRules.isSheltered), border
        * friction alone takes its relations with a neighbour no lower than
        * this: Rival, short of Hostile (and of AI_DENOUNCE_AT, so no
        * denunciation follows from it either). A new nation used to be
        * Hostile with every neighbour within four minutes of founding, and
        * raided from then on, without the player having done anything.
        * What the player does (denouncing, poaching, robbing caravans, war)
        * still counts in full.
        */
        public const int SHELTERED_FRICTION_FLOOR = -39;

        /**
        * While the player's nation is sheltered, it is raided at most once
        * in this many rounds (two minutes), by whichever hostile neighbour
        * comes first; other nations raid each other every RAID_EVERY_ROUNDS.
        */
        public const int SHELTERED_RAID_EVERY_ROUNDS = 24;

        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;
        private readonly EventProducer eventProducer;
        private readonly Player player;
        // laws, wars and truces (realm/); null in a world built without one
        private Realm realm;

        private int round = 0;
        private readonly Dictionary<EntityId, int> siegeProgress = new Dictionary<EntityId, int>();
        private readonly Dictionary<EntityId, Nation> besiegers = new Dictionary<EntityId, Nation>();
        // a sheltered nation has been raided this round (SHELTERED_RAID_EVERY_ROUNDS); only within a round
        private bool shelteredRaidedThisRound = false;
        // the named state last told to the player for each other nation, so a
        // drift see-sawing across a boundary is not told over and over;
        // transient: after a load the next change is told once more
        private readonly Dictionary<NationId, NationRelationState> toldPlayer = new Dictionary<NationId, NationRelationState>();

        public Diplomacy(NationRepository nationRepository, EntityRepository entityRepository, EventProducer eventProducer, Player player) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
            this.eventProducer = eventProducer;
            this.player = player;
        }

        public void setRealm(Realm realm) {
            this.realm = realm;
        }

        /**
        * A member of one nation trading at another's market warms the two
        * nations toward each other a little.
        */
        public static void recordTrade(NationRepository nationRepository, NationId traderNationId, Settlement market) {
            Nation trader = nationRepository.getNation(traderNationId);
            Nation host = nationRepository.getNation(market.getNationId());
            if (trader != null && host != null && trader != host) {
                nationRepository.getRelations().adjust(trader, host, TRADE);
            }
        }

        /** How many coins of gift buy how much goodwill: two points per five coins. */
        public static int giftValue(int coins) {
            return coins * 2 / 5;
        }

        public int getRound() {
            return round;
        }

        /** How many rounds in a row a siege of this settlement has held, or 0. */
        public int getSiegeProgress(Settlement settlement) {
            int progress;
            return siegeProgress.TryGetValue(settlement.getId(), out progress) ? progress : 0;
        }

        /** Who is besieging the settlement, or null. */
        public Nation getBesieger(Settlement settlement) {
            Nation nation;
            return besiegers.TryGetValue(settlement.getId(), out nation) ? nation : null;
        }

        public void update(int totalTicks) {
            if (totalTicks > 0 && totalTicks % ROUND_TICKS == 0) {
                runRound();
            }
        }

        public void runRound() {
            round++;
            Territory.updateClaims(entityRepository);
            List<Nation> nations = nationRepository.getNations();
            if (nations.Count < 2) {
                clearSieges(new HashSet<EntityId>());
                return;
            }

            for (int i = 0; i < nations.Count; i++) {
                for (int j = i + 1; j < nations.Count; j++) {
                    drift(nations[i], nations[j], nations);
                }
            }

            foreach (Nation nation in nations) {
                if (!isPawnLed(nation)) {
                    continue;
                }
                foreach (Nation other in nations) {
                    if (other != nation && exists(nation) && exists(other)) {
                        choose(nation, other);
                    }
                }
            }

            HashSet<EntityId> besiegedThisRound = new HashSet<EntityId>();
            shelteredRaidedThisRound = false;
            for (int i = 0; i < nations.Count; i++) {
                for (int j = i + 1; j < nations.Count; j++) {
                    if (exists(nations[i]) && exists(nations[j])) {
                        conflict(nations[i], nations[j], besiegedThisRound);
                    }
                }
            }
            clearSieges(besiegedThisRound);
        }

        // 1. drift ------------------------------------------------------------------------------------------

        private void drift(Nation a, Nation b, List<Nation> nations) {
            NationRelations relations = nationRepository.getRelations();
            bool allied = relations.isAllied(a, b);
            int delta = 0;

            Frontier frontier = findFrontier(a, b);
            if (frontier != null && frontier.distance <= Territory.NEIGHBOUR_DISTANCE && !allied) {
                delta += frontier.gap <= Territory.TOUCHING_GAP ? TOUCHING_FRICTION : NEIGHBOUR_FRICTION;
            }
            if (allied) {
                delta += ALLIANCE_WARMTH;
            }
            int sharedEnemies = 0;
            foreach (Nation c in nations) {
                if (c == a || c == b) {
                    continue;
                }
                if (relations.getState(a, c) <= NationRelationState.RIVAL && relations.getState(b, c) <= NationRelationState.RIVAL) {
                    delta += SHARED_ENEMY;
                    sharedEnemies++;
                }
            }
            if (delta == 0 && round % DRIFT_EVERY_ROUNDS == 0) {
                int score = relations.getScore(a, b);
                delta = score > 0 ? -1 : (score < 0 ? 1 : 0);
            }
            if (realm != null) {
                // nations at war cannot warm, nations under a truce cannot grate
                delta = realm.adjustDrift(a, b, delta);
            }
            int before = relations.getScore(a, b);
            if (delta < 0 && (a.isSheltered() || b.isSheltered())) {
                // friction alone does not make a young player's nation hostile
                delta = Math.Max(delta, Math.Min(0, SHELTERED_FRICTION_FLOOR - before));
            }
            // the only thing that sours a drift is the border; warmth is an alliance, a shared enemy or time
            RelationCause cause = delta < 0 ? RelationCause.Border
                : allied ? RelationCause.Alliance
                : sharedEnemies > 0 ? RelationCause.SharedEnemy
                : RelationCause.Easing;
            change(a, b, delta, cause);
            Nation other = isPlayersNation(a) ? b : (isPlayersNation(b) ? a : null);
            if (other != null && delta < 0 && before >= 0 && relations.getScore(a, b) < 0) {
                player.getStatus().update(cause.tellPlayer(other) + ": relations will sour while your settlements stand this close.");
            }
        }

        // 2. pawn-led choices -------------------------------------------------------------------------------

        private void choose(Nation self, Nation other) {
            NationRelations relations = nationRepository.getRelations();
            int score = relations.getScore(self, other);
            NationRelationState state = relations.getState(self, other);

            if (state != NationRelationState.ALLIED && score >= AI_ALLIANCE_AT && isPawnLed(other)) {
                relations.formAlliance(self, other);
                return;
            }

            if (state <= NationRelationState.RIVAL && areNeighbours(self, other)
                    && getStrength(self) * 2 <= getStrength(other)
                    && round % RAID_EVERY_ROUNDS == 0) {
                Settlement treasury = findRichestSettlement(self);
                if (treasury != null && treasury.getFunds() >= AI_TRIBUTE_COINS) {
                    treasury.removeFunds(AI_TRIBUTE_COINS);
                    giveCoins(other, AI_TRIBUTE_COINS);
                    if (isPlayersNation(other)) {
                        player.getStatus().update(self.getName() + " sent " + AI_TRIBUTE_COINS + " coins of tribute to your treasury.");
                    }
                    change(self, other, giftValue(AI_TRIBUTE_COINS));
                    return;
                }
            }

            if (score <= AI_DENOUNCE_AT && !relations.hasDenounced(self, other) && getStrength(self) > getStrength(other)
                    && (realm == null || !realm.hasTruce(self, other))) {
                relations.markDenounced(self, other);
                if (isPlayersNation(other)) {
                    player.getStatus().update(self.getName() + " has denounced your nation.");
                }
                change(self, other, AI_DENOUNCE);
            }
        }

        // 3. conflict ---------------------------------------------------------------------------------------

        private void conflict(Nation a, Nation b, HashSet<EntityId> besiegedThisRound) {
            if (nationRepository.getRelations().getState(a, b) != NationRelationState.HOSTILE) {
                return;
            }
            if (realm != null && realm.hasTruce(a, b)) {
                return;
            }
            Frontier frontier = findFrontier(a, b);
            if (frontier == null || frontier.distance > Territory.NEIGHBOUR_DISTANCE) {
                return;
            }
            int strengthA = getStrength(a);
            int strengthB = getStrength(b);
            if (strengthA == strengthB) {
                return;
            }
            bool aAttacks = strengthA > strengthB;
            Nation attacker = aAttacks ? a : b;
            Nation defender = aAttacks ? b : a;
            Settlement base_ = aAttacks ? frontier.fromA : frontier.fromB;
            Settlement target = aAttacks ? frontier.fromB : frontier.fromA;
            // conscripts (realm/Laws) can make a nation too costly to attack at all
            if (getStrength(attacker) <= getDefence(defender)) {
                return;
            }
            // a nation the player leads makes no choices of its own: its people
            // take up arms only against a nation the player has declared war on
            if (isLedByPlayer(attacker) && (realm == null || !realm.isAtWar(attacker, defender))) {
                return;
            }

            if (round % RAID_EVERY_ROUNDS == 0 && mayRaid(defender)) {
                if (raid(attacker, defender, target, base_) && defender.isSheltered()) {
                    shelteredRaidedThisRound = true;
                }
            }

            if (canBesiege(attacker, defender, base_, target) && mayBeginSiege(attacker, defender, target)
                    && !besiegedThisRound.Contains(target.getId())) {
                besiegedThisRound.Add(target.getId());
                besiege(attacker, defender, target);
            }
        }

        /**
        * Whether the attacker is strong enough to lay siege to the target: a
        * strength of at least four, at least twice the defender's, a base of at least the target's
        * tier, and a target that is not a walled town -- nor held by a young
        * nation the player leads, which its era shelters (EraRules).
        */
        public bool canBesiege(Nation attacker, Nation defender, Settlement base_, Settlement target) {
            return target.getTier() != SettlementTier.TOWN
                && !defender.isSheltered()
                && getStrength(attacker) >= MIN_SIEGE_STRENGTH
                && getStrength(attacker) >= 2 * getDefence(defender)
                && base_.getTier() >= target.getTier();
        }

        /**
        * Whether a siege of the target may begin or go on: not a new one
        * while the player's nation is being given warning that its shelter
        * has ended (EraRules.SiegeWarningTicks). A siege already under way
        * goes on.
        */
        public bool mayBeginSiege(Nation attacker, Nation defender, Settlement target) {
            return !defender.isForewarned() || getBesieger(target) == attacker;
        }

        /**
        * Whether a raid on this nation may come this round: any raid round,
        * but a sheltered nation (the player's, before the Town era) is raided
        * at most once every SHELTERED_RAID_EVERY_ROUNDS.
        */
        private bool mayRaid(Nation victim) {
            if (!victim.isSheltered()) {
                return true;
            }
            return round % SHELTERED_RAID_EVERY_ROUNDS == 0 && !shelteredRaidedThisRound;
        }

        /** Carries off what it can; returns false if there was nothing to take. */
        private bool raid(Nation raider, Nation victim, Settlement target, Settlement home) {
            Inventory stock = target.getInventory();
            // open borders (realm/Laws) let raiders carry off more
            int multiplier = realm == null ? 1 : realm.raidLootMultiplier(victim);
            int coins = Math.Min(RAID_COINS * multiplier, stock.getNumItems(ItemType.COIN));
            int wood = Math.Min(RAID_GOODS * multiplier, stock.getNumItems(ItemType.WOOD));
            int stone = Math.Min(RAID_GOODS * multiplier, stock.getNumItems(ItemType.STONE));
            if (coins + wood + stone == 0) {
                return false;
            }
            List<string> parts = new List<string>();
            if (coins > 0) {
                stock.removeItem(ItemType.COIN, coins);
                home.getInventory().addItem(ItemType.COIN, coins);
                parts.Add(coins + " coins");
            }
            if (wood > 0) {
                stock.removeItem(ItemType.WOOD, wood);
                home.getInventory().addItem(ItemType.WOOD, wood);
                parts.Add(wood + " wood");
            }
            if (stone > 0) {
                stock.removeItem(ItemType.STONE, stone);
                home.getInventory().addItem(ItemType.STONE, stone);
                parts.Add(stone + " stone");
            }
            string loot = string.Join(", ", parts);
            eventProducer.produceNationRaidEvent(raider, victim, target, loot);
            if (isPlayersNation(victim)) {
                player.getStatus().update(raider.getName() + " raided your " + describeSettlement(target) + ": " + loot + " taken. Relations: "
                    + nationRepository.getRelations().describe(raider, victim) + "; gifts or peace end raids.");
            }
            else if (isPlayersNation(raider) && isLedByPlayer(raider)) {
                player.getStatus().update("Your people raided " + victim.getName() + " (at war with you): " + loot + " brought home.");
            }
            else if (isPlayersNation(raider)) {
                // the player joined this nation; its leader decides
                NationRelations relations = nationRepository.getRelations();
                int score = relations.getScore(raider, victim);
                player.getStatus().update("Your nation's raiders struck " + victim.getName() + " ("
                    + NationRelationStates.getName(relations.getState(raider, victim)) + ", " + (score > 0 ? "+" : "") + score + "): "
                    + loot + " brought home.");
            }
            return true;
        }

        private void besiege(Nation attacker, Nation defender, Settlement target) {
            Nation current = getBesieger(target);
            if (current != attacker) {
                siegeProgress[target.getId()] = 0;
                besiegers[target.getId()] = attacker;
            }
            int progress = siegeProgress[target.getId()] + 1;
            siegeProgress[target.getId()] = progress;

            if (progress >= SIEGE_ROUNDS_TO_CAPTURE) {
                capture(target, defender, attacker);
                return;
            }
            target.setSiegeText("Besieged by " + attacker.getName() + " (" + progress + "/" + SIEGE_ROUNDS_TO_CAPTURE + ")");
            if (isPlayersNation(defender) && (progress == 1 || progress == SIEGE_ROUNDS_TO_CAPTURE - 2)) {
                player.getStatus().update(attacker.getName() + " is besieging your " + describeSettlement(target) + " ("
                    + progress + "/" + SIEGE_ROUNDS_TO_CAPTURE + ")! Gift, ally or grow it to hold it.");
            }
            else if (isPlayersNation(attacker) && progress == 1) {
                player.getStatus().update("Your nation is besieging " + defender.getName() + "'s " + describeSettlement(target) + ".");
            }
        }

        private void clearSieges(HashSet<EntityId> besiegedThisRound) {
            List<EntityId> lifted = new List<EntityId>();
            foreach (EntityId id in siegeProgress.Keys) {
                if (!besiegedThisRound.Contains(id)) {
                    lifted.Add(id);
                }
            }
            foreach (EntityId id in lifted) {
                Nation besieger;
                besiegers.TryGetValue(id, out besieger);
                siegeProgress.Remove(id);
                besiegers.Remove(id);
                Settlement settlement = entityRepository.getEntity(id) as Settlement;
                if (settlement != null) {
                    settlement.setSiegeText(null);
                    tellSiegeLifted(settlement, besieger);
                }
            }
        }

        /**
        * Tells the player when a siege of one of their nation's settlements
        * lifts (a capture never reaches here: it clears its own siege). The
        * Era box's red siege line just turned into the amber countdown, so
        * nothing said whether the tribute, gift or alliance had worked
        * (0.6.1/0.6.2 playtests).
        */
        private void tellSiegeLifted(Settlement settlement, Nation besieger) {
            if (player.getNationId() == null || !player.getNationId().Equals(settlement.getNationId())) {
                return;
            }
            if (besieger != null && exists(besieger)) {
                player.getStatus().update(besieger.getName() + " has lifted its siege of your " + describeSettlement(settlement) + ".");
            }
            else {
                player.getStatus().update("The siege of your " + describeSettlement(settlement) + " has ended.");
            }
        }

        /**
        * Hands a settlement to the nation that took it (#176). The losing
        * nation's people who lived there lose their homes, any stall they kept
        * there is given up, and those inside are turned out. Its treasury and
        * stockpile are the spoils. A nation left with no settlement is
        * disbanded.
        */
        public void capture(Settlement settlement, Nation from, Nation to) {
            siegeProgress.Remove(settlement.getId());
            besiegers.Remove(settlement.getId());

            from.removeSettlement(settlement.getId());
            to.addSettlement(settlement.getId());
            settlement.changeNation(to);

            Market market = settlement.getMarket();
            foreach (EntityId memberId in from.getMemberIds()) {
                Entity member = entityRepository.getEntity(memberId);
                Stall stall = market.getStall(memberId);
                if (stall != null) {
                    if (member != null && member.getType() == EntityType.PLAYER) {
                        member.getInventory().transferContentsOfInventory(stall.getInventory());
                    }
                    stall.setOwnerId(null);
                    if (from.getRole(memberId) == NationRole.MERCHANT) {
                        from.setRole(memberId, NationRole.SERF);
                    }
                }
                Pawn pawn = member as Pawn;
                if (pawn != null) {
                    if (settlement.getId().Equals(pawn.getHomeSettlementId())) {
                        pawn.setHomeSettlementId(null);
                    }
                    if (settlement.getId().Equals(pawn.getCurrentSettlementId())) {
                        turnOut(pawn, settlement, from.getColor());
                    }
                }
                Player memberPlayer = member as Player;
                if (memberPlayer != null && settlement.getId().Equals(memberPlayer.getHomeSettlementId())) {
                    memberPlayer.setHomeSettlementId(null);
                }
            }

            eventProducer.produceSettlementCaptureEvent(settlement, from, to);
            if (isPlayersNation(from)) {
                player.getStatus().update(to.getName() + " has captured your " + describeSettlement(settlement) + "!");
            }
            else if (isPlayersNation(to)) {
                player.getStatus().update("Your nation has captured a " + describeSettlement(settlement) + " from " + from.getName() + "!");
            }

            if (from.getNumberOfSettlements() == 0) {
                disband(from, to, settlement);
            }
            Territory.updateClaims(entityRepository);
        }

        /**
        * A nation with no settlement left is no nation: it is removed and
        * every member, pawn or player, is left without one. conqueror and
        * lastSettlement name the capture that ended it, when one did, so the
        * chronicle can tell how it fell.
        */
        public void disband(Nation nation, Nation conqueror = null, Settlement lastSettlement = null) {
            nationRepository.removeNation(nation);
            eventProducer.produceNationDisbandEvent(nation, conqueror, lastSettlement);
            foreach (EntityId memberId in nation.getMemberIds()) {
                Entity member = entityRepository.getEntity(memberId);
                Pawn pawn = member as Pawn;
                if (pawn != null) {
                    pawn.setNationId(null);
                    pawn.setHomeSettlementId(null);
                    pawn.setOrder(null);
                    pawn.setColor(Rgba.White);
                    Settlement inside = pawn.getCurrentSettlementId() == null ? null : entityRepository.getEntity(pawn.getCurrentSettlementId()) as Settlement;
                    if (inside != null) {
                        turnOut(pawn, inside, Rgba.White);
                    }
                }
                Player memberPlayer = member as Player;
                if (memberPlayer != null) {
                    memberPlayer.setNationId(null);
                    memberPlayer.setHomeSettlementId(null);
                    memberPlayer.setColor(Rgba.White);
                }
            }
            if (nation.isMember(player.getId())) {
                player.getStatus().update(nation.getName() + " has fallen: it has no settlements left. You are without a nation.");
            }
            else {
                player.getStatus().update(nation.getName() + " has fallen and been disbanded.");
            }
        }

        /** Puts a pawn outside the settlement it was in, at the edge of the plaza. */
        private static void turnOut(Pawn pawn, Settlement settlement, Rgba color) {
            settlement.removeCurrentlyPresentEntity(pawn.getId());
            pawn.clearCurrentSettlementId();
            pawn.clearMillTarget();
            pawn.setVisible(true);
            pawn.setPosition(settlement.getPosition() + new Vector3(SettlementLayout.PalisadeRadius + 5f, 0f, 0f));
            pawn.setColor(color);
        }

        // relations helpers ---------------------------------------------------------------------------------

        /**
        * Moves a pair's score and, when the player's nation is one of the pair
        * and the named state changed, says so on the status line -- with the
        * cause, when there is one, there and in the chronicle.
        */
        public void change(Nation a, Nation b, int delta, RelationCause cause = null) {
            NationRelations relations = nationRepository.getRelations();
            NationRelationState before = relations.getState(a, b);
            NationRelationState after = relations.adjust(a, b, delta, cause == null ? null : cause.getBetween());
            if (before == after) {
                return;
            }
            Nation other = isPlayersNation(a) ? b : (isPlayersNation(b) ? a : null);
            if (other != null) {
                NationRelationState told;
                if (cause != null && toldPlayer.TryGetValue(other.getId(), out told) && told == after) {
                    // a score see-sawing across a boundary (trade warming it, the
                    // border souring it) is told once, not every time it crosses
                    return;
                }
                toldPlayer[other.getId()] = after;
                string standing = relations.describe(a, b);
                player.getStatus().update(cause == null
                    ? "Relations with " + other.getName() + " are now " + standing + "."
                    : cause.tellPlayer(other) + ": relations are now " + standing + ".");
            }
        }

        // queries -------------------------------------------------------------------------------------------

        /** Members plus the members of every ally. */
        public int getStrength(Nation nation) {
            int strength = nation.getNumberOfMembers();
            foreach (Nation other in nationRepository.getNations()) {
                if (other != nation && nationRepository.getRelations().isAllied(nation, other)) {
                    strength += other.getNumberOfMembers();
                }
            }
            return strength;
        }

        /** What a nation can put up against an attacker: its strength plus any conscripts (realm/Laws). */
        public int getDefence(Nation nation) {
            return getStrength(nation) + (realm == null ? 0 : realm.getConscripts(nation));
        }

        public bool isPawnLed(Nation nation) {
            return entityRepository.getEntity(nation.getLeaderId()) is Pawn;
        }

        public bool areNeighbours(Nation a, Nation b) {
            Frontier frontier = findFrontier(a, b);
            return frontier != null && frontier.distance <= Territory.NEIGHBOUR_DISTANCE;
        }

        /** Whether the player leads this nation (not merely belongs to it). */
        public bool isLedByPlayer(Nation nation) {
            return nation != null && nation.getLeaderId() != null && nation.getLeaderId().Equals(player.getId());
        }

        private bool isPlayersNation(Nation nation) {
            return nation != null && player.getNationId() != null && player.getNationId().Equals(nation.getId());
        }

        private bool exists(Nation nation) {
            return nationRepository.getNation(nation.getId()) != null;
        }

        private static string describeSettlement(Settlement settlement) {
            return SettlementTiers.getDisplayName(settlement.getTier()).ToLowerInvariant();
        }

        private Settlement findRichestSettlement(Nation nation) {
            Settlement richest = null;
            foreach (Settlement settlement in getSettlements(nation)) {
                if (richest == null || settlement.getFunds() > richest.getFunds()) {
                    richest = settlement;
                }
            }
            return richest;
        }

        /**
        * Coins given to a nation go into its first settlement's treasury,
        * where its leader can withdraw them, or to its leader if it has no
        * settlement.
        */
        public void giveCoins(Nation nation, int coins) {
            List<Settlement> settlements = getSettlements(nation);
            if (settlements.Count > 0) {
                settlements[0].addFunds(coins);
                return;
            }
            Entity leader = entityRepository.getEntity(nation.getLeaderId());
            if (leader != null) {
                leader.getInventory().addItem(ItemType.COIN, coins);
            }
        }

        public List<Settlement> getSettlements(Nation nation) {
            List<Settlement> settlements = new List<Settlement>();
            foreach (EntityId id in nation.getSettlements()) {
                Settlement settlement = entityRepository.getEntity(id) as Settlement;
                if (settlement != null && !settlement.isMarkedForDeletion()) {
                    settlements.Add(settlement);
                }
            }
            return settlements;
        }

        /** The closest pair of settlements between two nations, or null if either has none. */
        public Frontier findFrontier(Nation a, Nation b) {
            Frontier best = null;
            foreach (Settlement fromA in getSettlements(a)) {
                foreach (Settlement fromB in getSettlements(b)) {
                    float distance = Territory.groundDistance(fromA.getPosition(), fromB.getPosition());
                    if (best == null || distance < best.distance) {
                        best = new Frontier(fromA, fromB, distance, distance - fromA.getClaimRadius() - fromB.getClaimRadius());
                    }
                }
            }
            return best;
        }

        /** Where two nations come closest: one settlement from each, and how far apart they and their borders are. */
        public class Frontier {
            public readonly Settlement fromA;
            public readonly Settlement fromB;
            public readonly float distance;
            public readonly float gap;

            public Frontier(Settlement fromA, Settlement fromB, float distance, float gap) {
                this.fromA = fromA;
                this.fromB = fromB;
                this.distance = distance;
                this.gap = gap;
            }
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("round", round);
            writer.writeIdIntMap("siegeProgress", siegeProgress);
            writer.beginArray("besiegers");
            foreach (KeyValuePair<EntityId, Nation> entry in besiegers) {
                writer.beginArray();
                writer.writeValue(entry.Key);
                writer.writeValue(entry.Value == null ? null : entry.Value.getId().getGuid().ToString("N"));
                writer.endArray();
            }
            writer.endArray();
        }

        /** Nations laying siege, which a save must hold even if they have since been disbanded. */
        internal IEnumerable<Nation> getBesiegingNations() {
            return besiegers.Values;
        }

        internal void load(JsonElement element, SaveReader reader) {
            round = SaveReader.getInt(element, "round");
            reader.readIdIntMap(element, "siegeProgress", siegeProgress);
            besiegers.Clear();
            foreach (JsonElement entry in SaveReader.property(element, "besiegers").EnumerateArray()) {
                besiegers[reader.entityId(entry[0])] = reader.nation(entry[1]);
            }
        }
    }
}
