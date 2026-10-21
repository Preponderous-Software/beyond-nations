using System.Collections.Generic;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Each law's effect, its cost, and the delay before it comes in
    * (realm/Laws, realm/Realm). The world is DiplomacyFixture's, never
    * ticked, and law rounds are run by calling Realm.update directly.
    */
    public class TestLaws {
        private readonly DiplomacyFixture world = new DiplomacyFixture();
        private Realm realm { get { return world.simulation.getRealm(); } }
        private int round = 0;

        /** Runs the next law round (realm.update at the next multiple of LAW_ROUND_TICKS). */
        private void lawRound() {
            round++;
            realm.update(round * Realm.LAW_ROUND_TICKS);
        }

        /** Proclaims a law and runs law rounds until it is in force. */
        private void enact(Nation nation, LawType law, int level) {
            realm.proclaim(nation, law, level);
            while (realm.getLevel(nation, law) != level) {
                lawRound();
            }
        }

        private List<Pawn> pawnsOf(Nation nation) {
            List<Pawn> pawns = new List<Pawn>();
            foreach (EntityId id in nation.getMemberIds()) {
                Pawn pawn = world.entities.getEntity(id) as Pawn;
                if (pawn != null) {
                    pawns.Add(pawn);
                }
            }
            return pawns;
        }

        /** Exactly this many coins: a pawn starts with some of its own. */
        private static void giveCoins(Pawn pawn, int coins) {
            Inventory inventory = pawn.getInventory();
            inventory.removeItem(ItemType.COIN, inventory.getNumItems(ItemType.COIN));
            inventory.addItem(ItemType.COIN, coins);
        }

        private string status() {
            return world.player.getStatus().getStatus();
        }

        [Fact]
        public void testALawComesIn_OnlyAfterTheDelay_AndIsChronicled() {
            Nation own = world.playerNation(3);
            world.settlement(own, 0, 0);

            new SetLawCommand(world.nations, realm).execute(world.player, LawType.TAXES, 1);
            Assert.Equal("Taxes: Low proclaimed. It takes effect in 30 seconds.", status());
            Assert.Equal(1, realm.getLaws(own).getPending(LawType.TAXES));

            // two rounds in, 1,000 of the 1,500 ticks have passed: not yet
            lawRound();
            lawRound();
            Assert.Equal(0, realm.getLevel(own, LawType.TAXES));
            lawRound();
            Assert.Equal(1, realm.getLevel(own, LawType.TAXES));
            Assert.Equal(-1, realm.getLaws(own).getPending(LawType.TAXES));
            Assert.Equal("Taxes in " + own.getName() + " rose to Low.", status());
            ChronicleEntry last = world.simulation.getChronicle().getRecent(1)[0];
            Assert.Equal(EventType.Statecraft, last.getEventType());
            Assert.Equal("Taxes in " + own.getName() + " rose to Low.", last.getText());
        }

        [Fact]
        public void testChoosingTheLawInForce_WithdrawsAPendingChange() {
            Nation own = world.playerNation(1);
            realm.proclaim(own, LawType.RATIONING, 1);

            new SetLawCommand(world.nations, realm).execute(world.player, LawType.RATIONING, 0);

            Assert.Equal("The change to rationing is withdrawn; they stay Off.", status());
            for (int i = 0; i < 4; i++) {
                lawRound();
            }
            Assert.Equal(0, realm.getLevel(own, LawType.RATIONING));
        }

        [Fact]
        public void testOnlyTheLeader_MakesTheLaws() {
            Nation theirs = world.pawnNation("Aland", 2);
            theirs.addMember(world.player.getId());
            world.player.setNationId(theirs.getId());

            new SetLawCommand(world.nations, realm).execute(world.player, LawType.TAXES, 2);

            Assert.Equal("Only the leader of Aland can make its laws.", status());
            Assert.Null(realm.getLaws(theirs));
        }

        [Fact]
        public void testHighTaxes_FillTheTreasury_AndCostLoyaltyEveryRound() {
            Nation own = world.playerNation(3);
            Settlement treasury = world.settlement(own, 0, 0);
            foreach (Pawn pawn in pawnsOf(own)) {
                giveCoins(pawn, 10);
            }
            int fundsBefore = treasury.getFunds();

            enact(own, LawType.TAXES, 2);

            // two members, three coins each, in the round the law came in
            Assert.Equal(fundsBefore + 6, treasury.getFunds());
            Assert.Equal(6, realm.getLaws(own).getLastTaxCollected());
            foreach (Pawn pawn in pawnsOf(own)) {
                Assert.Equal(7, pawn.getInventory().getNumItems(ItemType.COIN));
                Assert.Equal(-1, PawnRelations.getRelationship(pawn, world.player));
            }
            lawRound();
            Assert.Equal(fundsBefore + 12, treasury.getFunds());
            Assert.Equal(12, realm.getLaws(own).getTotalTaxCollected());
            Assert.Equal(-2, PawnRelations.getRelationship(pawnsOf(own)[0], world.player));
        }

        [Fact]
        public void testLowTaxes_TakeOneCoin_AndLoyaltyOnlyEveryThirdRound() {
            Nation own = world.playerNation(2);
            Settlement treasury = world.settlement(own, 0, 0);
            Pawn member = pawnsOf(own)[0];
            giveCoins(member, 10);
            int fundsBefore = treasury.getFunds();

            enact(own, LawType.TAXES, 1);   // three rounds: in force on the third
            lawRound();
            lawRound();

            Assert.Equal(fundsBefore + 3, treasury.getFunds());
            Assert.Equal(7, member.getInventory().getNumItems(ItemType.COIN));
            // the third round cost a point; the fourth and fifth did not, the sixth will
            Assert.Equal(-1, PawnRelations.getRelationship(member, world.player));
            lawRound();
            Assert.Equal(-2, PawnRelations.getRelationship(member, world.player));
        }

        [Fact]
        public void testTaxes_PutImmigrantsOff_AndOpenBorders_DrawThemIn() {
            Nation own = world.playerNation(1);
            Settlement settlement = world.settlement(own, 0, 0);
            int baseTicks = PopulationGrowth.IMMIGRATION_COOLDOWN_TICKS;
            Assert.Equal(baseTicks, realm.immigrationCooldown(settlement, baseTicks));

            enact(own, LawType.OPEN_BORDERS, 1);
            Assert.Equal(baseTicks / 2, realm.immigrationCooldown(settlement, baseTicks));
            enact(own, LawType.TAXES, 1);
            Assert.Equal(baseTicks, realm.immigrationCooldown(settlement, baseTicks));
            enact(own, LawType.TAXES, 2);
            Assert.Equal(-1, realm.immigrationCooldown(settlement, baseTicks));
        }

        [Fact]
        public void testAMemberPushedTooFar_Deserts() {
            Nation own = world.playerNation(3);
            world.settlement(own, 0, 0);
            Pawn bitter = pawnsOf(own)[0];
            bitter.decreaseRelationship(world.player, -Realm.DESERT_AT - 1);   // -14
            Pawn content = pawnsOf(own)[1];
            content.increaseRelationship(world.player, 20);

            enact(own, LawType.TAXES, 2);

            Assert.False(own.isMember(bitter.getId()));
            Assert.Null(bitter.getNationId());
            Assert.True(own.isMember(content.getId()));
            Assert.Equal(1, realm.getLaws(own).getDeserters());
            Assert.Equal(bitter.getName() + " deserted " + own.getName() + ", unable to bear its laws.", status());
        }

        [Fact]
        public void testRationing_SlowsHunger_AndCostsMoraleEveryOtherRound() {
            Nation own = world.playerNation(2);
            Pawn member = pawnsOf(own)[0];
            member.setEnergy(50f);
            float handedBack = member.getMetabolism() * Realm.LAW_ROUND_TICKS * Laws.RATIONING_SAVED_PERCENT / 100f;

            enact(own, LawType.RATIONING, 1);   // in force on round 3: odd, no morale lost

            Assert.Equal(50f + handedBack, member.getEnergy(), 3);
            Assert.Equal(0, PawnRelations.getRelationship(member, world.player));
            lawRound();
            Assert.Equal(50f + 2 * handedBack, member.getEnergy(), 3);
            Assert.Equal(-1, PawnRelations.getRelationship(member, world.player));
        }

        [Fact]
        public void testOpenBorders_BringTolls_AndThievesEveryThirdRound() {
            Nation own = world.playerNation(1);
            Settlement settlement = world.settlement(own, 0, 0);
            settlement.getInventory().addItem(ItemType.WOOD, 10);
            int funds = settlement.getFunds();

            enact(own, LawType.OPEN_BORDERS, 1);   // round 3, a thieves' round
            Assert.Equal(funds + Laws.OPEN_BORDERS_TOLL - Laws.OPEN_BORDERS_THEFT_AMOUNT, settlement.getFunds());
            Assert.Equal(10 - Laws.OPEN_BORDERS_THEFT_AMOUNT, settlement.getInventory().getNumItems(ItemType.WOOD));
            Assert.StartsWith("Thieves slipped through the open borders of " + own.getName(), status());

            lawRound();
            Assert.Equal(funds + 2 * Laws.OPEN_BORDERS_TOLL - Laws.OPEN_BORDERS_THEFT_AMOUNT, settlement.getFunds());
        }

        [Fact]
        public void testOpenBorders_TradeWarmsOtherNations_EveryFourthRound() {
            Nation own = world.playerNation(1);
            Nation other = world.pawnNation("Aland", 1);
            world.settlement(own, 0, 0);

            enact(own, LawType.OPEN_BORDERS, 1);   // round 3
            Assert.Equal(0, world.relations.getScore(own, other));
            lawRound();                            // round 4
            Assert.Equal(Diplomacy.TRADE, world.relations.getScore(own, other));
        }

        [Fact]
        public void testOpenBorders_LetRaidersCarryOffDouble() {
            int[] taken = new int[2];
            for (int open = 0; open < 2; open++) {
                DiplomacyFixture fixture = new DiplomacyFixture();
                Nation own = fixture.playerNation(1);
                Nation raider = fixture.pawnNation("Aland", 3);
                Settlement target = fixture.settlement(own, 0, 0);
                fixture.settlement(raider, 300, 0);
                target.getInventory().addItem(ItemType.COIN, 40);
                int before = target.getFunds();
                fixture.relations.adjust(own, raider, -70);
                if (open == 1) {
                    Realm openRealm = fixture.simulation.getRealm();
                    openRealm.proclaim(own, LawType.OPEN_BORDERS, 1);
                    openRealm.update(Realm.LAW_DELAY_TICKS);
                }
                int stolen = before;
                fixture.rounds(Diplomacy.RAID_EVERY_ROUNDS);   // one raid
                taken[open] = stolen - target.getFunds() + (open == 1 ? Laws.OPEN_BORDERS_TOLL : 0);
            }
            Assert.Equal(Diplomacy.RAID_COINS, taken[0]);
            Assert.Equal(Diplomacy.RAID_COINS * Laws.OPEN_BORDERS_RAID_MULTIPLIER, taken[1]);
        }

        [Fact]
        public void testConscription_HoldsOffARaider_ThatWouldOtherwiseRaid() {
            int[] taken = new int[2];
            for (int levy = 0; levy < 2; levy++) {
                DiplomacyFixture fixture = new DiplomacyFixture();
                Nation own = fixture.playerNation(3);          // strength 3; two pawns make one conscript
                Nation raider = fixture.pawnNation("Aland", 4); // strength 4
                Settlement target = fixture.settlement(own, 0, 0);
                fixture.settlement(raider, 300, 0);
                target.getInventory().addItem(ItemType.COIN, 40);
                fixture.relations.adjust(own, raider, -70);
                Realm levyRealm = fixture.simulation.getRealm();
                if (levy == 1) {
                    levyRealm.proclaim(own, LawType.CONSCRIPTION, 1);
                    levyRealm.update(Realm.LAW_DELAY_TICKS);
                    Assert.Equal(1, levyRealm.getConscripts(own));
                    Assert.Equal(4, fixture.diplomacy.getDefence(own));
                }
                int before = target.getFunds();
                fixture.rounds(Diplomacy.RAID_EVERY_ROUNDS);
                taken[levy] = before - target.getFunds();
            }
            Assert.Equal(Diplomacy.RAID_COINS, taken[0]);
            Assert.Equal(0, taken[1]);
        }

        [Fact]
        public void testConscripts_GatherLess() {
            Nation own = world.playerNation(2);
            Pawn conscript = pawnsOf(own)[0];
            enact(own, LawType.CONSCRIPTION, 1);

            conscript.getInventory().addItem(ItemType.WOOD, 5);
            conscript.getInventory().addItem(ItemType.STONE, 7);
            realm.afterGather(conscript, 0, 0);

            // 40% of each haul, rounded down, goes to drill
            Assert.Equal(3, conscript.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(5, conscript.getInventory().getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testLaws_OfANationThePlayerDoesNotLead_DoNothing() {
            Nation theirs = world.pawnNation("Aland", 3);
            Settlement treasury = world.settlement(theirs, 0, 0);
            foreach (Pawn pawn in pawnsOf(theirs)) {
                pawn.getInventory().addItem(ItemType.COIN, 10);
            }
            int before = treasury.getFunds();
            realm.proclaim(theirs, LawType.TAXES, 2);
            realm.proclaim(theirs, LawType.CONSCRIPTION, 1);
            for (int i = 0; i < 5; i++) {
                lawRound();
            }
            Assert.Equal(before, treasury.getFunds());
            Assert.Equal(0, realm.getConscripts(theirs));
            Assert.Equal(PopulationGrowth.IMMIGRATION_COOLDOWN_TICKS, realm.immigrationCooldown(treasury, PopulationGrowth.IMMIGRATION_COOLDOWN_TICKS));
        }

        [Fact]
        public void testAWorldThatNeverTouchesTheLaws_IsUnchangedByThem() {
            // the same seed run with law rounds and with the realm's hooks idle
            GameConfig config = new GameConfig();
            config.setWorldSeed(7);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < 3000; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            Assert.Equal(0, RealmTestHelp.count(simulation.getEventRepository(), EventType.Statecraft));
            Assert.Equal(3000 / Realm.LAW_ROUND_TICKS, simulation.getRealm().getLawRound());
        }
    }
}
