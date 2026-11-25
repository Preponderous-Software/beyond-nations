using System.Collections.Generic;
using System.Linq;
using Xunit;

using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.journal;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests {

    /**
    * The picture Journal without a window: its tabs, the tiles each page
    * shows of a staged world, a tap opening a tile's card, the layout on a
    * phone held either way, and how the hosts open it.
    */
    // the tiles' words name keys or buttons by TouchLayout, which these tests switch on
    [Collection("TouchLayout")]
    public class TestJournal {

        private static Simulation freshWorld() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            return new Simulation(config);
        }

        /** A Village-era nation the player leads, with two settlements (--stage-outpost), lived in for a minute. */
        private static Simulation stagedWorld() {
            Simulation simulation = freshWorld();
            OutpostShowcase.stage(simulation);
            for (int i = 0; i < 3000; i++) {
                simulation.fixedUpdateWithPlayerAway(0.02f);
            }
            simulation.startPlayerFresh();
            return simulation;
        }

        private static JournalTile find(List<JournalTile> tiles, string id) {
            JournalTile tile = tiles.FirstOrDefault(t => t.id == id);
            Assert.True(tile != null, "no tile " + id + " among " + string.Join(", ", tiles));
            return tile;
        }

        // --- the tab model ---------------------------------------------------------

        [Fact]
        public void testTabs_AreFiveInOrder_AndTurnBothWaysWrappingRound() {
            Assert.Equal(new[] { JournalTab.Goals, JournalTab.Chronicle, JournalTab.Era, JournalTab.Inventory, JournalTab.Nation }, JournalTabs.All);
            Assert.Equal(JournalTab.Chronicle, JournalTabs.next(JournalTab.Goals));
            Assert.Equal(JournalTab.Goals, JournalTabs.next(JournalTab.Nation));
            Assert.Equal(JournalTab.Nation, JournalTabs.previous(JournalTab.Goals));
            foreach (JournalTab tab in JournalTabs.All) {
                Assert.Equal(tab, JournalTabs.previous(JournalTabs.next(tab)));
                Assert.True(JournalIcons.hasPicture(JournalTabs.icon(tab)));
                Assert.Equal(tab, JournalTabs.parse(JournalTabs.label(tab)));
            }
        }

        [Fact]
        public void testStageArgument_NamesATabAndOptionallyACard() {
            Assert.True(JournalTabs.tryParseStage("era", out JournalTab tab, out int card));
            Assert.Equal(JournalTab.Era, tab);
            Assert.Equal(0, card);
            Assert.True(JournalTabs.tryParseStage("Nation:4", out tab, out card));
            Assert.Equal(JournalTab.Nation, tab);
            Assert.Equal(4, card);
            Assert.False(JournalTabs.tryParseStage("diary", out _, out _));
            Assert.False(JournalTabs.tryParseStage("goals:0", out _, out _));
            Assert.False(JournalTabs.tryParseStage("goals:x", out _, out _));
        }

        [Fact]
        public void testScreen_TurningAndShowingATab_ClosesTheCard() {
            Simulation simulation = stagedWorld();
            JournalScreen screen = new JournalScreen();
            screen.showTab(JournalTab.Inventory);
            List<JournalTile> tiles = screen.tiles(simulation);
            screen.tap(tiles[0]);
            Assert.NotNull(screen.selected(tiles));
            screen.turn(1);
            Assert.Equal(JournalTab.Nation, screen.getTab());
            Assert.Null(screen.getSelectedId());
            screen.turn(-2);
            Assert.Equal(JournalTab.Era, screen.getTab());
        }

        [Fact]
        public void testOpen_ReturnsTheActionTheHostsTurnIntoTheJournal_OnTheTabAskedFor() {
            Assert.Equal(UiAction.OpenJournal, JournalScreen.open(JournalTab.Era));
            JournalScreen screen = new JournalScreen();
            screen.takeRequest();
            Assert.Equal(JournalTab.Era, screen.getTab());

            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);
            screens.openJournal();
            Assert.Equal(ScreenType.JOURNAL, screens.getCurrent());
            Assert.True(screens.isWorldVisible());
            Assert.False(screens.shouldAdvanceSimulation());
            // J closes it, as does Escape
            screens.journalPressed();
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
            screens.journalPressed();
            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
            // nothing opens it over a menu
            screens.goTo(ScreenType.PAUSE);
            screens.openJournal();
            screens.journalPressed();
            Assert.Equal(ScreenType.PAUSE, screens.getCurrent());
        }

        [Fact]
        public void testSwipe_IsFarEnoughAndMostlySideways() {
            Assert.True(JournalScreen.isSwipe(-90f, 10f, 70f));
            Assert.True(JournalScreen.isSwipe(90f, -20f, 70f));
            Assert.False(JournalScreen.isSwipe(40f, 0f, 70f));
            Assert.False(JournalScreen.isSwipe(90f, 80f, 70f));
        }

        // --- tiles from a staged world -----------------------------------------------

        [Fact]
        public void testGoals_TheCurrentGoalLeads_DoneOnesAreTickedAndTheRestAhead() {
            Simulation simulation = stagedWorld();
            List<JournalTile> tiles = JournalContent.goals(simulation);
            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            Assert.Equal(TileState.Current, tiles[0].state);
            Assert.Equal("goal-" + tracker.getCurrent().getId(), tiles[0].id);
            Assert.Contains(tracker.getCurrent().getTitle(), tiles[0].detailTitle);
            Assert.Equal(tracker.getObjectives().Count, tiles.Count);
            Assert.Equal(tracker.getCompletedCount(), tiles.Count(t => t.state == TileState.Done));
            Assert.All(tiles.Skip(1), t => Assert.True(t.state == TileState.Done || t.state == TileState.Locked));
            JournalContent.arcProgress(simulation, out int done, out int total);
            Assert.Equal(ObjectiveCatalog.firstArc().Count, total);
            Assert.InRange(done, 0, total);
            // a picture and a short word for every goal; no tile label is a sentence
            Assert.All(tiles, t => Assert.True(JournalIcons.hasPicture(t.icon)));
            Assert.All(tiles, t => Assert.True(t.label.Length <= 10, t.label));
        }

        [Fact]
        public void testGoals_AFreshWorld_StartsOnWoodWithItsCounterAsARing() {
            Simulation simulation = freshWorld();
            simulation.getObjectiveTracker().evaluate(simulation);
            JournalTile now = JournalContent.goals(simulation)[0];
            Assert.Equal(JournalIcon.Log, now.icon);
            Assert.Equal("Wood", now.label);
            Assert.True(now.hasProgress());
            Assert.EndsWith("/" + simulation.getObjectiveTracker().getCurrentProgress().getTarget(), now.value);
        }

        [Fact]
        public void testChronicle_NewestFirst_WithTheLineAndDateOnlyOnTheCard() {
            Simulation simulation = stagedWorld();
            List<JournalTile> tiles = JournalContent.chronicle(simulation, ChronicleScope.YourNews);
            Assert.NotEmpty(tiles);
            ChronicleFocus focus = ChronicleFocus.of(simulation);
            List<ChronicleEntry> mine = focus.select(simulation.getChronicle().getEntries());
            ChronicleEntry newest = mine[mine.Count - 1];
            Assert.Equal("chronicle-" + newest.getSequence(), tiles[0].id);
            Assert.Equal(newest.getTimestamp(), tiles[0].detailTitle);
            Assert.Equal(newest.getText(), tiles[0].detailLines[0]);
            Assert.DoesNotContain(newest.getTimestamp(), tiles[0].label);
            Assert.True(JournalContent.chronicle(simulation, ChronicleScope.AllNations).Count >= tiles.Count);
        }

        [Fact]
        public void testEra_TheLadderTheRequirementsWithTheCalendarAndTheSeason() {
            Simulation simulation = stagedWorld();
            List<JournalTile> tiles = JournalContent.era(simulation);
            Assert.Equal(TileState.Done, find(tiles, "era-camp").state);
            JournalTile village = find(tiles, "era-village");
            Assert.Equal(TileState.Current, village.state);
            Assert.Equal("2/4", village.value);
            Assert.Equal(TileState.Locked, find(tiles, "era-town").state);
            Assert.Equal(TileState.Locked, find(tiles, "era-kingdom").state);
            // every requirement for the Town, each with a ring
            foreach (EraRequirement requirement in simulation.getEraTracker().getRequirements(simulation)) {
                JournalTile need = find(tiles, "need-" + requirement.label.ToLowerInvariant());
                Assert.True(need.hasProgress());
                Assert.Equal(requirement.isMet() ? TileState.Done : TileState.Plain, need.state);
            }
            // the calendar wait: day 1 of the 7 the Town waits for
            JournalTile day = find(tiles, "need-day");
            Assert.Equal(JournalIcon.Calendar, day.icon);
            Assert.Contains(day.detailLines, line => line.StartsWith("The calendar: the Town era waits for day " + EraRules.TownDay));
            JournalTile season = find(tiles, "season");
            Assert.StartsWith("Day ", season.value);
        }

        [Fact]
        public void testInventory_TheBoxesItemsWithBigCounts_HeldOrNot() {
            Simulation simulation = stagedWorld();
            Inventory inventory = simulation.getPlayer().getInventory();
            List<JournalTile> tiles = JournalContent.inventory(simulation);
            JournalTile wood = find(tiles, "item-wood");
            Assert.Equal(inventory.getNumItems(ItemType.WOOD).ToString(), wood.value);
            Assert.Equal(JournalIcon.Log, wood.icon);
            JournalTile coins = find(tiles, "item-coin");
            Assert.Equal(inventory.getNumItems(ItemType.COIN).ToString(), coins.value);
            Assert.DoesNotContain(tiles, t => t.id == "item-axe");

            inventory.addItem(ItemType.AXE, 1);
            JournalTile axe = find(JournalContent.inventory(simulation), "item-axe");
            Assert.True(axe.hasProgress());
            Assert.Contains(axe.detailLines, line => line.EndsWith("uses left on the one in hand."));
        }

        [Fact]
        public void testNation_TheNationsNumbers_AndNoneWithoutOne() {
            Simulation simulation = stagedWorld();
            NationStanding standing = NationStanding.of(simulation);
            List<JournalTile> tiles = JournalContent.nation(simulation);
            Assert.Equal(standing.nation.getName(), find(tiles, "nation-name").label);
            Assert.Equal(standing.members.ToString(), find(tiles, "nation-members").value);
            Assert.StartsWith(standing.settlements + "/", find(tiles, "nation-settlements").value);
            Assert.Equal(standing.funds.ToString(), find(tiles, "nation-funds").value);
            Assert.Equal(JournalIcon.Crown, find(tiles, "nation-ruler").icon);

            List<JournalTile> none = JournalContent.nation(freshWorld());
            Assert.Equal(TileState.Locked, find(none, "nation-none").state);
        }

        [Fact]
        public void testHere_SaysWhoseLand_TheRegion_AndWhichWayYouFace() {
            JournalTile rival = JournalContent.hereTile("Blackburg", false, "Blackburg (Rival)", "Forest", "plenty of wood, little stone", "north-east");
            Assert.Equal(JournalIcon.Compass, rival.icon);
            Assert.True(JournalIcons.hasPicture(rival.icon));
            Assert.Equal("Blackburg", rival.label);
            Assert.Equal("NE", rival.value);
            Assert.Equal(new List<string> { "Land: Blackburg (Rival)", "Region: Forest, plenty of wood, little stone", "Facing: north-east" }, rival.detailLines);

            JournalTile wilds = JournalContent.hereTile(null, false, "wilds", "Moor", "scarce in everything", "south");
            Assert.Equal("Unclaimed", wilds.label);
            Assert.Equal("S", wilds.value);
            Assert.Equal("Land: unclaimed wilds", wilds.detailLines[0]);

            JournalTile home = JournalContent.hereTile("Aland", true, "your own land", "Plains", "", "here");
            Assert.Equal("Your land", home.label);
            Assert.Equal(TileState.Current, home.state);
            Assert.Equal("-", home.value);
            Assert.Equal("Region: Plains", home.detailLines[1]);
        }

        [Fact]
        public void testHere_IsOnTheNationTab_WithOrWithoutANation() {
            Simulation staged = stagedWorld();
            JournalTile here = find(JournalContent.nation(staged), "nation-here");
            Player player = staged.getPlayer();
            Assert.Contains("Facing: " + Compass.facing(player.getForward()), here.detailLines);
            Assert.Contains("Region: " + RegionCatalog.name(staged.getRegionMap().regionAt(player.getPosition())), here.detailLines[1]);
            string land = here.label == "Unclaimed" ? "unclaimed wilds"
                : Territory.describeFor(player, staged.getEntityRepository(), staged.getNationRepository());
            Assert.Equal("Land: " + land, here.detailLines[0]);

            find(JournalContent.nation(freshWorld()), "nation-here");
        }

        [Fact]
        public void testTap_OpensTheTilesCard_AndASecondTapClosesIt() {
            Simulation simulation = stagedWorld();
            JournalScreen screen = new JournalScreen();
            screen.showTab(JournalTab.Era);
            List<JournalTile> tiles = screen.tiles(simulation);
            JournalTile village = find(tiles, "era-village");
            screen.tap(village);
            JournalTile card = screen.selected(tiles);
            Assert.Same(village, card);
            Assert.NotEmpty(card.detailLines);
            Assert.Contains(card.detailLines, line => line.StartsWith("To reach the Town era"));
            screen.tap(village);
            Assert.Null(screen.selected(tiles));
        }

        [Fact]
        public void testTap_OnTheLayoutFindsTheTileUnderTheFinger() {
            JournalLayout layout = JournalLayout.compute(390f, 844f, true, false);
            for (int i = 0; i < 7; i++) {
                layout.tileOffset(i, out float x, out float y);
                float centreX = layout.gridX + x + layout.tile / 2f;
                float centreY = layout.gridY + y + layout.tile / 2f;
                if (centreY < layout.gridY + layout.gridHeight) {
                    Assert.Equal(i, layout.hitTest(7, 0f, centreX, centreY));
                }
            }
            Assert.Equal(-1, layout.hitTest(7, 0f, 2f, 2f));
            Assert.Equal(2, layout.tabAt(layout.tabX(2) + 5f, layout.tabsY + 5f));
        }

        [Fact]
        public void testTiles_OnATouchScreen_NameButtonsNotKeys() {
            try {
                TouchLayout.setState(TouchMode.On, true, 150f);
                Simulation simulation = stagedWorld();
                foreach (JournalTab tab in JournalTabs.All) {
                    foreach (JournalTile tile in JournalContent.tilesFor(tab, simulation)) {
                        foreach (string line in tile.detailLines.Append(tile.detailTitle).Append(tile.label)) {
                            Assert.DoesNotMatch(@"\bpress [A-Z]\b|\{[A-Za-z]+\}", line ?? "");
                        }
                    }
                }
            } finally {
                TouchLayout.reset();
            }
        }

        [Fact]
        public void testFooter_NamesKeysOnADesktop_AndNothingOnATouchScreen() {
            try {
                TouchLayout.reset();
                string desktop = JournalScreen.footerText(false);
                Assert.Contains("closes", desktop);
                Assert.DoesNotContain("{", desktop);
                Assert.Null(JournalScreen.footerText(true));
                // the page says touch is on: no keys, whatever the caller thought
                TouchLayout.setState(TouchMode.On, true, 150f);
                Assert.Null(JournalScreen.footerText(false));
            } finally {
                TouchLayout.reset();
            }
        }

        // --- layout ----------------------------------------------------------------

        [Theory]
        [InlineData(390f, 844f, true)]
        [InlineData(844f, 390f, true)]
        [InlineData(390f, 844f, false)]
        [InlineData(844f, 390f, false)]
        [InlineData(1280f, 720f, false)]
        [InlineData(960f, 540f, false)]
        public void testLayout_FitsTheScreen_WithFingertipTargets(float width, float height, bool touch) {
            foreach (bool card in new[] { false, true }) {
                JournalLayout layout = JournalLayout.compute(width, height, touch, card);
                Assert.True(layout.tabHeight >= TouchLayout.MinTarget);
                Assert.True(layout.tabWidth >= TouchLayout.MinTarget, "tab " + layout.tabWidth);
                Assert.True(layout.tabsX >= 0f && layout.tabX(4) + layout.tabWidth <= width + 0.01f);
                Assert.True(layout.tile >= TouchLayout.MinTarget, "tile " + layout.tile);
                Assert.True(layout.columns >= 3, "columns " + layout.columns);
                Assert.True(layout.columns * layout.tile + (layout.columns - 1) * layout.gap <= layout.gridWidth + 0.01f);
                Assert.True(layout.gridX >= 0f && layout.gridX + layout.gridWidth <= width);
                // at least one whole row of tiles shows, under the tabs and above the foot
                Assert.True(layout.gridY > layout.tabsY + layout.tabHeight);
                Assert.True(layout.gridHeight >= layout.tile, "grid " + layout.gridHeight + " tile " + layout.tile);
                Assert.True(layout.gridY + layout.gridHeight <= height);
                if (touch) {
                    // clear of the corner Close button
                    Assert.True(layout.tabsY >= TouchLayout.MinTarget + 16f);
                }
                if (card) {
                    Assert.True(layout.detailWidth > 0f && layout.detailHeight >= TouchLayout.MinTarget * 2f);
                    Assert.True(layout.detailX >= 0f && layout.detailX + layout.detailWidth <= width + 0.01f);
                    Assert.True(layout.detailY + layout.detailHeight <= height);
                    // the card never covers the grid
                    bool apart = layout.detailBeside
                        ? layout.detailX >= layout.gridX + layout.gridWidth
                        : layout.detailY >= layout.gridY + layout.gridHeight;
                    Assert.True(apart);
                }
            }
        }

        [Fact]
        public void testLayout_ScrollsOnlyWhenTheTilesOverflow() {
            JournalLayout layout = JournalLayout.compute(390f, 844f, true, false);
            Assert.Equal(0f, layout.maxScroll(3));
            Assert.True(layout.maxScroll(JournalContent.ChronicleTiles) > 0f);
        }

        [Fact]
        public void testIcons_EveryOneHasAPictureInsideItsBox() {
            foreach (JournalIcon icon in System.Enum.GetValues(typeof(JournalIcon))) {
                Assert.True(JournalIcons.hasPicture(icon), icon + " has no picture");
                foreach (JournalIcons.Shape shape in JournalIcons.picture(icon)) {
                    int points = shape.kind == JournalIcons.Kind.Triangle ? 6 : shape.kind == JournalIcons.Kind.Quad ? 8 : 2;
                    for (int i = 0; i < points; i++) {
                        Assert.InRange(shape.v[i], -1f, JournalIcons.Box + 1f);
                    }
                }
            }
        }
    }
}
