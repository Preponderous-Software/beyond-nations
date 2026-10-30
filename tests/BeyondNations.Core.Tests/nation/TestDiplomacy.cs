using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestDiplomacy {

        [Fact]
        public void testNeighbours_DriftApart_FasterWhenBordersTouch() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation a = world.pawnNation("Aland", 1);
            Nation b = world.pawnNation("Bshire", 1);
            Nation c = world.pawnNation("Cford", 1);
            Nation d = world.pawnNation("Dham", 1);
            world.settlement(a, 0, 0);
            world.settlement(b, 300, 0);      // neighbours, borders 160 apart
            world.settlement(c, 5000, 0);
            world.settlement(d, 5140, 0);     // camps 140 apart: borders touch

            world.rounds(5);

            Assert.Equal(5 * Diplomacy.NEIGHBOUR_FRICTION, world.relations.getScore(a, b));
            Assert.Equal(5 * Diplomacy.TOUCHING_FRICTION, world.relations.getScore(c, d));
            Assert.Equal(0, world.relations.getScore(a, c));
        }

        [Fact]
        public void testDistantNations_EaseBackTowardNeutral() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation a = world.pawnNation("Aland", 1);
            Nation b = world.pawnNation("Bshire", 1);
            world.settlement(a, 0, 0);
            world.settlement(b, 3000, 0);
            world.relations.adjust(a, b, 10);

            world.rounds(Diplomacy.DRIFT_EVERY_ROUNDS * 3);

            Assert.Equal(7, world.relations.getScore(a, b));
        }

        [Fact]
        public void testSharedEnemy_DrawsTwoNationsTogether() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation a = world.pawnNation("Aland", 1);
            Nation b = world.pawnNation("Bshire", 1);
            Nation enemy = world.pawnNation("Cford", 1);
            world.settlement(a, 0, 0);
            world.settlement(b, 3000, 0);
            world.settlement(enemy, 6000, 0);
            world.relations.adjust(a, enemy, -30);
            world.relations.adjust(b, enemy, -30);

            world.rounds(3);

            Assert.Equal(3 * Diplomacy.SHARED_ENEMY, world.relations.getScore(a, b));
        }

        [Fact]
        public void testPawnLedNations_AllyOnceRelationsAreWarm() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation a = world.pawnNation("Aland", 1);
            Nation b = world.pawnNation("Bshire", 1);
            world.settlement(a, 0, 0);
            world.settlement(b, 3000, 0);
            world.relations.adjust(a, b, Diplomacy.AI_ALLIANCE_AT);
            int eventsBefore = world.simulation.getEventRepository().getNumberOfEvents(EventType.NationRelationChange);

            world.rounds(1);

            Assert.True(world.relations.isAllied(a, b));
            Assert.Equal(eventsBefore + 1, world.simulation.getEventRepository().getNumberOfEvents(EventType.NationRelationChange));
        }

        [Fact]
        public void testPawnLedNation_NeverAlliesWithThePlayerOnItsOwn() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = world.playerNation(1);
            Nation b = world.pawnNation("Bshire", 1);
            world.settlement(own, 0, 0);
            world.settlement(b, 3000, 0);
            world.relations.adjust(own, b, 80);

            world.rounds(2);

            Assert.False(world.relations.isAllied(own, b));
        }

        [Fact]
        public void testWeakRival_PaysTributeToAStrongNeighbour() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation weak = world.pawnNation("Aland", 1);
            Nation strong = world.pawnNation("Bshire", 4);
            Settlement weakHome = world.settlement(weak, 0, 0);
            Settlement strongHome = world.settlement(strong, 300, 0);
            weakHome.addFunds(50);
            world.relations.adjust(weak, strong, -30);

            world.rounds(Diplomacy.RAID_EVERY_ROUNDS);

            Assert.Equal(50 - Diplomacy.AI_TRIBUTE_COINS, weakHome.getFunds());
            Assert.Equal(Diplomacy.AI_TRIBUTE_COINS, strongHome.getFunds());
            // four rounds of friction, one tribute
            Assert.Equal(-30 + 4 * Diplomacy.NEIGHBOUR_FRICTION + Diplomacy.giftValue(Diplomacy.AI_TRIBUTE_COINS), world.relations.getScore(weak, strong));
        }

        /**
        * "Sent 20 coins of tribute to your treasury" left the 0.6.0 playtest
        * (s04) watching the player's purse, which did not move: the coins are
        * in a settlement's Funds, and the message now says which (#375).
        */
        [Fact]
        public void testTributeToThePlayersNation_NamesTheTreasuryItWentInto() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation weak = world.pawnNation("Aland", 1);
            Nation own = world.playerNation(4);
            Settlement weakHome = world.settlement(weak, 0, 0);
            Settlement ownHome = world.settlement(own, 300, 0);
            weakHome.addFunds(50);
            world.relations.adjust(weak, own, -30);
            int coinsCarried = world.player.getInventory().getNumItems(ItemType.COIN);

            world.rounds(Diplomacy.RAID_EVERY_ROUNDS);

            Assert.Equal(Diplomacy.AI_TRIBUTE_COINS, ownHome.getFunds());
            Assert.Equal(coinsCarried, world.player.getInventory().getNumItems(ItemType.COIN));
            Assert.Contains("Aland sent " + Diplomacy.AI_TRIBUTE_COINS + " coins of tribute to the treasury of your camp (its Funds, shown while you stand in it).",
                world.player.getStatus().getStatus());
        }

        [Fact]
        public void testStrongNation_DenouncesAWeakerOneItDespises() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation strong = world.pawnNation("Aland", 3);
            Nation weak = world.playerNation(1);
            world.settlement(strong, 0, 0);
            world.settlement(weak, 3000, 0);
            world.relations.adjust(strong, weak, Diplomacy.AI_DENOUNCE_AT);

            world.rounds(1);
            int afterFirst = world.relations.getScore(strong, weak);
            world.rounds(1);

            Assert.True(world.relations.hasDenounced(strong, weak));
            Assert.Equal(Diplomacy.AI_DENOUNCE_AT + Diplomacy.AI_DENOUNCE, afterFirst);
            // once only: the second round brings no second denunciation
            Assert.Equal(afterFirst, world.relations.getScore(strong, weak));
        }

        [Fact]
        public void testHostileNeighbour_RaidsTheWeakersStockpile() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation raider = world.pawnNation("Aland", 3);
            Nation victim = world.pawnNation("Bshire", 2);
            Settlement raiderHome = world.settlement(raider, 0, 0);
            Settlement target = world.settlement(victim, 300, 0);
            target.addFunds(12);
            target.getInventory().addItem(ItemType.WOOD, 3);
            world.relations.adjust(raider, victim, -80);

            world.rounds(Diplomacy.RAID_EVERY_ROUNDS);

            Assert.Equal(12 - Diplomacy.RAID_COINS, target.getFunds());
            Assert.Equal(0, target.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(Diplomacy.RAID_COINS, raiderHome.getFunds());
            Assert.Equal(3, raiderHome.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(1, world.simulation.getEventRepository().getNumberOfEvents(EventType.NationRaid));
        }

        [Fact]
        public void testSiege_KeptUp_CapturesTheSettlementAndDisbandsANationLeftWithNone() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation attacker = world.pawnNation("Aland", 4);
            Nation defender = world.pawnNation("Bshire", 2);
            world.settlement(attacker, 0, 0);
            Settlement target = world.settlement(defender, 300, 0);
            Pawn defenderMember = (Pawn) world.entities.getEntity(defender.getMemberIds()[1]);
            defenderMember.setHomeSettlementId(target.getId());
            SettlementLife.admit(defenderMember, target);
            world.relations.adjust(attacker, defender, -80);

            world.rounds(Diplomacy.SIEGE_ROUNDS_TO_CAPTURE - 1);
            Assert.Equal(Diplomacy.SIEGE_ROUNDS_TO_CAPTURE - 1, world.diplomacy.getSiegeProgress(target));
            Assert.Equal("Besieged by Aland (" + (Diplomacy.SIEGE_ROUNDS_TO_CAPTURE - 1) + "/" + Diplomacy.SIEGE_ROUNDS_TO_CAPTURE + ")", target.getSiegeText());
            Assert.Contains("Besieged by Aland", target.getNameTagText());

            world.rounds(1);

            Assert.Equal(attacker.getId(), target.getNationId());
            Assert.Equal("Aland", target.getNationName());
            Assert.Equal(attacker.getColor(), target.getColor());
            Assert.Contains(target.getId(), attacker.getSettlements());
            Assert.Null(target.getSiegeText());
            Assert.Equal(1, world.simulation.getEventRepository().getNumberOfEvents(EventType.SettlementCapture));

            // Bshire had nothing else, so it is gone, and its people with it
            Assert.Null(world.nations.getNation(defender.getId()));
            Assert.Equal(1, world.simulation.getEventRepository().getNumberOfEvents(EventType.NationDisband));
            Assert.Null(defenderMember.getNationId());
            Assert.Null(defenderMember.getHomeSettlementId());
            Assert.False(defenderMember.isCurrentlyInSettlement());
            Assert.DoesNotContain(defenderMember.getId(), target.getCurrentlyPresentEntities());
        }

        [Fact]
        public void testSiege_LiftedWhenTheAttackerIsNoLongerStrongEnough() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation attacker = world.pawnNation("Aland", 4);
            Nation defender = world.pawnNation("Bshire", 2);
            world.settlement(attacker, 0, 0);
            Settlement target = world.settlement(defender, 300, 0);
            world.relations.adjust(attacker, defender, -80);

            world.rounds(2);
            Assert.Equal(2, world.diplomacy.getSiegeProgress(target));

            world.addMember(defender);
            world.rounds(1);

            Assert.Equal(0, world.diplomacy.getSiegeProgress(target));
            Assert.Null(target.getSiegeText());
            Assert.Equal(defender.getId(), target.getNationId());
        }

        /**
        * The Era box's red siege line just turned amber when a siege of the
        * player's village lifted, so nothing said the tribute, gift or
        * alliance had worked (0.6.1/0.6.2 playtests).
        */
        [Fact]
        public void testSiege_OfThePlayersSettlement_LiftingIsTold() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation attacker = world.pawnNation("Aland", 4);
            Nation own = world.playerNation(2);
            world.settlement(attacker, 0, 0);
            Settlement target = world.settlement(own, 300, 0);
            world.relations.adjust(attacker, own, -80);

            world.rounds(2);
            Assert.Equal(2, world.diplomacy.getSiegeProgress(target));

            world.addMember(own);
            world.rounds(1);

            Assert.Equal(0, world.diplomacy.getSiegeProgress(target));
            Assert.Equal("Aland has lifted its siege of your camp.", world.player.getStatus().getLatest());
        }

        [Fact]
        public void testSiege_BetweenOtherNations_LiftsWithoutTellingThePlayer() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation attacker = world.pawnNation("Aland", 4);
            Nation defender = world.pawnNation("Bshire", 2);
            world.settlement(attacker, 0, 0);
            world.settlement(defender, 300, 0);
            world.relations.adjust(attacker, defender, -80);

            world.rounds(2);
            world.addMember(defender);
            world.rounds(1);

            Assert.DoesNotContain("lifted", world.player.getStatus().getStatus());
        }

        /**
        * A conquered nation used to be told it "never raised so much as a
        * hut", because the epitaph went by its settlement count at disband,
        * which a capture had just made 0 (0.6.2 playtest).
        */
        [Fact]
        public void testCapture_OfALastSettlement_GivesAConquestEpitaph() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation attacker = world.pawnNation("Aland", 4);
            Nation defender = world.pawnNation("Bshire", 2);
            world.settlement(attacker, 0, 0);
            Settlement target = world.settlement(defender, 300, 0);

            world.diplomacy.capture(target, defender, attacker);

            ChronicleEntry epitaph = System.Linq.Enumerable.Single(world.simulation.getChronicle().getEntries(),
                e => e.getEventType() == EventType.NationDisband);
            Assert.Contains("Bshire", epitaph.getText());
            Assert.Contains("Aland", epitaph.getText());
            Assert.Contains("last camp", epitaph.getText());
            Assert.DoesNotContain("hut", epitaph.getText());
            Assert.True(epitaph.involvesNation(attacker.getId()));
        }

        [Fact]
        public void testTowns_CannotBeBesieged() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation attacker = world.pawnNation("Aland", 6);
            Nation defender = world.pawnNation("Bshire", 1);
            world.settlement(attacker, 0, 0, 5);
            Settlement town = world.settlement(defender, 300, 0, 5);
            world.relations.adjust(attacker, defender, -80);

            world.rounds(Diplomacy.SIEGE_ROUNDS_TO_CAPTURE + 1);

            Assert.Equal(defender.getId(), town.getNationId());
            Assert.Equal(0, world.diplomacy.getSiegeProgress(town));
        }

        [Fact]
        public void testAnAlly_CountsTowardStrength_AndCanLiftASiege() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation attacker = world.pawnNation("Aland", 4);
            Nation defender = world.pawnNation("Bshire", 2);
            Nation ally = world.pawnNation("Cford", 3);
            world.settlement(attacker, 0, 0);
            Settlement target = world.settlement(defender, 300, 0);
            world.settlement(ally, 5000, 0);
            world.relations.adjust(attacker, defender, -80);
            world.relations.adjust(defender, ally, 50);
            world.relations.formAlliance(defender, ally);

            Assert.Equal(5, world.diplomacy.getStrength(defender));
            world.rounds(Diplomacy.SIEGE_ROUNDS_TO_CAPTURE + 1);

            Assert.Equal(defender.getId(), target.getNationId());
        }

        [Fact]
        public void testCapture_FromTheNationKeepingOtherSettlements_LeavesItStanding() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation attacker = world.pawnNation("Aland", 4);
            Nation defender = world.playerNation(2);
            world.settlement(attacker, 0, 0);
            Settlement lost = world.settlement(defender, 300, 0);
            world.settlement(defender, 3000, 0);
            world.player.setHomeSettlementId(lost.getId());
            lost.getMarket().createStall();
            Stall stall = lost.getMarket().getStalls()[0];
            stall.setOwnerId(world.player.getId());
            stall.getInventory().addItem(ItemType.APPLE, 4);

            world.diplomacy.capture(lost, defender, attacker);

            Assert.NotNull(world.nations.getNation(defender.getId()));
            Assert.Equal(1, defender.getNumberOfSettlements());
            Assert.Null(world.player.getHomeSettlementId());
            Assert.Null(stall.getOwnerId());
            Assert.Equal(4, world.player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(attacker.getName() + " has captured your camp!", world.player.getStatus().getStatus());
        }

        [Fact]
        public void testPlayerTrading_AtAForeignMarket_WarmsRelations() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = world.playerNation(1);
            Nation host = world.pawnNation("Aland", 1);
            Settlement market = world.settlement(host, 0, 0);

            Diplomacy.recordTrade(world.nations, own.getId(), market);

            Assert.Equal(Diplomacy.TRADE, world.relations.getScore(own, host));
        }

        [Fact]
        public void testSimulation_RunsARoundOnItsCadence() {
            DiplomacyFixture world = new DiplomacyFixture();
            for (int i = 0; i < Diplomacy.ROUND_TICKS; i++) {
                world.simulation.fixedUpdate(1f / 50f);
            }
            Assert.Equal(1, world.diplomacy.getRound());
        }
    }
}
