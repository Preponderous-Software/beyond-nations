using System.Collections.Generic;
using System.Text.Json.Nodes;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Settlement ownership (#176) and nation management (#175): giving a
    * settlement to a member and taking it back, what the holder may then
    * do, renaming the nation and handing on its rule, and the save format
    * that keeps who holds what.
    */
    public class TestSettlementManagement {

        private static Simulation staged(out Settlement settlement) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            settlement = SettlementShowcase.stage(simulation);
            return simulation;
        }

        private static Nation own(Simulation simulation) {
            return simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());
        }

        private static Pawn firstMember(Simulation simulation) {
            return TransferLeadershipCommand.candidates(own(simulation), simulation.getEntityRepository())[0];
        }

        private static GrantSettlementCommand grants(Simulation simulation) {
            return new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository());
        }

        // --- ownership ---

        [Fact]
        public void testHolder_WithNoOneGivenIt_IsTheLeader() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);

            Assert.Null(settlement.getOwnerId());
            Assert.Equal(simulation.getPlayer().getId(), SettlementOwnership.holderOf(settlement, own(simulation)));
            Assert.False(SettlementOwnership.isGranted(settlement, own(simulation)));
            Assert.True(SettlementOwnership.mayManage(simulation.getPlayer().getId(), own(simulation), settlement));
        }

        [Fact]
        public void testGrant_GivesItToTheMember_AndNamesThemOnTheNametag() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Pawn pawn = firstMember(simulation);

            Assert.True(grants(simulation).grant(simulation.getPlayer(), settlement, pawn.getId()));

            Assert.Equal(pawn.getId(), SettlementOwnership.holderOf(settlement, own(simulation)));
            Assert.True(SettlementOwnership.isGranted(settlement, own(simulation)));
            Assert.Contains("Held by " + pawn.getName(), settlement.getNameTagText());
            Assert.Equal("The " + settlement.getDisplayName() + " is now held by " + pawn.getName() + ".", simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void testGrantedSettlement_IsManagedByItsHolder_NotTheLeader() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Player player = simulation.getPlayer();
            Pawn pawn = firstMember(simulation);
            grants(simulation).grant(player, settlement, pawn.getId());
            settlement.addFunds(20);
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);

            new WithdrawSettlementFundsCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player);

            Assert.Equal(20, settlement.getFunds());
            Assert.Equal(pawn.getName() + " holds this settlement. Take it back on the settlement screen to manage it.", player.getStatus().getStatus());
            Assert.True(SettlementOwnership.mayManage(pawn.getId(), own(simulation), settlement));
            Assert.False(SettlementOwnership.mayManage(player.getId(), own(simulation), settlement));

            Assert.True(grants(simulation).takeBack(player, settlement));
            new WithdrawSettlementFundsCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player);
            Assert.Equal(10, settlement.getFunds());
            Assert.DoesNotContain("Held by", settlement.getNameTagText());
        }

        [Fact]
        public void testOnlyTheLeader_GivesASettlementAway() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Player player = simulation.getPlayer();
            Pawn pawn = firstMember(simulation);
            new TransferLeadershipCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                .execute(player, pawn);

            Assert.False(grants(simulation).grant(player, settlement, player.getId()));
            Assert.Null(settlement.getOwnerId());
            Assert.Equal("Only the leader of " + own(simulation).getName() + " can give a settlement away.", player.getStatus().getStatus());
        }

        [Fact]
        public void testASettlementKeptByTheLeader_StaysTheirs_WhenTheRulePassesOn() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Player player = simulation.getPlayer();
            Pawn pawn = firstMember(simulation);
            grants(simulation).grant(player, settlement, player.getId());

            new TransferLeadershipCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                .execute(player, pawn);

            Assert.Equal(player.getId(), SettlementOwnership.holderOf(settlement, own(simulation)));
            Assert.True(SettlementOwnership.mayManage(player.getId(), own(simulation), settlement));
            // and the member who holds it may hand it back to the new leader
            Assert.True(grants(simulation).takeBack(player, settlement));
            Assert.Equal(pawn.getId(), SettlementOwnership.holderOf(settlement, own(simulation)));
        }

        [Fact]
        public void testAHolderWhoLeaves_GivesItBackToTheLeader() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Pawn pawn = firstMember(simulation);
            grants(simulation).grant(simulation.getPlayer(), settlement, pawn.getId());

            own(simulation).removeMember(pawn.getId());
            Assert.Equal(simulation.getPlayer().getId(), SettlementOwnership.holderOf(settlement, own(simulation)));
            Assert.True(SettlementOwnership.reconcile(settlement, own(simulation), simulation.getEntityRepository()));

            Assert.Null(settlement.getOwnerId());
            Assert.Null(settlement.getOwnerName());
            Assert.DoesNotContain("Held by", settlement.getNameTagText());
        }

        [Fact]
        public void testACapturedSettlement_IsTheNewLeaders() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            grants(simulation).grant(simulation.getPlayer(), settlement, firstMember(simulation).getId());
            Nation other = new Nation("Otherland", new EntityId(), simulation.getRandom());

            settlement.changeNation(other);

            Assert.Null(settlement.getOwnerId());
            Assert.Null(settlement.getOwnerName());
        }

        // --- rename ---

        [Fact]
        public void testRename_RenamesTheNationAndItsSettlements_AndReservesTheName() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            string old = own(simulation).getName();

            bool renamed = new RenameNationCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getNationNameGenerator())
                .execute(simulation.getPlayer(), "  High   Marches ");

            Assert.True(renamed);
            Assert.Equal("High Marches", own(simulation).getName());
            Assert.Equal("High Marches", settlement.getNationName());
            Assert.StartsWith(SettlementTiers.getDisplayName(settlement.getTier()) + " of High Marches", settlement.getNameTagText());
            Assert.Equal(old + " is now called High Marches.", simulation.getPlayer().getStatus().getStatus());
            Assert.True(simulation.getNationNameGenerator().isTaken("High Marches"));
        }

        [Theory]
        [InlineData("ab", "A nation's name needs at least 3 letters.")]
        [InlineData("Abcdefghijklmnopqrstuvwxyz", "A nation's name can be at most 24 letters.")]
        [InlineData("Bad<Name>", "A nation's name can hold only letters, digits, spaces, hyphens and apostrophes.")]
        public void testRename_RefusesABadName(string name, string refusal) {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            string old = own(simulation).getName();

            bool renamed = new RenameNationCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getNationNameGenerator())
                .execute(simulation.getPlayer(), name);

            Assert.False(renamed);
            Assert.Equal(old, own(simulation).getName());
            Assert.Equal(refusal, simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void testRename_RefusesAnotherNationsName_AndANonLeader() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Nation other = new Nation("Otherland", new EntityId(), simulation.getRandom());
            simulation.getNationRepository().addNation(other);

            Assert.Equal("Another nation is already called Otherland.", RenameNationCommand.validate("otherland", own(simulation), simulation.getNationRepository()));
            Assert.Equal(own(simulation).getName() + " already has that name.", RenameNationCommand.validate(own(simulation).getName(), own(simulation), simulation.getNationRepository()));

            new TransferLeadershipCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                .execute(simulation.getPlayer(), firstMember(simulation));
            bool renamed = new RenameNationCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getNationNameGenerator())
                .execute(simulation.getPlayer(), "Fine Name");
            Assert.False(renamed);
            Assert.Equal("Only the leader of " + own(simulation).getName() + " can rename it.", simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void testSuggest_IsAFixedWalk_OfDifferentNames() {
            List<string> names = new List<string>();
            for (int i = 0; i < 5; i++) {
                names.Add(NationNameGenerator.suggest(i));
            }

            Assert.Equal(NationNameGenerator.suggest(3), names[3]);
            Assert.Equal(5, new HashSet<string>(names).Count);
            Assert.Equal(NationNameGenerator.suggest(0), NationNameGenerator.suggest(330));
        }

        // --- handing on the rule ---

        [Fact]
        public void testTransfer_MakesTheMemberLeader_AndKeepsThePlayerAsASerf() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Player player = simulation.getPlayer();
            Pawn pawn = firstMember(simulation);
            Nation nation = own(simulation);
            int members = nation.getNumberOfMembers();

            bool handed = new TransferLeadershipCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                .execute(player, pawn);

            Assert.True(handed);
            Assert.Equal(pawn.getId(), nation.getLeaderId());
            Assert.Equal(NationRole.LEADER, nation.getRole(pawn.getId()));
            Assert.Equal(NationRole.SERF, nation.getRole(player.getId()));
            Assert.Equal(members, nation.getNumberOfMembers());
            Assert.Equal(nation.getId(), player.getNationId());
            Assert.Equal(pawn.getName() + " now leads " + nation.getName() + ". You remain one of its people.", player.getStatus().getStatus());
            // the settlement nobody was given goes with the crown
            Assert.Equal(pawn.getId(), SettlementOwnership.holderOf(settlement, nation));
            // and the world plays on
            for (int i = 0; i < 300; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            Assert.Equal(nation.getId(), player.getNationId());
        }

        [Fact]
        public void testTransfer_RefusesSomeoneOutsideTheNation() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Pawn stranger = new Pawn(settlement.getPosition(), "Stranger Danger", simulation.getRandom());
            simulation.getEntityRepository().addEntity(stranger);

            bool handed = new TransferLeadershipCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                .execute(simulation.getPlayer(), stranger);

            Assert.False(handed);
            Assert.Equal(simulation.getPlayer().getId(), own(simulation).getLeaderId());
        }

        // --- saving ---

        [Fact]
        public void testWhoHoldsASettlement_RoundTripsThroughASave() {
            Settlement settlement;
            Simulation simulation = staged(out settlement);
            Pawn pawn = firstMember(simulation);
            grants(simulation).grant(simulation.getPlayer(), settlement, pawn.getId());

            Simulation loaded = SaveGame.decode(SaveGame.encode(simulation, "test"));

            Settlement copy = (Settlement) loaded.getEntityRepository().getEntity(settlement.getId());
            Assert.Equal(pawn.getId(), copy.getOwnerId());
            Assert.Equal(pawn.getName(), copy.getOwnerName());
            Assert.Null(WorldComparer.difference(simulation, loaded));
        }

        [Fact]
        public void testMigration6To7_GivesEverySettlementToItsLeader_AndLeavesOtherEntitiesAlone() {
            JsonObject body = new JsonObject {
                ["formatVersion"] = 6,
                ["entities"] = new JsonArray {
                    new JsonObject { ["type"] = "SETTLEMENT", ["id"] = "a" },
                    new JsonObject { ["type"] = "PAWN", ["id"] = "b" },
                },
                ["orphanEntities"] = new JsonArray {
                    new JsonObject { ["type"] = "SETTLEMENT", ["id"] = "c" },
                },
            };

            JsonObject migrated = SaveMigrations.migrate(body, 6, 7);

            Assert.Equal(7, (int) migrated["formatVersion"]);
            JsonObject settlement = (JsonObject) migrated["entities"][0];
            Assert.True(settlement.ContainsKey("ownerId"));
            Assert.Null(settlement["ownerId"]);
            Assert.True(settlement.ContainsKey("ownerName"));
            Assert.False(((JsonObject) migrated["entities"][1]).ContainsKey("ownerId"));
            Assert.True(((JsonObject) migrated["orphanEntities"][0]).ContainsKey("ownerId"));
        }

        [Fact]
        public void testTheVersion6Fixture_FromBeforeOwnership_LoadsWithEverySettlementHeldByItsLeader() {
            byte[] file = System.IO.File.ReadAllBytes(SaveFixtures.pathFor(6));
            Assert.Equal(6, SaveGame.check(file).FormatVersion);
            Simulation loaded = SaveGame.decode(file);
            int settlements = 0;
            foreach (Entity entity in loaded.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                Settlement settlement = (Settlement) entity;
                Assert.Null(settlement.getOwnerId());
                Assert.Null(settlement.getOwnerName());
                settlements++;
            }
            Assert.True(settlements > 0);
            for (int i = 0; i < 1500; i++) {
                loaded.fixedUpdate(1f / 50f);
            }
            Assert.Null(WorldComparer.difference(loaded, SaveGame.decode(SaveGame.encode(loaded, "test"))));
        }
    }
}
