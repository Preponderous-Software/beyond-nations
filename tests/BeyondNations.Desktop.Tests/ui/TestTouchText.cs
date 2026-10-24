using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Silk.NET.Input;
using Xunit;
using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.feel;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * On a touch screen (TouchLayout) no line the player reads names a
    * keyboard key or a mouse button: each one names the on-screen button
    * that does the same (ObjectiveKeyNames). The lines are gathered from
    * every screen that names a control, the same calls the screens make.
    */
    [Collection("TouchLayout")]
    public class TestTouchText {

        private static Simulation newSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            return new Simulation(config);
        }

        /**
        * Every key name a player could be told to press, read from the
        * binding table so a new binding is covered too. "?" is left out: the
        * touch controls have a "?" button of their own.
        */
        private static List<string> boundKeyNames() {
            List<string> names = new List<string>();
            foreach (FieldInfo field in typeof(KeyBindings).GetFields(BindingFlags.Public | BindingFlags.Static)) {
                if (field.FieldType != typeof(Key)) {
                    continue;
                }
                string name = KeyNames.describe((Key) field.GetValue(null));
                if (name != "?" && !names.Contains(name)) {
                    names.Add(name);
                }
            }
            names.Add("Escape");
            names.Add("ESCAPE");
            return names;
        }

        private static readonly string[] MouseWords = { "click", "mouse", "right-drag", "keyboard", "any key" };

        /**
        * The key or mouse word a line names, or null. A single letter counts
        * as a key unless it is the word "A" or "I" in a sentence ("A merchant's
        * stall ...").
        */
        public static string namedKey(string line) {
            foreach (string name in boundKeyNames()) {
                string pattern = @"(?<![A-Za-z0-9'])" + Regex.Escape(name) + @"(?![A-Za-z0-9])";
                if (name.Length == 1 && (name == "A" || name == "I")) {
                    pattern += @"(?! [a-z])";
                }
                if (Regex.IsMatch(line, pattern)) {
                    return name;
                }
            }
            foreach (string word in MouseWords) {
                if (line.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) {
                    return word;
                }
            }
            return null;
        }

        private static void assertNamesNoKey(string line) {
            Assert.NotNull(line);
            Assert.DoesNotContain("{", line);
            string key = namedKey(line);
            Assert.True(key == null, "\"" + line + "\" names the key \"" + key + "\" on a touch screen");
        }

        /** Every player-facing line that names a control, as the screens build them now. */
        private static List<string> linesThatNameControls(Simulation simulation, Simulation staged, DiplomacyShowcase.Staged showcase) {
            List<string> lines = new List<string>();

            // objectives, ambitions and the era's requirements
            foreach (Objective objective in ObjectiveCatalog.firstArc()) {
                lines.Add(ObjectiveKeyNames.resolve(objective.getHint(simulation)));
            }
            for (int i = 0; i < ObjectiveCatalog.AmbitionCategoryCount; i++) {
                lines.Add(ObjectiveKeyNames.resolve(ObjectiveCatalog.ambition(i).getHint(simulation)));
            }
            foreach (string label in new[] { "Village", "Town", "Towns", "Members", "Stalls", "Settlements", "Funds", "Day" }) {
                lines.Add(ObjectiveKeyNames.resolve(EraRules.hintFor(new EraRequirement(label, 0, 3))));
            }

            // panel footers and help lines
            lines.Add(TitleScreen.continueHint());
            lines.Add(ObjectiveKeyNames.closeHint(InventoryScreen.CloseHint));
            lines.Add(ObjectiveKeyNames.closeHint(NationScreen.CloseHint));
            lines.Add(ObjectiveKeyNames.closeHint(CraftingScreen.CloseHint, CraftingScreen.TouchHint));
            lines.Add(ObjectiveKeyNames.closeHint(ControlsScreen.CloseHint, ControlsScreen.TouchCloseHint));
            lines.Add(ObjectiveKeyNames.resolve(ChroniclePanel.recentHeading(ChronicleScope.YourNews)));
            lines.Add(ObjectiveKeyNames.resolve(ChroniclePanel.CycleHint));
            foreach (OnboardingHints.Step step in OnboardingHints.Steps) {
                lines.Add(OnboardingHints.text(step, TouchLayout.isActive()));
            }

            // what the panels say without a nation
            lines.AddRange(LawsPanel.getLines(simulation));
            lines.AddRange(NationScreen.getHeaderLines(simulation));

            // the hunger warning
            Player player = simulation.getPlayer();
            player.setEnergy(WorldHud.StarvingEnergy - 1);
            lines.Add(WorldHud.getHungerWarning(player));
            player.setEnergy(WorldHud.HungryEnergy - 1);
            lines.Add(WorldHud.getHungerWarning(player));

            // a settlement without a Workshop, and the status line the
            // diplomacy showcase leaves, as the HUD draws it
            lines.Add(CraftingScreen.getLocationLine(simulation));
            lines.AddRange(new SettlementInfoBox(showcase.playerSettlement, staged.getNationRepository(), staged.getEntityRepository()).getLines());
            lines.Add(ObjectiveKeyNames.resolve(staged.getPlayer().getStatus().getStatus()));
            return lines;
        }

        [Fact]
        public void onATouchScreenNoLineNamesAKeyOrMouseButton() {
            Simulation simulation = newSimulation();
            Simulation staged = newSimulation();
            DiplomacyShowcase.Staged showcase = DiplomacyShowcase.stage(staged);
            // the crafting screen's location line, inside a settlement
            staged.getPlayer().setCurrentSettlementId(showcase.playerSettlement.getId());
            try {
                TouchLayout.setState(TouchMode.On, true, 120f);
                List<string> lines = linesThatNameControls(simulation, staged, showcase);
                lines.Add(CraftingScreen.getLocationLine(staged));
                foreach (string line in lines) {
                    assertNamesNoKey(line);
                }
            }
            finally {
                TouchLayout.reset();
            }
        }

        [Fact]
        public void onTheDesktopTheSameLinesStillNameTheKeys() {
            TouchLayout.reset();
            Simulation simulation = newSimulation();
            Simulation staged = newSimulation();
            DiplomacyShowcase.Staged showcase = DiplomacyShowcase.stage(staged);
            List<string> lines = linesThatNameControls(simulation, staged, showcase);
            Assert.Contains("(Press I or Esc to close, C to craft)", lines);
            Assert.Contains("(Press O or Esc to close)", lines);
            Assert.Contains("Chronicle: your news  [Y: more]", lines);
            Assert.Contains(lines, line => line.EndsWith("Press O to see the nations."));
            Assert.Contains(lines, line => line.StartsWith("Next (K): "));
            Assert.Contains(lines, line => line.Contains("Press E at a tree"));
        }

        [Fact]
        public void everyTouchButtonNameIsNotAKey() {
            foreach (HintKey key in (HintKey[]) Enum.GetValues(typeof(HintKey))) {
                assertNamesNoKey(ObjectiveKeyNames.ofTouch(key));
            }
        }

        [Fact]
        public void theControlsListPutsItsKeysUnderKeyboardHeadingsOnATouchScreen() {
            foreach (ControlsScreen.Row row in ControlsScreen.getRows(true)) {
                if (row.Section == ControlsScreen.TouchSection) {
                    assertNamesNoKey(row.Keys);
                    assertNamesNoKey(row.Action);
                }
                else {
                    Assert.StartsWith(ControlsScreen.KeyboardPrefix, row.Section);
                }
            }
            Assert.DoesNotContain(ControlsScreen.getRows(), row => row.Section.StartsWith(ControlsScreen.KeyboardPrefix));
        }

        [Fact]
        public void theKeyDetectorCatchesKeysButNotArticles() {
            Assert.Equal("O", namedKey("Press O to see the nations."));
            Assert.Equal("Esc", namedKey("(Press I or Esc to close)"));
            Assert.Equal("ESCAPE", namedKey("(Press ESCAPE to resume)"));
            Assert.Equal("Left Shift", namedKey("Hold Left Shift to run"));
            Assert.Equal("click", namedKey("Inside a settlement, click Sell Resources."));
            Assert.Equal("Y", namedKey("Chronicle: your news  [Y: more]"));
            Assert.Null(namedKey("A merchant's stall with coins buys wood and stone."));
            Assert.Null(namedKey("Tap Create Nation in the bar, then press Use."));
        }
    }
}
