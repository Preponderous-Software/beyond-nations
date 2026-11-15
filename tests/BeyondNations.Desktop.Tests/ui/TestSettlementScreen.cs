using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The settlement screen (#178), the Manage tab of the nations screen
    * (#175) and how ownership (#176) shows on them and on the command bar.
    * Read off the --stage-holdings world, with no window.
    */
    [Collection("TouchLayout")]
    public class TestSettlementScreen {

        private static Simulation staged(out Settlement outpost) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            outpost = HoldingsShowcase.stage(simulation);
            return simulation;
        }

        private static Nation own(Simulation simulation) {
            return simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());
        }

        private static Settlement home(Simulation simulation) {
            return (Settlement) simulation.getEntityRepository().getEntity(simulation.getPlayer().getHomeSettlementId());
        }

        private static List<string> section(Simulation simulation, Settlement settlement, string heading) {
            foreach (SettlementScreen.Section s in SettlementScreen.getSections(simulation, settlement)) {
                if (s.heading == heading) {
                    return s.lines;
                }
            }
            return null;
        }

        [Fact]
        public void testSections_NameTheNationTheHolderThePeopleBuildingsStoresAndMarket() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            Settlement settlement = home(simulation);

            List<SettlementScreen.Section> sections = SettlementScreen.getSections(simulation, settlement);

            Assert.Equal(new[] { "Ownership", "People", "Buildings", "Stores", "Market" }, sections.ConvertAll(s => s.heading));
            List<string> ownership = section(simulation, settlement, "Ownership");
            Assert.Equal("Nation: " + own(simulation).getName() + " (your nation).", ownership[0]);
            Assert.Equal("Held by: you, as leader of " + own(simulation).getName() + ".", ownership[1]);
            Assert.StartsWith("Residents: " + settlement.getResidentCount() + " of " + settlement.getResidentCap(), section(simulation, settlement, "People")[0]);
            Assert.EndsWith("You live here: it is your home.", section(simulation, settlement, "People")[1]);
            Assert.Equal("Town, 5 of " + BuildingCatalog.MAX_BUILDINGS + " buildings: 2 houses, 1 storehouse, 2 farms.", section(simulation, settlement, "Buildings")[0]);
            Assert.Equal("Funds: " + settlement.getFunds() + " coins.", section(simulation, settlement, "Stores")[0]);
            Assert.Contains("40 wood", section(simulation, settlement, "Stores")[1]);
            Assert.Contains("25 stone", section(simulation, settlement, "Stores")[1]);
            Assert.StartsWith(settlement.getMarket().getNumStalls() + " of " + settlement.getMarket().getMaxNumStalls() + " stalls", section(simulation, settlement, "Market")[0]);
            Assert.StartsWith("Prices (buy/sell): Apples ", section(simulation, settlement, "Market")[1]);
        }

        [Fact]
        public void testAGrantedSettlement_NamesItsHolder_AndOffersTakeBack() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            string holder = outpost.getOwnerName();

            List<string> ownership = section(simulation, outpost, "Ownership");
            SettlementScreen.Actions actions = SettlementScreen.getActions(simulation, outpost);

            Assert.Equal("Held by: " + holder + ", given it by the leader of " + own(simulation).getName() + ".", ownership[1]);
            Assert.True(actions.canGive);
            Assert.True(actions.canTakeBack);
            Assert.False(actions.canHandBack);
            Assert.False(actions.canExit);
            // the player first, and never the one already holding it
            Assert.Same(simulation.getPlayer(), actions.candidates[0]);
            Assert.DoesNotContain(actions.candidates, e => e.getId() == outpost.getOwnerId());
            Assert.Equal("Keep it yourself", SettlementScreen.describePick(actions.candidates[0], simulation.getPlayer()));
        }

        [Fact]
        public void testAForeignSettlement_HidesItsStores_AndOffersNothing() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            Nation other = new Nation("Otherland", new EntityId(), simulation.getRandom());
            simulation.getNationRepository().addNation(other);
            outpost.changeNation(other);

            SettlementScreen.Actions actions = SettlementScreen.getActions(simulation, outpost);

            Assert.Equal(new List<string> { "Only its own people see what Otherland keeps here." }, section(simulation, outpost, "Stores"));
            Assert.False(actions.canGive || actions.canTakeBack || actions.canHandBack);
            Assert.Equal("It is not your nation's.", actions.note);
            Assert.Single(SettlementScreen.getBrowsable(simulation, outpost));
        }

        [Fact]
        public void testInside_ExitIsOffered_AndTheScreenResolvesToWhereThePlayerIs() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(simulation.getPlayer(), outpost);

            Assert.True(SettlementScreen.getActions(simulation, outpost).canExit);
            Assert.Same(outpost, SettlementScreen.resolve(simulation, null));
            Assert.Same(home(simulation), SettlementScreen.resolve(simulation, home(simulation).getId()));
            Assert.Equal(2, SettlementScreen.getBrowsable(simulation, outpost).Count);
        }

        [Fact]
        public void testOutside_TheScreenResolvesToTheNearestOfTheNationsSettlements() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);

            Assert.Same(outpost, SettlementScreen.resolve(simulation, null));
        }

        [Fact]
        public void testWrap_GoesRoundBothEnds() {
            Assert.Equal(2, SettlementScreen.wrap(-1, 3));
            Assert.Equal(0, SettlementScreen.wrap(3, 3));
            Assert.Equal(0, SettlementScreen.wrap(5, 0));
        }

        [Fact]
        public void testCommandBar_OffersSettlement_InsideAnySettlement_AndTheHoldersButtons() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, outpost);

            List<string> labels = WorldCommandBar.getCommands(simulation).ConvertAll(c => c.getLabel());

            Assert.Contains(WorldCommandBar.SettlementLabel, labels);
            // the outpost is held by a member, so its funds are theirs
            Assert.DoesNotContain("Withdraw Funds", labels);

            new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository()).takeBack(player, outpost);
            labels = WorldCommandBar.getCommands(simulation).ConvertAll(c => c.getLabel());
            Assert.Contains("Withdraw Funds", labels);

            WorldCommandBar.getCommands(simulation).Find(c => c.getLabel() == WorldCommandBar.SettlementLabel).execute();
            Assert.True(SettlementScreen.consumeOpenRequest());
            Assert.Equal(outpost.getId(), SettlementScreen.getShownId());
        }

        [Fact]
        public void testEntering_OpensTheScreen_AndBackReturnsToTheWorld() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);
            SettlementScreen.consumeOpenRequest();

            Assert.False(SettlementScreens.beforeDraw(simulation, screens));
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(simulation.getPlayer(), outpost);
            Assert.True(SettlementScreens.beforeDraw(simulation, screens));

            Assert.Equal(ScreenType.SETTLEMENT, screens.getCurrent());
            Assert.Equal(outpost.getId(), SettlementScreen.getShownId());
            screens.closeSettlement();
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
            // still inside: no second opening
            Assert.False(SettlementScreens.beforeDraw(simulation, screens));
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testAWorldLoadedWithThePlayerInside_OpensOnTheWorld() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(simulation.getPlayer(), outpost);
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);
            SettlementScreen.consumeOpenRequest();
            SettlementScreens.beforeDraw(null, screens);

            Assert.False(SettlementScreens.beforeDraw(simulation, screens));
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
            SettlementScreens.beforeDraw(null, screens);
        }

        // --- the Manage tab ---

        [Fact]
        public void testManage_ListsTheSettlements_WithWhoHoldsEach() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);

            List<ManagePanel.SettlementRow> rows = ManagePanel.getSettlementRows(simulation);

            Assert.Equal(2, rows.Count);
            Assert.Same(home(simulation), rows[0].settlement);
            Assert.Contains(": held by you, ", rows[0].text);
            Assert.EndsWith("(you live here).", rows[0].text);
            Assert.Contains(": held by " + outpost.getOwnerName() + ", ", rows[1].text);
            Assert.StartsWith("You lead " + own(simulation).getName() + ":", ManagePanel.getLines(simulation)[0]);
        }

        [Fact]
        public void testManage_DisbandNeedsANationOfOne_OutsideItsSettlements() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            Nation nation = own(simulation);
            Player player = simulation.getPlayer();
            int others = nation.getNumberOfMembers() - 1;

            Assert.Equal(others + " other members still belong. Hand the rule on first.",
                ManagePanel.describeDisbandRefusal(nation, player));
            Assert.Equal("Disbanding " + nation.getName() + " ends it and razes its 2 settlements.", ManagePanel.describeDisband(nation));

            foreach (EntityId id in nation.getMemberIds()) {
                if (id != player.getId()) {
                    nation.removeMember(id);
                }
            }
            Assert.Null(ManagePanel.describeDisbandRefusal(nation, player));
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, outpost);
            Assert.Equal("Leave the settlement first.", ManagePanel.describeDisbandRefusal(nation, player));
        }

        [Fact]
        public void testManage_ForAMember_SaysOnlyTheLeaderCanManage() {
            Settlement outpost;
            Simulation simulation = staged(out outpost);
            Pawn heir = TransferLeadershipCommand.candidates(own(simulation), simulation.getEntityRepository())[0];
            new TransferLeadershipCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                .execute(simulation.getPlayer(), heir);

            Assert.Equal(heir.getName() + " leads " + own(simulation).getName() + ". Only the leader can rename it, hand it on or disband it.",
                ManagePanel.getLines(simulation)[0]);
            Assert.Equal("The leader of " + own(simulation).getName() + " decides who holds it.",
                SettlementScreen.getActions(simulation, home(simulation)).note);
        }

        [Fact]
        public void testNationScreen_TabsAreMutuallyExclusive_IncludingManage() {
            NationScreen screen = new NationScreen();
            screen.showManage(true);
            Assert.True(screen.isShowingManage());
            Assert.False(screen.isShowingLaws() || screen.isShowingMembers());
            screen.showLaws(true);
            Assert.False(screen.isShowingManage());
        }
    }
}
