using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * A phone's command bar keeps one page, the buttons used most where the
    * player stands first, and the rest in the More sheet under headings
    * (#512: upright in a market town the bar paged "More 1/6", with Trade at
    * Market on the second page and a pawn's Talk on the fifth). Every
    * button stays reachable by touch, and the desktop's bar is as it was.
    */
    public class TestCommandBarGrouping {

        // ProggyClean, as TestCommandBarLayout: 7 pixels a character
        private static float measure(string label) {
            return label.Length * 7f;
        }

        private const float FrameHeight = 19f;

        private static CommandBarLayout.Settings phoneBar(float width, float height, float reserve, int maxRows) {
            WorldHud.Layout layout = WorldHud.Layout.compute(width, height, 1f);
            bool stackCentre = WorldHud.isStackCentre(width, layout, true);
            CommandBarLayout.Settings settings = WorldHud.barSettings(width, WorldHud.getBarRight(width, layout, stackCentre), height - reserve,
                layout, 1f, FrameHeight, 8f, 4f, 8f);
            settings.Left = WorldHud.getBarLeft(layout, true, stackCentre);
            settings.MaxRows = maxRows;
            return settings;
        }

        public static IEnumerable<object[]> PhoneBars() {
            // CSS pixels, with the controls' band main.js reserves; the rows the HUD leaves the bar
            yield return new object[] { 390f, 844f, 220f, 2 };
            yield return new object[] { 390f, 844f, 220f, 3 };
            yield return new object[] { 844f, 390f, 150f, 2 };
        }

        private static Simulation inMarketTown() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            TradeShowcase.stageMarket(simulation);
            return simulation;
        }

        private static Simulation inOwnCamp() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            TradeShowcase.stageCamp(simulation);
            return simulation;
        }

        private static void addPawnInReach(Simulation simulation) {
            Pawn pawn = new Pawn(simulation.getPlayer().getPosition() + new Vector3(0.5f, 0, 0), "Ada Smith", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
        }

        private static List<string> onBar(List<WorldCommand> commands, CommandBarLayout.Result placed) {
            List<string> shown = new List<string>();
            for (int i = 0; i < commands.Count; i++) {
                if (placed.Shown[i]) {
                    shown.Add(commands[i].getLabel());
                }
            }
            return shown;
        }

        [Fact]
        public void theContextIsWhereThePlayerStands() {
            Assert.Equal(WorldCommandBar.BarContext.OtherSettlement, WorldCommandBar.contextOf(inMarketTown()));
            Assert.Equal(WorldCommandBar.BarContext.OwnSettlement, WorldCommandBar.contextOf(inOwnCamp()));
            Assert.Equal(WorldCommandBar.BarContext.Wilds, WorldCommandBar.contextOf(new Simulation(new GameConfig())));
        }

        [Theory]
        [MemberData(nameof(PhoneBars))]
        public void inAMarketTown_TheMarketButtonsComeFirst_OnTheOnePageTheBarHas(float width, float height, float reserve, int maxRows) {
            Simulation simulation = inMarketTown();
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
            CommandBarLayout.Settings settings = phoneBar(width, height, reserve, maxRows);
            // it used to page: the market's own button on the second page
            Assert.True(CommandBarLayout.compute(commands, measure, settings).PageCount > 2);

            CommandBarLayout.Result placed = WorldHud.arrangeBar(ref commands, measure, settings, WorldCommandBar.BarContext.OtherSettlement);
            Assert.True(placed.isPaged());
            List<string> shown = onBar(commands, placed);
            Assert.Equal(MarketTradePanel.OpenLabel, shown[0]);
            Assert.Contains("Purchase Food", shown);
            Assert.Contains("Sell Resources", shown);
            Assert.True(shown.IndexOf("Purchase Food") < shown.IndexOf("Sell Resources"));
        }

        [Theory]
        [MemberData(nameof(PhoneBars))]
        public void inTheOwnCamp_FeedResidentsAndTheSettlementScreenComeFirst(float width, float height, float reserve, int maxRows) {
            Simulation simulation = inOwnCamp();
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
            CommandBarLayout.Result placed = WorldHud.arrangeBar(ref commands, measure, phoneBar(width, height, reserve, maxRows),
                WorldCommandBar.BarContext.OwnSettlement);
            Assert.True(placed.isPaged());
            List<string> shown = onBar(commands, placed);
            Assert.StartsWith("Feed Residents", shown[0]);
            Assert.Equal(WorldCommandBar.SettlementLabel, shown[1]);
        }

        [Fact]
        public void outInTheWorld_EnterSettlementComesFirst() {
            List<WorldCommand> commands = new List<WorldCommand> {
                new WorldCommand("Leave Nation", CommandGroup.Drastic, () => { }),
                new WorldCommand("Found Settlement", CommandGroup.Nation, () => { }),
                new WorldCommand("Enter Settlement", CommandGroup.Place, () => { }),
                new WorldCommand("Teleport Home", CommandGroup.Place, () => { }),
                new WorldCommand("Send Caravan", CommandGroup.Trade, () => { }),
                new WorldCommand("Plant Sapling", CommandGroup.Place, () => { }),
            };
            List<string> order = WorldCommandBar.frontFirst(CommandBarLayout.order(commands), WorldCommandBar.BarContext.Wilds)
                .ConvertAll(c => c.getLabel());
            Assert.Equal(new[] { "Enter Settlement", "Teleport Home", "Found Settlement", "Plant Sapling", "Send Caravan", "Leave Nation" }, order);
        }

        [Fact]
        public void theFrontOrder_KeepsEveryCommandOnce_AndNeverPutsADrasticOrPawnButtonFirst() {
            Simulation simulation = inMarketTown();
            addPawnInReach(simulation);
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
            List<WorldCommand> front = WorldCommandBar.frontFirst(commands, WorldCommandBar.BarContext.OtherSettlement);
            Assert.Equal(commands.Count, front.Count);
            foreach (WorldCommand command in commands) {
                Assert.Single(front, c => c == command);
            }
            // the rest keep the bar's own order
            List<WorldCommand> rest = front.GetRange(WorldCommandBar.frontLabels(WorldCommandBar.BarContext.OtherSettlement).Length,
                front.Count - WorldCommandBar.frontLabels(WorldCommandBar.BarContext.OtherSettlement).Length);
            List<WorldCommand> restInBarOrder = commands.FindAll(c => rest.Contains(c));
            Assert.Equal(restInBarOrder, rest);
        }

        public static IEnumerable<object[]> Places() {
            foreach (object[] bar in PhoneBars()) {
                foreach (bool market in new[] { true, false }) {
                    foreach (bool pawn in new[] { false, true }) {
                        yield return new object[] { bar[0], bar[1], bar[2], bar[3], market, pawn };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Places))]
        public void everyButton_IsOnTheBarOrInTheSheet_Once_TheSheetInTheBarsGroupOrder(float width, float height, float reserve, int maxRows,
                bool market, bool pawn) {
            Simulation simulation = market ? inMarketTown() : inOwnCamp();
            if (pawn) {
                addPawnInReach(simulation);
            }
            List<WorldCommand> desktop = WorldCommandBar.getCommands(simulation);
            List<WorldCommand> commands = new List<WorldCommand>(desktop);
            CommandBarLayout.Result placed = WorldHud.arrangeBar(ref commands, measure, phoneBar(width, height, reserve, maxRows),
                WorldCommandBar.contextOf(simulation));
            List<CommandSheet.Section> sections = CommandSheet.sections(commands, placed.Shown);

            int[] seen = new int[commands.Count];
            for (int i = 0; i < commands.Count; i++) {
                if (placed.Shown[i]) {
                    seen[i]++;
                }
            }
            CommandGroup? last = null;
            foreach (CommandSheet.Section section in sections) {
                Assert.NotEmpty(section.Commands);
                Assert.Equal(CommandSheet.titleOf(section.Group), section.Title);
                if (last.HasValue) {
                    Assert.True(section.Group > last.Value, section.Title + " is out of the bar's group order");
                }
                last = section.Group;
                int previous = -1;
                foreach (int i in section.Commands) {
                    Assert.Equal(section.Group, commands[i].getGroup());
                    Assert.True(i > previous, "the sheet reorders " + section.Title);
                    previous = i;
                    seen[i]++;
                }
            }
            // feature parity: every button the desktop's bar has is a tap or two away
            Assert.Equal(desktop.Count, commands.Count);
            for (int i = 0; i < commands.Count; i++) {
                Assert.True(seen[i] == 1, commands[i].getLabel() + " is shown " + seen[i] + " times");
                Assert.Contains(commands[i], desktop);
            }
            // the drastic buttons are last, apart, and never on the bar beside the rest
            if (sections.Count > 0 && desktop.Exists(c => c.getGroup() == CommandGroup.Drastic)) {
                Assert.Equal(CommandGroup.Drastic, sections[sections.Count - 1].Group);
            }
            for (int i = 0; i < commands.Count; i++) {
                if (placed.Shown[i]) {
                    Assert.NotEqual(CommandGroup.Drastic, commands[i].getGroup());
                }
            }
        }

        [Theory]
        [MemberData(nameof(PhoneBars))]
        public void aPawnComingIntoReach_MovesNothingOnThePhonesBar(float width, float height, float reserve, int maxRows) {
            foreach (bool market in new[] { true, false }) {
                Simulation alone = market ? inMarketTown() : inOwnCamp();
                Simulation withPawn = market ? inMarketTown() : inOwnCamp();
                addPawnInReach(withPawn);
                CommandBarLayout.Settings settings = phoneBar(width, height, reserve, maxRows);
                List<WorldCommand> a = WorldCommandBar.getCommands(alone);
                List<WorldCommand> b = WorldCommandBar.getCommands(withPawn);
                Assert.True(b.Count > a.Count);
                CommandBarLayout.Result before = WorldHud.arrangeBar(ref a, measure, settings, WorldCommandBar.contextOf(alone));
                CommandBarLayout.Result after = WorldHud.arrangeBar(ref b, measure, settings, WorldCommandBar.contextOf(withPawn));
                Assert.Equal(onBar(a, before), onBar(b, after));
                Assert.Equal(before.More, after.More);
                Assert.Equal(before.Top, after.Top);
                for (int i = 0; i < a.Count; i++) {
                    if (before.Shown[i]) {
                        int j = b.FindIndex(c => c.getLabel() == a[i].getLabel());
                        Assert.Equal(before.Buttons[i], after.Buttons[j]);
                    }
                }
            }
        }

        [Fact]
        public void theFirstPage_FillsTheRoomBesideMore_WhenAWideButtonTakesTheRowAbove() {
            // as in the camp on a real phone: Feed Residents too wide to go beside More
            CommandBarLayout.Settings settings = new CommandBarLayout.Settings {
                Left = 0f, Right = 400f, Bottom = 300f, ButtonHeight = 40f, SpacingX = 8f, SpacingY = 4f,
                TextPadding = 8f, MinButtonWidth = 40f, MaxRows = 2
            };
            List<WorldCommand> commands = new List<WorldCommand> {
                new WorldCommand(new string('F', 48), CommandGroup.Place, () => { }),
                new WorldCommand("Settlement", CommandGroup.Place, () => { }),
                new WorldCommand("Exit Settlement", CommandGroup.Place, () => { }),
                new WorldCommand("Withdraw Funds", CommandGroup.Nation, () => { }),
                new WorldCommand(new string('G', 50), CommandGroup.Nation, () => { }),
                new WorldCommand("Leave Nation", CommandGroup.Drastic, () => { }),
            };
            CommandBarLayout.Result placed = CommandBarLayout.firstPage(commands, measure, settings);
            float bottomRow = settings.Bottom - settings.ButtonHeight;
            Assert.True(placed.Shown[0]);
            Assert.True(placed.Buttons[0].Y < bottomRow);
            // Settlement and Exit go beside More, on the row it would have had alone
            Assert.True(placed.Shown[1] && placed.Shown[2]);
            Assert.Equal(bottomRow, placed.Buttons[1].Y);
            Assert.Equal(bottomRow, placed.Buttons[2].Y);
            Assert.True(placed.Buttons[1].X >= placed.More.Right);
            // what fits nowhere is left for the sheet, and so are the drastic buttons
            Assert.False(placed.Shown[4]);
            Assert.False(placed.Shown[5]);
            Assert.True(placed.isPaged());
            for (int i = 0; i < commands.Count; i++) {
                for (int j = i + 1; j < commands.Count; j++) {
                    if (placed.Shown[i] && placed.Shown[j]) {
                        Assert.False(placed.Buttons[i].overlaps(placed.Buttons[j]));
                    }
                }
                if (placed.Shown[i]) {
                    Assert.False(placed.Buttons[i].overlaps(placed.More));
                    Assert.True(placed.Buttons[i].Right <= settings.Right + 0.01f);
                }
            }
            // a button added at the end moves none before it
            List<WorldCommand> more = new List<WorldCommand>(commands) { new WorldCommand("Eat Apple (+15)", CommandGroup.Craft, () => { }) };
            CommandBarLayout.Result again = CommandBarLayout.firstPage(more, measure, settings);
            for (int i = 0; i < commands.Count; i++) {
                Assert.Equal(placed.Shown[i], again.Shown[i]);
                Assert.Equal(placed.Buttons[i], again.Buttons[i]);
            }
        }

        [Fact]
        public void theDesktopsBar_IsNotReordered_AndHasNoSheet() {
            Simulation simulation = inMarketTown();
            addPawnInReach(simulation);
            WorldHud.Layout layout = WorldHud.Layout.compute(1280f, 720f, 1f);
            CommandBarLayout.Settings settings = WorldHud.barSettings(1280f, WorldHud.getBarRight(1280f, layout, false), 720f,
                layout, 1f, FrameHeight, 8f, 4f, 8f);
            List<WorldCommand> original = WorldCommandBar.getCommands(simulation);
            List<WorldCommand> commands = original;
            CommandBarLayout.Result placed = WorldHud.arrangeBar(ref commands, measure, settings, WorldCommandBar.BarContext.OtherSettlement);
            Assert.Same(original, commands);
            Assert.False(placed.isPaged());
            CommandBarLayout.Result plain = CommandBarLayout.compute(original, measure, settings);
            Assert.Equal(plain.Buttons, placed.Buttons);
            Assert.All(placed.Shown, Assert.True);
            Assert.Empty(CommandSheet.sections(commands, placed.Shown));
        }

        [Theory]
        [MemberData(nameof(PhoneBars))]
        public void theSheetsButtons_FitItsWidth_AndOverlapNothing(float width, float height, float reserve, int maxRows) {
            Simulation simulation = inMarketTown();
            addPawnInReach(simulation);
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
            CommandBarLayout.Settings settings = phoneBar(width, height, reserve, maxRows);
            CommandBarLayout.Result placed = WorldHud.arrangeBar(ref commands, measure, settings, WorldCommandBar.BarContext.OtherSettlement);
            List<CommandSheet.Section> sections = CommandSheet.sections(commands, placed.Shown);
            CommandBarLayout.Settings sheet = settings;
            sheet.Left = 0f;
            sheet.Right = width - 2f * WorldHud.Layout.compute(width, height, 1f).Margin;
            const float heading = 13f;
            CommandSheet.Placement placement = CommandSheet.layout(commands, sections, measure, sheet, heading);
            List<CommandBarLayout.Rect> rects = new List<CommandBarLayout.Rect>();
            for (int s = 0; s < sections.Count; s++) {
                CommandBarLayout.Rect headingRect = new CommandBarLayout.Rect { X = 0f, Y = placement.HeadingTops[s], Width = sheet.Right, Height = heading };
                foreach (int i in sections[s].Commands) {
                    CommandBarLayout.Rect button = placement.Buttons[i];
                    Assert.True(button.X >= 0f && button.Right <= sheet.Right + 0.01f, commands[i].getLabel() + " runs off the sheet");
                    Assert.True(button.Y > placement.HeadingTops[s], commands[i].getLabel() + " is above its heading");
                    Assert.False(button.overlaps(headingRect), commands[i].getLabel() + " covers its heading");
                    Assert.True(button.Bottom <= placement.Height + 0.01f);
                    Assert.Equal(settings.ButtonHeight, button.Height);
                    foreach (CommandBarLayout.Rect other in rects) {
                        Assert.False(button.overlaps(other), commands[i].getLabel() + " overlaps another button");
                    }
                    rects.Add(button);
                }
            }
            Assert.True(rects.Count > 0);
        }

        [Fact]
        public void theSheetsButtons_WaitTheSettleTime_EachTimeItOpens() {
            // drawn as WorldHud does: each frame the sheet is open its buttons
            // are checked where they are on the screen, and endFrame runs every frame
            ButtonSettle settle = new ButtonSettle();
            CommandBarLayout.Rect talk = new CommandBarLayout.Rect { X = 16f, Y = 300f, Width = 200f, Height = 44f };
            CommandBarLayout.Rect close = new CommandBarLayout.Rect { X = 16f, Y = 600f, Width = 80f, Height = 44f };
            System.Func<double, bool> frame = now => {
                bool settled = settle.isSettled("Talk to Ada Smith (Stranger)", talk.X, talk.Y, now)
                    & settle.isSettled(CommandSheet.CloseLabel, close.X, close.Y, now);
                settle.endFrame();
                return settled;
            };
            // the tap on More opens it: nothing in it takes that tap, or one just after
            Assert.False(frame(1.0));
            Assert.False(frame(1.0 + ButtonSettle.SettleSeconds / 2));
            Assert.True(frame(1.0 + ButtonSettle.SettleSeconds));
            // closed for a while (endFrame with nothing drawn), then opened again: it waits again
            settle.endFrame();
            settle.endFrame();
            Assert.False(frame(9.0));
            Assert.True(frame(9.0 + ButtonSettle.SettleSeconds));
            // dragged to scroll, a button has moved on the screen, so it waits again
            talk.Y -= 40f;
            Assert.False(frame(10.0));
        }

        [Fact]
        public void theSheetSaysWhyAButtonIsGreyed_AndForgetsItWhenClosed() {
            CommandSheet.setOpen(true);
            try {
                Assert.Equal(CommandSheet.Intro, CommandSheet.getNote());
                CommandSheet.setNote("You have no coins.");
                Assert.Equal("You have no coins.", CommandSheet.getNote());
                CommandSheet.close();
                CommandSheet.setOpen(true);
                Assert.Equal(CommandSheet.Intro, CommandSheet.getNote());
            } finally {
                CommandSheet.close();
            }
            Assert.False(CommandSheet.isOpen());
        }
    }
}
