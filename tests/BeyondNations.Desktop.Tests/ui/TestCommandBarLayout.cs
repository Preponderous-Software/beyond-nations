using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The command bar keeps its buttons still (playtest: a caravan came into
    * range and the click meant for Enter Settlement raided it; a Buy Axe
    * button pushed Exit Settlement under the cursor aimed at Purchase Food;
    * two Build buttons swapped), fits the window without running under the
    * right-hand column, and asks twice before anything that cannot be
    * undone. The build key builds what the settlement box says it will, and
    * also asks twice.
    */
    public class TestCommandBarLayout {

        // ProggyClean, the ImGui default font: 7 pixels a character, 13 high
        private static float measure(string label) {
            return label.Length * 7f;
        }

        private const float FrameHeight = 19f;

        private static CommandBarLayout.Settings settingsFor(float width, float height, float reserve, bool touch, float uiScale,
                out WorldHud.Layout layout, out bool stackCentre) {
            layout = WorldHud.Layout.compute(width, height, uiScale);
            stackCentre = WorldHud.isStackCentre(width, layout, touch);
            float bottom = height - reserve;
            return WorldHud.barSettings(width, WorldHud.getBarRight(width, layout, stackCentre), bottom, layout,
                uiScale, FrameHeight * uiScale, 8f * uiScale, 4f * uiScale, 8f * uiScale);
        }

        private static WorldCommand command(string label, CommandGroup group) {
            return new WorldCommand(label, group, () => { });
        }

        /** About the most a leader standing in their own town is offered at once. */
        private static List<WorldCommand> busySettlement() {
            List<WorldCommand> commands = new List<WorldCommand> {
                command("Leave Nation", CommandGroup.Drastic),
                command("Withdraw Funds", CommandGroup.Nation),
                command("Collect Stockpile (41)", CommandGroup.Nation),
                command("Exit Settlement", CommandGroup.Place),
                command("Purchase Food", CommandGroup.Place),
                command("Sell Resources", CommandGroup.Place),
                command("Build Stall", CommandGroup.Build).requireConfirm(),
                command("Build House", CommandGroup.Build).requireConfirm(),
                command("Build Farm", CommandGroup.Build).requireConfirm(),
                command("Build Storehouse", CommandGroup.Build).requireConfirm(),
                command("Build Workshop", CommandGroup.Build).requireConfirm(),
                command("Transfer Items", CommandGroup.Stall),
                command("Collect Coins (12)", CommandGroup.Stall),
                command("Collect Food (3)", CommandGroup.Stall),
                command("Craft Pickaxe", CommandGroup.Craft),
                command("Buy Axe (327)", CommandGroup.Craft),
                // the locked caravan button and the war/peace levers #361 adds
                command("Send Caravan (needs Storehouse)", CommandGroup.Trade),
                command("Offer Peace to Newham", CommandGroup.Trade),
                command("Declare War on Newham", CommandGroup.Drastic),
                command("Denounce Newham", CommandGroup.Drastic),
            };
            return CommandBarLayout.order(commands);
        }

        private static Dictionary<string, CommandBarLayout.Rect> place(List<WorldCommand> commands, CommandBarLayout.Settings settings) {
            CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings);
            Dictionary<string, CommandBarLayout.Rect> byLabel = new Dictionary<string, CommandBarLayout.Rect>();
            for (int i = 0; i < commands.Count; i++) {
                byLabel[commands[i].getLabel()] = result.Buttons[i];
            }
            return byLabel;
        }

        [Fact]
        public void theSameCommandsAlwaysGoInTheSamePlaces() {
            CommandBarLayout.Settings settings = settingsFor(960, 540, 0, false, 1f, out _, out _);
            Dictionary<string, CommandBarLayout.Rect> first = place(busySettlement(), settings);
            Dictionary<string, CommandBarLayout.Rect> second = place(busySettlement(), settings);
            Assert.Equal(first, second);
        }

        [Fact]
        public void aHostileCommandComingIntoRangeMovesNothing_AndSitsApartBelowTheRest() {
            CommandBarLayout.Settings settings = settingsFor(960, 540, 0, false, 1f, out _, out _);
            List<WorldCommand> road = CommandBarLayout.order(new List<WorldCommand> {
                command("Leave Nation", CommandGroup.Drastic),
                command("Enter Settlement", CommandGroup.Place),
                command("Teleport Home", CommandGroup.Place),
                command("Plant Sapling", CommandGroup.Place),
            });
            Dictionary<string, CommandBarLayout.Rect> before = place(road, settings);

            List<WorldCommand> withCaravan = new List<WorldCommand>(road) {
                command("Buy stone (12)", CommandGroup.Trade),
                command("Sell to Caravan", CommandGroup.Trade),
                command("Escort Caravan", CommandGroup.Trade),
                command("Raid Caravan", CommandGroup.Drastic),
            };
            Dictionary<string, CommandBarLayout.Rect> after = place(CommandBarLayout.order(withCaravan), settings);

            foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in before) {
                Assert.Equal(entry.Value, after[entry.Key]);
            }
            CommandBarLayout.Rect raid = after["Raid Caravan"];
            foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in after) {
                if (entry.Key != "Raid Caravan" && entry.Key != "Leave Nation") {
                    Assert.True(entry.Value.Bottom < raid.Y, entry.Key + " should be on a row above the raid");
                }
            }
        }

        [Fact]
        public void aBenignCommandAppearingInTheMarketDoesNotMoveExitOrPurchaseFood() {
            CommandBarLayout.Settings settings = settingsFor(960, 540, 0, false, 1f, out _, out _);
            List<WorldCommand> market = CommandBarLayout.order(new List<WorldCommand> {
                command("Exit Settlement", CommandGroup.Place),
                command("Purchase Food", CommandGroup.Place),
                command("Sell Resources", CommandGroup.Place),
            });
            Dictionary<string, CommandBarLayout.Rect> before = place(market, settings);
            List<WorldCommand> withAxe = new List<WorldCommand>(market) { command("Buy Axe (327)", CommandGroup.Craft) };
            Dictionary<string, CommandBarLayout.Rect> after = place(CommandBarLayout.order(withAxe), settings);
            foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in before) {
                Assert.Equal(entry.Value, after[entry.Key]);
            }
        }

        [Fact]
        public void armingAConfirmedButtonOrACountTickingUpDoesNotNudgeItsNeighbours() {
            CommandBarLayout.Settings settings = settingsFor(1280, 720, 0, false, 1f, out _, out _);
            WorldCommand raid = command("Raid Caravan", CommandGroup.Drastic);
            Assert.True(raid.needsConfirm());
            Assert.True(CommandBarLayout.buttonWidth(raid, measure, settings) >= measure(CommandBarLayout.getArmedLabel(raid)) + settings.TextPadding);

            Dictionary<string, CommandBarLayout.Rect> nine = place(new List<WorldCommand> {
                command("Collect Stockpile (9)", CommandGroup.Nation), command("Exit Settlement", CommandGroup.Place) }, settings);
            Dictionary<string, CommandBarLayout.Rect> ten = place(new List<WorldCommand> {
                command("Collect Stockpile (10)", CommandGroup.Nation), command("Exit Settlement", CommandGroup.Place) }, settings);
            Assert.Equal(nine["Exit Settlement"], ten["Exit Settlement"]);
        }

        public static IEnumerable<object[]> Screens() {
            // width, height, touch band, touch, UI scale
            yield return new object[] { 960f, 540f, 0f, false, 1f };
            yield return new object[] { 1280f, 720f, 0f, false, 1f };
            yield return new object[] { 1280f, 720f, 0f, false, 1.5f };
            // phones as the web build sees them (#325): CSS pixels, with the
            // controls' band main.js reserves at the bottom
            yield return new object[] { 844f, 390f, 150f, true, 1f };
            yield return new object[] { 390f, 844f, 220f, true, 1f };
        }

        [Theory]
        [MemberData(nameof(Screens))]
        public void theBarFitsTheWindowAndStaysClearOfTheRightColumn(float width, float height, float reserve, bool touch, float uiScale) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out WorldHud.Layout layout, out bool stackCentre);
            List<WorldCommand> commands = busySettlement();
            CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings);

            Assert.True(result.Top >= 0f, "the bar runs off the top: " + result.Top);
            // the right-hand column runs to the bottom beside the bar, except
            // on a phone held upright, where it sits at the top
            CommandBarLayout.Rect rightColumn = new CommandBarLayout.Rect {
                X = layout.RightX, Y = layout.Margin, Width = layout.BoxWidth, Height = height - reserve - 2f * layout.Margin
            };
            for (int i = 0; i < commands.Count; i++) {
                CommandBarLayout.Rect button = result.Buttons[i];
                string label = commands[i].getLabel();
                Assert.True(button.X >= 0f && button.Right <= width, label + " runs off the side");
                Assert.True(button.Y >= 0f && button.Bottom <= height - reserve, label + " runs off the bottom or under the touch controls");
                if (!stackCentre) {
                    Assert.False(button.overlaps(rightColumn), label + " is under the right-hand column");
                }
                for (int j = i + 1; j < commands.Count; j++) {
                    Assert.False(button.overlaps(result.Buttons[j]), label + " overlaps " + commands[j].getLabel());
                }
            }
        }

        [Fact]
        public void aLabelTooLongForTheBarIsClippedToIt() {
            CommandBarLayout.Settings settings = settingsFor(390, 844, 220, true, 1f, out _, out _);
            WorldCommand longOne = command("Propose Alliance to The Most Serene Republic of Somewhere", CommandGroup.Trade);
            CommandBarLayout.Result result = CommandBarLayout.compute(new List<WorldCommand> { longOne }, measure, settings);
            Assert.True(result.Buttons[0].Right <= settings.Right);
        }

        [Fact]
        public void theConfirmGateRunsOnTheSecondPressOfTheSameThingInTime() {
            ConfirmGate gate = new ConfirmGate(4.0);
            Assert.False(gate.press("Raid Caravan", 10.0));
            Assert.True(gate.isArmed("Raid Caravan", 11.0));
            Assert.True(gate.press("Raid Caravan", 11.0));
            Assert.False(gate.isArmed("Raid Caravan", 11.5));

            // something else in between, or too long a wait, starts over
            Assert.False(gate.press("Raid Caravan", 20.0));
            Assert.False(gate.press("Build Farm", 20.5));
            Assert.False(gate.press("Raid Caravan", 21.0));
            Assert.False(gate.press("Raid Caravan", 30.0));
        }

        /**
        * 0.8.0 playtests s02, s03, s07: at one or two frames a second the
        * second click on Build House landed 6 to 13 s after the first and
        * only re-armed it. While the pointer rests on the armed button
        * (hold, each frame) it stays armed; once it moves off, the window
        * runs out as before.
        */
        [Fact]
        public void anArmedButtonStaysArmedWhileThePointerRestsOnIt_HoweverSlowTheFrames() {
            Status status = new Status(new TickCounter(), 1000);
            ConfirmGate gate = new ConfirmGate();
            Assert.False(gate.press("Build House", 10.0));
            gate.prompt(status, "Click Build House again to build it.");

            // one frame a second for 13 s, the pointer on the button
            for (double now = 11.0; now <= 23.0; now += 1.0) {
                gate.update(now, status);
                gate.hold("Build House", now);
            }
            Assert.Equal("Click Build House again to build it.", status.getStatus());
            Assert.True(gate.press("Build House", 23.5));

            // pointer moved off: the window lapses as before
            Assert.False(gate.press("Build House", 30.0));
            gate.hold("Build Farm", 31.0);
            Assert.False(gate.isArmed("Build House", 30.0 + ConfirmGate.DefaultWindowSeconds + 1.0));
        }

        /**
        * 0.6.1/0.6.2 playtests: after the window lapsed the button reverted
        * but the status line still said "Click War again...", and asking
        * again read "(x2)", like "click twice".
        */
        [Fact]
        public void aLapsedConfirmTakesItsPromptBack_AndAskingAgainIsNotCounted() {
            Status status = new Status(new TickCounter(), 1000);
            ConfirmGate gate = new ConfirmGate(4.0);
            Assert.False(gate.press("war", 10.0));
            gate.prompt(status, "Click War again to declare war on Blackport.");

            gate.update(13.0, status);
            Assert.Equal("Click War again to declare war on Blackport.", status.getStatus());

            gate.update(15.0, status);
            Assert.Equal(ConfirmGate.LapsedMessage, status.getStatus());
            Assert.False(gate.isArmed("war", 15.0));

            // asking again after the lapse reads as the plain prompt
            Assert.False(gate.press("war", 20.0));
            gate.prompt(status, "Click War again to declare war on Blackport.");
            Assert.False(gate.press("war", 30.0));
            gate.prompt(status, "Click War again to declare war on Blackport.");
            Assert.Equal("Click War again to declare war on Blackport.", status.getStatus());
        }

        [Fact]
        public void aLapsedConfirmLeavesANewerMessageAlone() {
            Status status = new Status(new TickCounter(), 1000);
            ConfirmGate gate = new ConfirmGate(4.0);
            Assert.False(gate.press("war", 10.0));
            gate.prompt(status, "Click War again to declare war on Blackport.");
            status.update("Greenshire has lifted its siege of your village.");

            gate.update(20.0, status);

            Assert.Equal("Greenshire has lifted its siege of your village.", status.getStatus());
        }

        [Fact]
        public void aConfirmedPressLeavesTheActionsOwnMessage() {
            Status status = new Status(new TickCounter(), 1000);
            ConfirmGate gate = new ConfirmGate(4.0);
            Assert.False(gate.press("war", 10.0));
            gate.prompt(status, "Click War again to declare war on Blackport.");
            Assert.True(gate.press("war", 11.0));

            gate.update(30.0, status);

            // nothing to take back: the prompt was spent by the confirmation
            Assert.Equal("Click War again to declare war on Blackport.", status.getStatus());
        }

        [Fact]
        public void hostileAndIrreversibleCommandsAskTwice() {
            Assert.True(command("Denounce Newham", CommandGroup.Drastic).needsConfirm());
            Assert.False(command("Purchase Food", CommandGroup.Place).needsConfirm());
            Assert.Equal("Click again to confirm: Raid Caravan.", WorldHud.getConfirmPrompt(command("Raid Caravan", CommandGroup.Drastic), false));
            Assert.Equal("Tap again to confirm: Raid Caravan.", WorldHud.getConfirmPrompt(command("Raid Caravan", CommandGroup.Drastic), true));
        }

        // ---- against a real world ----

        private static Simulation inOwnCamp(out Settlement settlement) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom())
                .execute(player);
            settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getHomeSettlementId());
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            Assert.True(player.isCurrentlyInSettlement());
            Inventory carried = player.getInventory();
            foreach (ItemType type in new[] { ItemType.WOOD, ItemType.STONE, ItemType.COIN }) {
                carried.removeItem(type, carried.getNumItems(type));
                settlement.getInventory().removeItem(type, settlement.getInventory().getNumItems(type));
            }
            return simulation;
        }

        private static WorldCommand find(Simulation simulation, string label) {
            return WorldCommandBar.getCommands(simulation).Find(c => c.getLabel() == label);
        }

        private static List<string> labels(Simulation simulation) {
            return WorldCommandBar.getCommands(simulation).ConvertAll(c => c.getLabel());
        }

        [Fact]
        public void inASettlement_ExitPurchaseAndSellAreAlwaysThereInTheSameOrder() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            // a stall to trade at: with none, the market buttons stay greyed
            Assert.True(settlement.getMarket().createStall());
            Player player = simulation.getPlayer();
            List<string> broke = labels(simulation);
            Assert.False(find(simulation, "Purchase Food").isEnabled());
            Assert.False(find(simulation, "Sell Resources").isEnabled());

            player.getInventory().addItem(ItemType.COIN, 50);
            player.getInventory().addItem(ItemType.WOOD, 5);
            Assert.True(find(simulation, "Purchase Food").isEnabled());
            Assert.True(find(simulation, "Sell Resources").isEnabled());
            Assert.Equal(broke, labels(simulation));

            List<string> order = labels(simulation);
            Assert.True(order.IndexOf("Exit Settlement") < order.IndexOf("Purchase Food"));
            Assert.True(order.IndexOf("Purchase Food") < order.IndexOf("Sell Resources"));
        }

        [Fact]
        public void buildButtonsKeepOneOrder_AreDisabledUntilAffordable_AndAskTwice() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();

            WorldCommand house = find(simulation, "Build House");
            WorldCommand farm = find(simulation, "Build Farm");
            Assert.NotNull(house);
            Assert.NotNull(farm);
            Assert.False(house.isEnabled());
            Assert.True(house.needsConfirm());
            List<string> before = labels(simulation);
            Assert.True(before.IndexOf("Build House") < before.IndexOf("Build Farm"));

            // the planner now wants a house first; the buttons stay put
            settlement.addBuilding(BuildingType.FARM);
            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getInventory().addItem(ItemType.STONE, 100);
            List<string> after = labels(simulation);
            Assert.True(after.IndexOf("Build House") < after.IndexOf("Build Farm"));
            Assert.True(find(simulation, "Build House").isEnabled());
        }

        [Fact]
        public void outInTheWorld_EnterSettlementHoldsItsPlaceWhenOutOfReach() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            new ExitSettlementCommand(simulation.getEntityRepository()).execute(player);
            player.setPosition(settlement.getPosition());
            List<string> near = labels(simulation);
            Assert.True(find(simulation, "Enter Settlement").isEnabled());

            player.setPosition(settlement.getPosition() + new System.Numerics.Vector3(0, 0, 400));
            Assert.False(find(simulation, "Enter Settlement").isEnabled());
            Assert.Equal(near.IndexOf("Enter Settlement"), labels(simulation).IndexOf("Enter Settlement"));
        }

        [Fact]
        public void leaveNationIsDrastic() {
            Simulation simulation = inOwnCamp(out _);
            WorldCommand leave = find(simulation, "Leave Nation");
            Assert.Equal(CommandGroup.Drastic, leave.getGroup());
            Assert.True(leave.needsConfirm());
        }

        /** Standing in a hostile neighbour's land as a leader (--stage-diplomacy): gift, peace, war, denounce. */
        private static Simulation inForeignLand(out string ownerName) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            DiplomacyShowcase.Staged staged = DiplomacyShowcase.stage(simulation);
            simulation.getPlayer().setPosition(staged.hostileSettlement.getPosition() + new Vector3(30, 0, 0));
            ownerName = simulation.getNationRepository().getNation(staged.hostileSettlement.getNationId()).getName();
            return simulation;
        }

        [Fact]
        public void inForeignLand_WarAndDenounceAreDrastic_PeaceIsNot() {
            Simulation simulation = inForeignLand(out string name);
            WorldCommand war = find(simulation, "Declare War on " + name);
            WorldCommand peace = find(simulation, "Offer Peace to " + name);
            WorldCommand denounce = find(simulation, "Denounce " + name);
            Assert.NotNull(war);
            Assert.NotNull(peace);
            Assert.Equal(CommandGroup.Drastic, war.getGroup());
            Assert.True(war.needsConfirm());
            Assert.Equal(CommandGroup.Drastic, denounce.getGroup());
            Assert.True(denounce.needsConfirm());
            Assert.Equal(CommandGroup.Trade, peace.getGroup());
            Assert.False(peace.needsConfirm());
        }

        [Fact]
        public void inForeignLand_WarBecomingAvailableMovesNoOtherButton() {
            Simulation simulation = inForeignLand(out string name);
            CommandBarLayout.Settings settings = settingsFor(960, 540, 0, false, 1f, out _, out _);
            List<WorldCommand> all = WorldCommandBar.getCommands(simulation);
            List<WorldCommand> withoutWar = all.FindAll(c => c.getLabel() != "Declare War on " + name);
            Assert.Equal(all.Count - 1, withoutWar.Count);
            Dictionary<string, CommandBarLayout.Rect> before = place(withoutWar, settings);
            Dictionary<string, CommandBarLayout.Rect> after = place(all, settings);
            foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in before) {
                Assert.Equal(entry.Value, after[entry.Key]);
            }
        }

        [Fact]
        public void inTheLeadersOwnCamp_TheLockedSendCaravanSitsWithTheTradeButtons() {
            Simulation simulation = inOwnCamp(out _);
            WorldCommand send = WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith(CaravanCommands.SendCaravanLabel));
            Assert.NotNull(send);
            Assert.NotEqual(CaravanCommands.SendCaravanLabel, send.getLabel());
            Assert.Equal(CommandGroup.Trade, send.getGroup());
            // after every place button, so it cannot move Exit, Purchase Food or Sell Resources
            List<string> order = labels(simulation);
            Assert.True(order.IndexOf("Sell Resources") < order.IndexOf(send.getLabel()));
        }

        [Theory]
        [MemberData(nameof(Screens))]
        public void theRealBarsFitTheWindowAndStayClearOfTheRightColumn(float width, float height, float reserve, bool touch, float uiScale) {
            Simulation camp = inOwnCamp(out _);
            camp.getPlayer().getInventory().addItem(ItemType.COIN, 500);
            camp.getPlayer().getInventory().addItem(ItemType.WOOD, 100);
            camp.getPlayer().getInventory().addItem(ItemType.STONE, 100);
            Simulation foreign = inForeignLand(out _);
            foreach (Simulation simulation in new[] { camp, foreign }) {
                CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out WorldHud.Layout layout, out bool stackCentre);
                List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
                CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings);
                Assert.True(result.Top >= 0f, "the bar runs off the top: " + result.Top);
                CommandBarLayout.Rect rightColumn = new CommandBarLayout.Rect {
                    X = layout.RightX, Y = layout.Margin, Width = layout.BoxWidth, Height = height - reserve - 2f * layout.Margin
                };
                for (int i = 0; i < commands.Count; i++) {
                    CommandBarLayout.Rect button = result.Buttons[i];
                    string label = commands[i].getLabel();
                    Assert.True(button.X >= 0f && button.Right <= width, label + " runs off the side");
                    Assert.True(button.Y >= 0f && button.Bottom <= height - reserve, label + " runs off the bottom or under the touch controls");
                    if (!stackCentre) {
                        Assert.False(button.overlaps(rightColumn), label + " is under the right-hand column");
                    }
                    for (int j = i + 1; j < commands.Count; j++) {
                        Assert.False(button.overlaps(result.Buttons[j]), label + " overlaps " + commands[j].getLabel());
                    }
                }
            }
        }

        [Fact]
        public void theLeaveKeyAsksBeforeDisbanding_AndDisbandsOnTheSecondPress() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            LeaveNationKey key = new LeaveNationKey();

            // playtest 0.6.0 s09: one press of L disbanded the nation (#368)
            Assert.False(key.press(simulation, 1.0));
            Assert.Equal(nation.getId(), player.getNationId());
            Assert.Equal("Press L again to disband " + nation.getName() + ".", player.getStatus().getStatus());

            // once the window lapses, the prompt is taken back (0.6.2 playtest)
            double late = 1.0 + ConfirmGate.DefaultWindowSeconds + 1.0;
            key.update(player, 1.0 + ConfirmGate.DefaultWindowSeconds - 1.0);
            Assert.Equal("Press L again to disband " + nation.getName() + ".", player.getStatus().getStatus());
            key.update(player, late - 0.5);
            Assert.Equal(ConfirmGate.LapsedMessage, player.getStatus().getStatus());

            // too late: asks again, without a "(x2)"
            Assert.False(key.press(simulation, late));
            Assert.NotNull(player.getNationId());
            Assert.Equal("Press L again to disband " + nation.getName() + ".", player.getStatus().getStatus());

            Assert.True(key.press(simulation, late + 0.5));
            Assert.Null(player.getNationId());

            // with no nation, the refusal shows at once
            Assert.True(key.press(simulation, late + 1.0));
            Assert.Equal("You are not a member of a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void pawnsComingAndGoingMoveNoOtherButton() {
            List<WorldCommand> withoutPawn = new List<WorldCommand> {
                command("Enter Settlement", CommandGroup.Place),
                command("Build Farm (25 wood, 10 stone)", CommandGroup.Build),
                command("Send Caravan", CommandGroup.Trade),
                command("Offer Peace to Newham", CommandGroup.Trade),
                command("Leave Nation", CommandGroup.Drastic).requireConfirm(),
            };
            List<WorldCommand> withPawn = new List<WorldCommand>(withoutPawn) {
                command("Talk to Charlotte Johnson (Friendly, greedy)", CommandGroup.People),
                command("Order Charlotte Johnson: Gather Wood", CommandGroup.People),
                command("Appoint Charlotte Johnson Merchant", CommandGroup.People),
            };
            CommandBarLayout.Settings settings = settingsFor(960f, 540f, 0f, false, 1f, out _, out _);
            List<WorldCommand> a = CommandBarLayout.order(withoutPawn);
            List<WorldCommand> b = CommandBarLayout.order(withPawn);
            CommandBarLayout.Result before = CommandBarLayout.compute(a, measure, settings);
            CommandBarLayout.Result after = CommandBarLayout.compute(b, measure, settings);
            for (int i = 0; i < a.Count; i++) {
                int j = b.IndexOf(a[i]);
                Assert.Equal(before.Buttons[i].X, after.Buttons[j].X);
                Assert.Equal(before.Buttons[i].Y, after.Buttons[j].Y);
            }
        }

        /** The leader's own town in playtest 0.6.2 s02 (shots/count_mismatch.png), at 960x540. */
        private static List<WorldCommand> ownTownWithPawn(string talk, string pawnName) {
            return new List<WorldCommand> {
                command("Leave Nation", CommandGroup.Drastic),
                command("Withdraw Funds", CommandGroup.Nation),
                command("Collect Stockpile (212)", CommandGroup.Nation),
                command("Exit Settlement", CommandGroup.Place),
                command(WorldCommandBar.feedResidentsLabel(8), CommandGroup.Place),
                command("Purchase Food", CommandGroup.Place),
                command("Sell Resources", CommandGroup.Place),
                command("Build House", CommandGroup.Build).requireConfirm(),
                command("Build Farm", CommandGroup.Build).requireConfirm(),
                command("Send Caravan (Village era)", CommandGroup.Trade),
                command("Craft Axe", CommandGroup.Craft),
                command(talk, CommandGroup.People).setWidthLabel("Talk to " + WorldCommandBar.PawnNameSlot + " (" + WorldCommandBar.TierNameSlot + ")"),
                command("Gift " + pawnName, CommandGroup.People).setWidthLabel("Gift " + WorldCommandBar.PawnNameSlot),
                command("Order " + pawnName + ": Gather Wood", CommandGroup.People).setWidthLabel("Order " + WorldCommandBar.PawnNameSlot + ": Gather Wood"),
            };
        }

        private static void assertSamePlaces(List<WorldCommand> a, List<WorldCommand> b, CommandBarLayout.Settings settings, System.Func<WorldCommand, bool> which) {
            CommandBarLayout.Result before = CommandBarLayout.compute(a, measure, settings);
            CommandBarLayout.Result after = CommandBarLayout.compute(b, measure, settings);
            for (int i = 0; i < a.Count; i++) {
                if (!which(a[i])) {
                    continue;
                }
                int j = b.FindIndex(c => c.getGroup() == a[i].getGroup() && c.getLabel() == a[i].getLabel());
                Assert.True(j >= 0, a[i].getLabel());
                Assert.Equal(before.Buttons[i], after.Buttons[j]);
            }
        }

        /**
        * Playtest 0.6.2 s02 (finding 17): after Craft Axe the second click
        * landed on Send Caravan. Craft is now last of all, after the pawn
        * buttons, so its going moves nothing.
        */
        [Theory]
        [MemberData(nameof(Screens))]
        public void craftingTheAxeMovesNoOtherButton(float width, float height, float reserve, bool touch, float uiScale) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out _, out _);
            List<WorldCommand> withCraft = CommandBarLayout.order(ownTownWithPawn("Talk to Irene Johnson (Stranger)", "Irene Johnson"));
            List<WorldCommand> crafted = withCraft.FindAll(c => c.getLabel() != "Craft Axe");
            Assert.Equal(withCraft.Count - 1, crafted.Count);
            assertSamePlaces(withCraft, crafted, settings, c => c.getLabel() != "Craft Axe");
            // and it is not under the pawn buttons, which a second click could then hit
            List<WorldCommand> ordered = withCraft;
            Assert.True(ordered.FindIndex(c => c.getLabel() == "Craft Axe") > ordered.FindLastIndex(c => c.getGroup() == CommandGroup.People));
        }

        /** s02 finding 17: Feed Residents going away put "Talk to Bob Roberts (Stranger) - Glutton" under the cursor. */
        [Theory]
        [MemberData(nameof(Screens))]
        public void feedResidentsCountingDownToNoneMovesNothing(float width, float height, float reserve, bool touch, float uiScale) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out _, out _);
            List<WorldCommand> hungry = CommandBarLayout.order(ownTownWithPawn("Talk to Bob Roberts (Stranger)", "Bob Roberts"));
            List<WorldCommand> fed = hungry.ConvertAll(c => c.getLabel() == WorldCommandBar.feedResidentsLabel(8)
                ? command(WorldCommandBar.feedResidentsLabel(0), CommandGroup.Place).setEnabled(false) : c);
            CommandBarLayout.Result before = CommandBarLayout.compute(hungry, measure, settings);
            CommandBarLayout.Result after = CommandBarLayout.compute(fed, measure, settings);
            for (int i = 0; i < hungry.Count; i++) {
                Assert.Equal(before.Buttons[i], after.Buttons[i]);
            }
        }

        /** s02 finding 17: Talk's label carried the pawn's tier and traits, so its width, and Gift's place, changed with the pawn. */
        [Theory]
        [MemberData(nameof(Screens))]
        public void anotherPawnOrAWarmerTierMovesNoButton(float width, float height, float reserve, bool touch, float uiScale) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out _, out _);
            List<WorldCommand> ada = CommandBarLayout.order(ownTownWithPawn("Talk to Ada (Stranger)", "Ada"));
            List<WorldCommand> long_ = CommandBarLayout.order(ownTownWithPawn("Talk to Bartholomew Fitzgerald-Hughes (Acquaintance)", "Bartholomew Fitzgerald-Hughes"));
            CommandBarLayout.Result before = CommandBarLayout.compute(ada, measure, settings);
            CommandBarLayout.Result after = CommandBarLayout.compute(long_, measure, settings);
            Assert.Equal(ada.Count, long_.Count);
            for (int i = 0; i < ada.Count; i++) {
                Assert.Equal(before.Buttons[i], after.Buttons[i]);
            }
            // the long name is cut to its button rather than widening it
            int talk = long_.FindIndex(c => c.getLabel().StartsWith("Talk to"));
            string shown = CommandBarLayout.fitLabel(long_[talk].getLabel(), measure, after.Buttons[talk].Width - settings.TextPadding);
            Assert.EndsWith(CommandBarLayout.Ellipsis, shown);
            Assert.True(measure(shown) <= after.Buttons[talk].Width - settings.TextPadding);
            Assert.Equal("Talk to Ada (Stranger)", CommandBarLayout.fitLabel("Talk to Ada (Stranger)", measure, before.Buttons[talk].Width - settings.TextPadding));
        }

        [Fact]
        public void thePawnButtonsStartARowOfTheirOwn() {
            CommandBarLayout.Settings settings = settingsFor(1280f, 720f, 0f, false, 1f, out _, out _);
            List<WorldCommand> commands = CommandBarLayout.order(new List<WorldCommand> {
                command("Exit Settlement", CommandGroup.Place),
                command("Talk to Ada (Stranger)", CommandGroup.People),
            });
            CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings);
            Assert.Equal(settings.Left, result.Buttons[1].X);
            Assert.True(result.Buttons[1].Bottom <= result.Buttons[0].Y);
        }

        // ---- the real bar ----

        /** Feed Residents is always offered in the player's own settlement, disabled while no one here is hungry. */
        /** #357: Eat, by choice, names what it will eat and keeps one width whatever that is. */
        [Fact]
        public void theEatButton_NamesTheFood_AndIsDisabledOnAFullBar() {
            Simulation simulation = inOwnCamp(out _);
            Player player = simulation.getPlayer();
            Assert.Null(WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith("Eat")));

            player.getInventory().addItem(ItemType.COOKED_MEAT, 1);
            player.getInventory().addItem(ItemType.APPLE, 1);
            player.setEnergy(50f);
            WorldCommand eat = WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith("Eat"));
            Assert.Equal("Eat Cooked Meat (+45)", eat.getLabel());
            Assert.Equal(CommandGroup.Craft, eat.getGroup());
            Assert.True(eat.isEnabled());
            eat.execute();
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.COOKED_MEAT));

            player.setEnergy(Player.MaxEnergy);
            WorldCommand full = WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith("Eat"));
            Assert.False(full.isEnabled());
            Assert.Equal(eat.getWidthLabel(), full.getWidthLabel());
        }

        /** #353: in autumn, with no one hungry, Feed Residents stocks those short of winter stores. */
        [Fact]
        public void inAutumn_FeedResidentsOffersToStockThoseLowOnFood() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            simulation.getSeasonCalendar().advanceClockTo(simulation.getTickCounter(), Season.AUTUMN);
            Pawn pawn = new Pawn(settlement.getPosition() + new Vector3(400, 0, 400), "Member", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            pawn.setNationId(player.getNationId());
            simulation.getNationRepository().getNation(player.getNationId()).addMember(pawn.getId());
            pawn.getInventory().clear();
            pawn.setEnergy(95f);
            pawn.setCurrentSettlementId(settlement.getId());
            player.getInventory().addItem(ItemType.APPLE, SeasonalRules.WINTER_FOOD_STORE + 2);

            WorldCommand feed = WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith("Feed Residents"));
            Assert.Equal(WorldCommandBar.feedResidentsLabel(0, 1), feed.getLabel());
            Assert.Equal("Feed Residents (1 short for winter)", feed.getLabel());
            Assert.True(feed.isEnabled());
            feed.execute();
            Assert.Equal(SeasonalRules.WINTER_FOOD_STORE, FoodItems.countFood(pawn.getInventory()));
        }

        [Fact]
        public void inTheOwnSettlement_FeedResidentsHoldsItsPlace_AndCountsWhomItWillFeed() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            CommandBarLayout.Settings settings = settingsFor(960f, 540f, 0f, false, 1f, out _, out _);
            // the player carries food throughout, so the Eat button is there both times
            player.getInventory().addItem(ItemType.APPLE, 3);
            List<WorldCommand> none = WorldCommandBar.getCommands(simulation);
            WorldCommand feedNone = none.Find(c => c.getLabel() == WorldCommandBar.feedResidentsLabel(0));
            Assert.NotNull(feedNone);
            Assert.False(feedNone.isEnabled());
            Assert.Equal(CommandGroup.Place, feedNone.getGroup());

            // a hungry resident in the settlement, out of the player's reach
            Pawn pawn = new Pawn(settlement.getPosition() + new Vector3(400, 0, 400), "Member", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            pawn.setNationId(player.getNationId());
            simulation.getNationRepository().getNation(player.getNationId()).addMember(pawn.getId());
            pawn.getInventory().clear();
            pawn.setEnergy(50f);
            pawn.setCurrentSettlementId(settlement.getId());

            List<WorldCommand> one = WorldCommandBar.getCommands(simulation);
            Assert.Equal(none.Count, one.Count);
            int index = none.IndexOf(feedNone);
            Assert.Equal(WorldCommandBar.feedResidentsLabel(1), one[index].getLabel());
            Assert.True(one[index].isEnabled());
            CommandBarLayout.Result before = CommandBarLayout.compute(none, measure, settings);
            CommandBarLayout.Result after = CommandBarLayout.compute(one, measure, settings);
            for (int i = 0; i < none.Count; i++) {
                Assert.Equal(before.Buttons[i], after.Buttons[i]);
            }

            one[index].execute();
            Assert.StartsWith("You fed 1 resident", player.getStatus().getStatus());
            Assert.Equal(WorldCommandBar.feedResidentsLabel(0), WorldCommandBar.getCommands(simulation)[index].getLabel());
        }

        [Fact]
        public void theTalkButtonNamesTheTier_NotTheTraits_AndIsMeasuredWithAStandInName() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(1234);
            Simulation simulation = new Simulation(config);
            Pawn pawn = new Pawn(simulation.getPlayer().getPosition() + new Vector3(0.5f, 0, 0), "Ada", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            WorldCommand talk = WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith("Talk to"));
            Assert.Equal("Talk to Ada (Stranger)", talk.getLabel());
            Assert.Equal("Talk to " + WorldCommandBar.PawnNameSlot + " (" + WorldCommandBar.TierNameSlot + ")", talk.getWidthLabel());
            Assert.Equal("Talk to " + PawnRelations.describeWithTraits(pawn, simulation.getPlayer()), talk.getTooltip());
        }

        [Fact]
        public void theBuildKeyBuildsWhatTheSettlementBoxNames_NotSomethingCheaper() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            Assert.Equal(BuildingType.FARM, BuildingPlanner.suggestNext(settlement));
            Assert.Equal(BuildingType.FARM, BuildKey.getTarget(simulation));

            // enough for a House (20/15) but not the Farm the box names (25/10)
            player.getInventory().addItem(ItemType.WOOD, 20);
            player.getInventory().addItem(ItemType.STONE, 15);
            BuildKey key = new BuildKey();
            Assert.False(key.press(simulation, 1.0));
            Assert.False(key.press(simulation, 1.5));
            Assert.Equal(0, settlement.getNumBuildings());
            Assert.Equal(20, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.StartsWith("A Farm needs", player.getStatus().getStatus());
        }

        [Fact]
        public void theBuildKeyAsksBeforeSpending_AndBuildsOnTheSecondPress() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            player.getInventory().addItem(ItemType.WOOD, 30);
            player.getInventory().addItem(ItemType.STONE, 20);
            BuildKey key = new BuildKey();

            Assert.False(key.press(simulation, 1.0));
            Assert.Equal(0, settlement.getNumBuildings());
            Assert.Equal(30, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(BuildKey.getConfirmPrompt(BuildingType.FARM), player.getStatus().getStatus());
            Assert.Equal("Press K again to build a Farm (25 wood, 10 stone).", BuildKey.getConfirmPrompt(BuildingType.FARM));

            Assert.True(key.press(simulation, 2.0));
            Assert.Equal(1, settlement.getNumBuildings(BuildingType.FARM));

            // a press long after the last one asks again
            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getInventory().addItem(ItemType.STONE, 100);
            Assert.False(key.press(simulation, 3.0));
            Assert.False(key.press(simulation, 30.0));
            Assert.Equal(1, settlement.getNumBuildings());
            // re-asked after the lapse, not counted as "(x2)" (0.6.2 playtest)
            string prompt = BuildKey.getConfirmPrompt(BuildingPlanner.suggestNext(settlement).Value);
            Assert.Equal(prompt, player.getStatus().getStatus());
            key.update(player, 30.0 + ConfirmGate.DefaultWindowSeconds + 1.0);
            Assert.Equal(ConfirmGate.LapsedMessage, player.getStatus().getStatus());
        }

        /**
        * #345 as the 0.6.0 playtest's s09 saw it, replayed against today's
        * bar: the pointer held still at one spot while pawns came and went.
        * The Talk button changed pawn under it, an Order row came and went,
        * Send Caravan jumped between rows, and a pawn row appearing between
        * the two clicks of Declare War re-armed it instead of confirming.
        * Now nothing already on the bar moves, a different pawn under the
        * pointer takes no click until it has stood still for ButtonSettle's
        * wait, and the armed War survives the new row.
        */
        [Theory]
        [MemberData(nameof(Screens))]
        public void session09sReflow_MovesNothing_SwapsNoClick_AndKeepsTheArmedWar(float width, float height, float reserve, bool touch, float uiScale) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out _, out _);
            List<WorldCommand>[] frames = {
                session09Bar(null, false),
                session09Bar("Charlotte Johnson", false),
                session09Bar("Charlotte Johnson", true),
                session09Bar("Abigail Evans", true),
                session09Bar("Abigail Evans", false),
                session09Bar(null, false),
            };

            // every button on the bar throughout keeps one place, Send Caravan and War included
            Dictionary<string, CommandBarLayout.Rect> first = place(frames[0], settings);
            foreach (List<WorldCommand> commands in frames) {
                Dictionary<string, CommandBarLayout.Rect> now = place(commands, settings);
                foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in first) {
                    Assert.Equal(entry.Value, now[entry.Key]);
                }
            }

            // Talk changes pawn in the same slot: the pointer resting there gets no click at once
            CommandBarLayout.Rect slot = place(frames[2], settings)["Talk to Charlotte Johnson (Stranger)"];
            Assert.Equal(slot, place(frames[3], settings)["Talk to Abigail Evans (Stranger)"]);
            ButtonSettle settle = new ButtonSettle();
            Assert.False(settle.isSettled("Talk to Charlotte Johnson (Stranger)", slot.X, slot.Y, 0.0));
            settle.endFrame();
            Assert.True(settle.isSettled("Talk to Charlotte Johnson (Stranger)", slot.X, slot.Y, 5.0));
            settle.endFrame();
            Assert.False(settle.isSettled("Talk to Abigail Evans (Stranger)", slot.X, slot.Y, 5.1));
            settle.endFrame();

            // War armed with no pawn near; a pawn row appears; the second click is still on War and confirms it
            ConfirmGate gate = new ConfirmGate();
            Assert.False(gate.press("Declare War on Newham", 10.0));
            Assert.Equal(first["Declare War on Newham"], place(frames[2], settings)["Declare War on Newham"]);
            Assert.True(gate.press("Declare War on Newham", 13.0));
        }

        /** The bar in s09: out in the world near Newham, with pawn, and its Order row, when given. */
        private static List<WorldCommand> session09Bar(string pawn, bool orders) {
            List<WorldCommand> commands = new List<WorldCommand> {
                command("Enter Settlement", CommandGroup.Place),
                command("Send Caravan", CommandGroup.Trade),
                command("Offer Peace to Newham", CommandGroup.Trade),
                command("Leave Nation", CommandGroup.Drastic).requireConfirm(),
                command("Declare War on Newham", CommandGroup.Drastic).requireConfirm(),
            };
            if (pawn != null) {
                commands.Add(command("Talk to " + pawn + " (Stranger)", CommandGroup.People)
                    .setWidthLabel("Talk to " + WorldCommandBar.PawnNameSlot + " (" + WorldCommandBar.TierNameSlot + ")"));
                if (orders) {
                    commands.Add(command("Order " + pawn + ": Gather Wood", CommandGroup.People)
                        .setWidthLabel("Order " + WorldCommandBar.PawnNameSlot + ": Gather Wood"));
                }
            }
            return CommandBarLayout.order(commands);
        }

        // --- #486: the pawn rows are kept inside a settlement ---

        /** A leader in their own settlement, with or without a resident in reach. */
        private static List<WorldCommand> settlementWithPawn(bool pawn) {
            List<WorldCommand> commands = new List<WorldCommand> {
                command("Leave Nation", CommandGroup.Drastic),
                command("Withdraw Funds", CommandGroup.Nation),
                command("Collect Stockpile (224)", CommandGroup.Nation),
                command("Exit Settlement", CommandGroup.Place),
                command("Settlement", CommandGroup.Place),
                command("Purchase Food", CommandGroup.Place),
                command("Sell Resources", CommandGroup.Place),
                command("Trade at Market", CommandGroup.Place),
                command("Build Workshop", CommandGroup.Build).requireConfirm(),
                command("Send Caravan", CommandGroup.Trade),
                command("Eat Apple", CommandGroup.Craft),
            };
            if (pawn) {
                commands.Add(command("Talk to Charlotte Lewis (Stranger)", CommandGroup.People)
                    .setWidthLabel("Talk to " + WorldCommandBar.PawnNameSlot + " (" + WorldCommandBar.TierNameSlot + ")"));
                commands.Add(command("Gift Charlotte Lewis", CommandGroup.People).setWidthLabel("Gift " + WorldCommandBar.PawnNameSlot));
                commands.Add(command("Invite Charlotte Lewis", CommandGroup.People).setWidthLabel("Invite " + WorldCommandBar.PawnNameSlot));
                commands.Add(command("Order Charlotte Lewis: Gather Wood", CommandGroup.People)
                    .setWidthLabel("Order " + WorldCommandBar.PawnNameSlot + ": Gather Wood"));
            }
            return CommandBarLayout.order(commands);
        }

        private static CommandBarLayout.Rect rectOf(List<WorldCommand> commands, CommandBarLayout.Result placed, string label) {
            return placed.Buttons[commands.FindIndex(c => c.getLabel() == label)];
        }

        [Theory]
        [InlineData(1280f, 720f, 0f, false, 1f)]
        [InlineData(960f, 540f, 0f, false, 1f)]
        [InlineData(1920f, 1080f, 0f, false, 1.5f)]
        [InlineData(390f, 844f, 260f, true, 1f)]   // a phone held upright
        [InlineData(844f, 390f, 140f, true, 1f)]   // and on its side
        public void testAPawnComingAndGoing_MovesNoButtonAndNotTheTop(float width, float height, float reserve, bool touch, float uiScale) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out _, out _);
            int needed = CommandBarLayout.countRows(WorldCommandBar.getPawnStandIns(), measure, settings);
            settings.ReservedPeopleRows = System.Math.Min(WorldCommandBar.MaxReservedPeopleRows, needed);
            Assert.True(settings.ReservedPeopleRows >= 1);
            // a pawn's buttons fit in what is kept (always so on a desktop window)
            int pawnRows = CommandBarLayout.countRows(settlementWithPawn(true).FindAll(c => c.getGroup() == CommandGroup.People), measure, settings);
            bool fits = pawnRows <= settings.ReservedPeopleRows;
            if (width >= 960f) {
                Assert.True(fits, "the pawn buttons fit the kept rows at " + width);
            }

            List<WorldCommand> alone = settlementWithPawn(false);
            List<WorldCommand> withPawn = settlementWithPawn(true);
            CommandBarLayout.Result placedAlone = CommandBarLayout.compute(alone, measure, settings);
            CommandBarLayout.Result placedWith = CommandBarLayout.compute(withPawn, measure, settings);

            // 0.8.0 re-run s05: Eat slid under a click meant for the market as the pawn row went
            foreach (WorldCommand command in alone) {
                if (!fits && command.getGroup() == CommandGroup.Craft) {
                    continue; // on a phone upright only the first kept rows hold still
                }
                CommandBarLayout.Rect a = rectOf(alone, placedAlone, command.getLabel());
                CommandBarLayout.Rect b = rectOf(withPawn, placedWith, command.getLabel());
                Assert.True(a.X == b.X && a.Y == b.Y, command.getLabel() + " moved when a pawn came into reach");
            }
            // the boxes and the panels hang off the bar's top, which stays put too
            if (fits) {
                Assert.Equal(placedAlone.Top, placedWith.Top);
            }
            // and nothing overlaps
            for (int i = 0; i < withPawn.Count; i++) {
                for (int j = i + 1; j < withPawn.Count; j++) {
                    Assert.False(placedWith.Buttons[i].overlaps(placedWith.Buttons[j]), withPawn[i].getLabel() + " overlaps " + withPawn[j].getLabel());
                }
            }
            // the pawn buttons sit in the kept rows, below Eat
            Assert.True(rectOf(withPawn, placedWith, "Order Charlotte Lewis: Gather Wood").Y > rectOf(withPawn, placedWith, "Eat Apple").Y);
        }

        [Fact]
        public void testTheKeptRows_AlsoCountWithNothingAfterThePawns() {
            CommandBarLayout.Settings settings = settingsFor(1280f, 720f, 0f, false, 1f, out _, out _);
            List<WorldCommand> alone = settlementWithPawn(false);
            alone.RemoveAll(c => c.getGroup() == CommandGroup.Craft);
            CommandBarLayout.Result without = CommandBarLayout.compute(alone, measure, settings);
            settings.ReservedPeopleRows = 2;
            CommandBarLayout.Result kept = CommandBarLayout.compute(alone, measure, settings);
            Assert.Equal(without.Top - 2f * (settings.ButtonHeight + settings.SpacingY), kept.Top, 3);
            // nothing below them moved
            for (int i = 0; i < alone.Count; i++) {
                Assert.Equal(without.Buttons[i].Y, kept.Buttons[i].Y);
            }
        }

        [Fact]
        public void testOutInTheWorld_NoRowsAreKept() {
            Simulation simulation = new Simulation(new GameConfig());
            CommandBarLayout.Settings settings = settingsFor(1280f, 720f, 0f, false, 1f, out _, out _);
            Assert.False(simulation.getPlayer().isCurrentlyInSettlement());
            Assert.Equal(0, WorldCommandBar.getReservedPeopleRows(simulation, measure, settings));
        }

        [Fact]
        public void testCountRows_WrapsLikeTheBar() {
            CommandBarLayout.Settings settings = settingsFor(1280f, 720f, 0f, false, 1f, out _, out _);
            Assert.Equal(0, CommandBarLayout.countRows(new List<WorldCommand>(), measure, settings));
            Assert.Equal(1, CommandBarLayout.countRows(new List<WorldCommand> { command("Gift Ada", CommandGroup.People) }, measure, settings));
            List<WorldCommand> standIns = WorldCommandBar.getPawnStandIns();
            CommandBarLayout.Result placed = CommandBarLayout.compute(standIns, measure, settings);
            HashSet<float> rows = new HashSet<float>();
            foreach (CommandBarLayout.Rect rect in placed.Buttons) {
                rows.Add(rect.Y);
            }
            Assert.Equal(rows.Count, CommandBarLayout.countRows(standIns, measure, settings));
        }

        // --- a phone's bar, a page at a time (#466) ----------------------------------------------------

        public static IEnumerable<object[]> PhoneBars() {
            // CSS pixels, with the controls' band main.js reserves; the rows the HUD leaves the bar
            yield return new object[] { 844f, 390f, 150f, 2 };
            yield return new object[] { 844f, 390f, 150f, 1 };
            yield return new object[] { 390f, 844f, 220f, 3 };
            yield return new object[] { 390f, 844f, 220f, 2 };
        }

        [Theory]
        [MemberData(nameof(PhoneBars))]
        public void onAPhone_EveryButtonIsOnSomePage_WithinTheRowsTheBarHas(float width, float height, float reserve, int maxRows) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, true, 1f, out WorldHud.Layout layout, out bool stackCentre);
            settings.Left = WorldHud.getBarLeft(layout, true, stackCentre);
            settings.MaxRows = maxRows;
            List<WorldCommand> commands = busySettlement();
            CommandBarLayout.Result first = CommandBarLayout.compute(commands, measure, settings);
            Assert.True(first.isPaged());
            float lowestTop = settings.Bottom - maxRows * settings.ButtonHeight - (maxRows - 1) * settings.SpacingY;

            int[] pagesShowing = new int[commands.Count];
            for (int page = 0; page < first.PageCount; page++) {
                CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings, page);
                Assert.Equal(page, result.Page);
                Assert.Equal(first.PageCount, result.PageCount);
                // More, and the bar's top, stay put whatever the page
                Assert.Equal(first.More, result.More);
                Assert.Equal(first.Top, result.Top);
                Assert.True(result.Top >= lowestTop - 0.01f, "page " + page + " takes more than " + maxRows + " rows");
                bool anyDrastic = false;
                bool anyOther = false;
                for (int i = 0; i < commands.Count; i++) {
                    if (!result.Shown[i]) {
                        continue;
                    }
                    pagesShowing[i]++;
                    CommandBarLayout.Rect button = result.Buttons[i];
                    string label = commands[i].getLabel();
                    Assert.True(button.X >= settings.Left && button.Right <= settings.Right + 0.01f, label + " runs off the bar");
                    Assert.True(button.Y >= result.Top - 0.01f && button.Bottom <= settings.Bottom + 0.01f, label + " runs out of the bar's rows");
                    Assert.False(button.overlaps(result.More), label + " is under More");
                    for (int j = i + 1; j < commands.Count; j++) {
                        if (result.Shown[j]) {
                            Assert.False(button.overlaps(result.Buttons[j]), label + " overlaps " + commands[j].getLabel());
                        }
                    }
                    if (commands[i].getGroup() == CommandGroup.Drastic) {
                        anyDrastic = true;
                    } else {
                        anyOther = true;
                    }
                }
                // the drastic buttons are still apart: a page of their own
                Assert.False(anyDrastic && anyOther, "page " + page + " mixes drastic and other buttons");
            }
            for (int i = 0; i < commands.Count; i++) {
                Assert.True(pagesShowing[i] == 1, commands[i].getLabel() + " is on " + pagesShowing[i] + " pages");
            }
            // the page wraps round, so More can keep counting up
            Assert.Equal(0, CommandBarLayout.compute(commands, measure, settings, first.PageCount).Page);
        }

        [Fact]
        public void aBarThatFitsItsRows_IsNotPaged_AndLaysOutAsBefore() {
            CommandBarLayout.Settings settings = settingsFor(1280f, 720f, 0f, false, 1f, out _, out _);
            List<WorldCommand> commands = busySettlement();
            Dictionary<string, CommandBarLayout.Rect> unlimited = place(commands, settings);
            settings.MaxRows = 12;
            CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings);
            Assert.False(result.isPaged());
            for (int i = 0; i < commands.Count; i++) {
                Assert.True(result.Shown[i]);
                Assert.Equal(unlimited[commands[i].getLabel()], result.Buttons[i]);
            }
        }

        [Fact]
        public void theRowsLeftUnderTheHud_AreCountedWhole_AndAtLeastOne() {
            Assert.Equal(3, CommandBarLayout.rowsBetween(100f, 200f, 30f, 4f));
            Assert.Equal(2, CommandBarLayout.rowsBetween(100f, 165f, 30f, 4f));
            Assert.Equal(1, CommandBarLayout.rowsBetween(190f, 200f, 30f, 4f));
            Assert.Equal("More 2/3", CommandBarLayout.getMoreLabel(1, 3));
        }

        [Fact]
        public void onATouchScreen_TheChronicleStripIsShorter_ToLeaveTheBarItsRows() {
            Assert.Equal(ChroniclePanel.RecentCount, ChroniclePanel.recentCount(false));
            Assert.True(ChroniclePanel.recentCount(true) < ChroniclePanel.RecentCount);
            Assert.True(ChroniclePanel.recentCount(true) >= 1);
        }

        [Fact]
        public void onAPhoneOnItsSide_TheBarKeepsBetweenTheColumns() {
            WorldHud.Layout landscape = WorldHud.Layout.compute(844f, 390f, 1f);
            Assert.False(WorldHud.isStackCentre(844f, landscape, true));
            Assert.Equal(landscape.LeftX + landscape.BoxWidth + landscape.Gap, WorldHud.getBarLeft(landscape, true, false));
            // upright, and on a desktop, the bar starts at the margin as before
            WorldHud.Layout upright = WorldHud.Layout.compute(390f, 844f, 1f);
            Assert.True(WorldHud.isStackCentre(390f, upright, true));
            Assert.Equal(upright.Margin, WorldHud.getBarLeft(upright, true, true));
            Assert.Equal(landscape.Margin, WorldHud.getBarLeft(landscape, false, false));
        }

        [Theory]
        [InlineData(844f, 390f, 140f, 2)]
        [InlineData(390f, 844f, 260f, 2)]
        [InlineData(390f, 844f, 260f, 1)]
        public void onAPhone_TheGreyedAppointTrader_IsOnAPawnPage_WithinTheBar(float width, float height, float reserve, int maxRows) {
            // --stage-trader (#493): a serf beside the player, the one trader slot taken
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            TraderShowcase.stage(simulation);
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
            int appoint = commands.FindIndex(c => c.getLabel().StartsWith("Appoint ") && c.getLabel().EndsWith(" Trader"));
            Assert.True(appoint >= 0, "no Appoint Trader on the bar");
            Assert.False(commands[appoint].isEnabled());

            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, true, 1f, out WorldHud.Layout layout, out bool stackCentre);
            settings.Left = WorldHud.getBarLeft(layout, true, stackCentre);
            settings.ReservedPeopleRows = WorldCommandBar.getReservedPeopleRows(simulation, measure, settings);
            settings.MaxRows = maxRows;
            CommandBarLayout.Result first = CommandBarLayout.compute(commands, measure, settings);
            int found = 0;
            for (int page = 0; page < first.PageCount; page++) {
                CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings, page);
                if (!result.Shown[appoint]) {
                    continue;
                }
                found++;
                CommandBarLayout.Rect button = result.Buttons[appoint];
                Assert.True(button.X >= settings.Left && button.Right <= settings.Right + 0.01f, "Appoint Trader runs off the bar");
                Assert.True(button.Y >= result.Top - 0.01f && button.Bottom <= settings.Bottom + 0.01f, "Appoint Trader is out of the bar's rows");
                Assert.False(button.overlaps(result.More));
                for (int i = 0; i < commands.Count; i++) {
                    if (i != appoint && result.Shown[i]) {
                        Assert.False(button.overlaps(result.Buttons[i]), "Appoint Trader overlaps " + commands[i].getLabel());
                    }
                }
            }
            Assert.Equal(1, found);
        }

        // --- #466 with #486: the kept pawn rows on a paged bar ----------------------------------------

        /** Every command on page page, by label (CommandBarLayout.compute with that page). */
        private static Dictionary<string, CommandBarLayout.Rect> pageOf(List<WorldCommand> commands, CommandBarLayout.Settings settings, int page,
                out CommandBarLayout.Result result) {
            result = CommandBarLayout.compute(commands, measure, settings, page);
            Dictionary<string, CommandBarLayout.Rect> shown = new Dictionary<string, CommandBarLayout.Rect>();
            for (int i = 0; i < commands.Count; i++) {
                if (result.Shown[i]) {
                    shown[commands[i].getLabel()] = result.Buttons[i];
                }
            }
            return shown;
        }

        [Theory]
        [InlineData(844f, 390f, 140f, 2)]   // a phone on its side, between the columns
        [InlineData(390f, 844f, 260f, 2)]   // upright
        [InlineData(390f, 844f, 260f, 1)]
        public void onAPagedBar_TheKeptPawnRowsCount_ButAddNoEmptyPage_AndAPawnMovesNoOtherButton(float width, float height, float reserve, int maxRows) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, true, 1f, out WorldHud.Layout layout, out bool stackCentre);
            settings.Left = WorldHud.getBarLeft(layout, true, stackCentre);
            int needed = CommandBarLayout.countRows(WorldCommandBar.getPawnStandIns(), measure, settings);
            settings.ReservedPeopleRows = System.Math.Min(WorldCommandBar.MaxReservedPeopleRows, needed);
            List<WorldCommand> alone = settlementWithPawn(false);
            List<WorldCommand> withPawn = settlementWithPawn(true);

            // the kept rows count toward MaxRows: a bar that would fit without them is paged with them
            CommandBarLayout.Settings roomy = settings;
            roomy.MaxRows = 0;
            CommandBarLayout.Result unpaged = CommandBarLayout.compute(alone, measure, roomy);
            roomy.ReservedPeopleRows = 0;
            CommandBarLayout.Result unkept = CommandBarLayout.compute(alone, measure, roomy);
            float step = settings.ButtonHeight + settings.SpacingY;
            int rowsWithout = (int) System.MathF.Round((settings.Bottom - unkept.Top + settings.SpacingY - settings.DrasticGap) / step);
            roomy.ReservedPeopleRows = settings.ReservedPeopleRows;
            roomy.MaxRows = rowsWithout;
            Assert.True(unpaged.Top < unkept.Top);
            Assert.True(CommandBarLayout.compute(alone, measure, roomy).isPaged(), "the kept rows were not counted toward MaxRows");

            settings.MaxRows = maxRows;
            CommandBarLayout.Result first = CommandBarLayout.compute(alone, measure, settings);
            CommandBarLayout.Result firstWith = CommandBarLayout.compute(withPawn, measure, settings);
            Assert.True(first.isPaged());
            // no pawn in reach: no page is left empty for one
            for (int page = 0; page < first.PageCount; page++) {
                Assert.NotEmpty(pageOf(alone, settings, page, out _));
            }
            // a pawn in reach adds pages at the end, and every other button keeps its page and place
            Assert.True(firstWith.PageCount > first.PageCount);
            Assert.Equal(first.Top, firstWith.Top);
            Assert.Equal(first.More, firstWith.More);
            for (int page = 0; page < first.PageCount; page++) {
                Dictionary<string, CommandBarLayout.Rect> before = pageOf(alone, settings, page, out _);
                Dictionary<string, CommandBarLayout.Rect> after = pageOf(withPawn, settings, page, out _);
                Assert.Equal(before, after);
            }
            // the pawns' pages hold only pawn buttons, after the drastic page
            bool sawDrastic = false;
            for (int page = 0; page < firstWith.PageCount; page++) {
                CommandBarLayout.Result result = CommandBarLayout.compute(withPawn, measure, settings, page);
                bool people = false;
                bool other = false;
                for (int i = 0; i < withPawn.Count; i++) {
                    if (!result.Shown[i]) {
                        continue;
                    }
                    CommandGroup group = withPawn[i].getGroup();
                    sawDrastic = sawDrastic || group == CommandGroup.Drastic;
                    if (group == CommandGroup.People) {
                        people = true;
                        Assert.True(sawDrastic, "a pawn page comes before the drastic page");
                    } else {
                        other = true;
                    }
                }
                Assert.False(people && other, "page " + page + " mixes pawn and other buttons");
            }
        }
    }
}
