using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Silk.NET.Input;
using Xunit;

using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.input;
using beyondnations.desktop.render;
using beyondnations.desktop.text;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests {

    /**
    * The decisions behind the usability pass: the UI scale, key names, the
    * controls list, the status tone, the energy bar, the HUD layout, nametag
    * sizing and the night lights. None of it needs a window.
    */
    public class TestUsabilityFeel {

        [Fact]
        public void testUiScaleOverrideWinsAndIsClamped() {
            Assert.Equal(2f, UiScale.resolve(2f, 1.5f, 1f));
            Assert.Equal(UiScale.Max, UiScale.resolve(9f, 1f, 1f));
            Assert.Equal(UiScale.Min, UiScale.resolve(0.5f, 1f, 1f));
        }

        [Fact]
        public void testUiScaleFollowsTheLargerOfContentScaleAndFramebufferRatio() {
            Assert.Equal(1.5f, UiScale.resolve(0f, 1.5f, 1f));
            Assert.Equal(2f, UiScale.resolve(0f, 1f, 2f));
            Assert.Equal(1f, UiScale.resolve(0f, 0f, 0f));
            // Rounded to a quarter: Windows' 125 % stays 1.25, an odd 1.33 becomes 1.25.
            Assert.Equal(1.25f, UiScale.resolve(0f, 1.33f, 1f));
            Assert.Equal(1f, UiScale.resolve(0f, float.NaN, float.PositiveInfinity));
        }

        [Fact]
        public void testKeyNamesReadAsAPlayerWouldSayThem() {
            Assert.Equal("Left Shift", KeyNames.describe(Key.ShiftLeft));
            Assert.Equal("Page Up", KeyNames.describe(Key.PageUp));
            Assert.Equal("?", KeyNames.describe(Key.Slash));
            Assert.Equal("E", KeyNames.describe(Key.E));
            Assert.Equal("F12", KeyNames.describe(Key.F12));
        }

        [Fact]
        public void testEveryKeyBindingHasARowOnTheControlsScreen() {
            List<ControlsScreen.Row> rows = ControlsScreen.getRows();
            HashSet<string> keyCells = new HashSet<string>();
            foreach (ControlsScreen.Row row in rows) {
                foreach (string part in row.Keys.Split(" / ")) {
                    keyCells.Add(part);
                }
                Assert.False(string.IsNullOrWhiteSpace(row.Action));
                Assert.False(string.IsNullOrWhiteSpace(row.Section));
            }
            foreach (FieldInfo field in typeof(KeyBindings).GetFields(BindingFlags.Public | BindingFlags.Static)) {
                Key key = (Key) field.GetValue(null);
                Assert.True(keyCells.Contains(KeyNames.describe(key)),
                    "KeyBindings." + field.Name + " (" + key + ") is missing from the controls screen");
            }
        }

        [Fact]
        public void testRefusalsAreTonedAsFailures() {
            Assert.True(StatusTone.isFailure("Not enough wood. Need 20."));
            Assert.True(StatusTone.isFailure("You are not in a nation."));
            Assert.True(StatusTone.isFailure("No entities within range to interact with."));
            Assert.True(StatusTone.isFailure("There is no one within reach to invite."));
            Assert.True(StatusTone.isFailure("Mira: \"I don't even know you.\" (needs Acquaintance)"));
            Assert.True(StatusTone.isFailure("Mira is already a merchant."));
        }

        [Fact]
        public void testSuccessesAreNotTonedAsFailures() {
            Assert.False(StatusTone.isFailure("Gathered wood from tree."));
            Assert.False(StatusTone.isFailure("Stall built."));
            Assert.False(StatusTone.isFailure("You joined nation Arvon."));
            Assert.False(StatusTone.isFailure("Press N to create a nation."));
            Assert.False(StatusTone.isFailure(""));
            Assert.False(StatusTone.isFailure(null));
        }

        [Fact]
        public void testEnergyBarTiersMatchTheHungerWarning() {
            Assert.Equal(EnergyBar.Tier.Fed, EnergyBar.getTier(100f));
            Assert.Equal(EnergyBar.Tier.Peckish, EnergyBar.getTier(WorldHud.HungryEnergy * 2f - 1f));
            Assert.Equal(EnergyBar.Tier.Hungry, EnergyBar.getTier(WorldHud.HungryEnergy - 1f));
            Assert.Equal(EnergyBar.Tier.Starving, EnergyBar.getTier(WorldHud.StarvingEnergy - 1f));
            Assert.Equal("Hungry 29", EnergyBar.getLabel(29.7f));
            Assert.Equal("Energy 73", EnergyBar.getLabel(73f));
        }

        [Fact]
        public void testEnergyBarFractionIsClampedToTheBar() {
            Assert.Equal(1f, EnergyBar.getFraction(140f));
            Assert.Equal(0.5f, EnergyBar.getFraction(EnergyBar.FullEnergy / 2f));
            Assert.Equal(0f, EnergyBar.getFraction(-5f));
            Assert.Equal(0f, EnergyBar.getFraction(float.NaN));
        }

        [Theory]
        [InlineData(640f, 480f, 1f)]
        [InlineData(1024f, 768f, 1f)]
        [InlineData(1920f, 1080f, 1f)]
        [InlineData(2560f, 1600f, 2f)]
        [InlineData(400f, 700f, 1f)]
        public void testHudColumnsNeverOverlapAndStayOnScreen(float width, float height, float scale) {
            WorldHud.Layout layout = WorldHud.Layout.compute(width, height, scale);

            Assert.True(layout.BoxWidth > 0f);
            Assert.True(layout.LeftX >= 0f);
            Assert.True(layout.LeftX + layout.BoxWidth < layout.RightX, "the two columns overlap");
            Assert.True(layout.RightX + layout.BoxWidth <= width);
            Assert.True(layout.CentreWrapWidth <= width);
        }

        [Fact]
        public void testHudBoxesGrowWithTheUiScaleOnAWideScreen() {
            WorldHud.Layout normal = WorldHud.Layout.compute(3840f, 2160f, 1f);
            WorldHud.Layout doubled = WorldHud.Layout.compute(3840f, 2160f, 2f);

            Assert.Equal(normal.BoxWidth * 2f, doubled.BoxWidth, 3);
        }

        [Fact]
        public void testNametagsAreCappedOnScreenUpCloseAndUnchangedFarAway() {
            float yScale = 1f / System.MathF.Tan(System.MathF.PI / 6f); // 60 degree field of view
            float close = LabelCulling.getLineHeightAt(0.6f, 4f, yScale, 0.026f);
            float far = LabelCulling.getLineHeightAt(0.6f, 60f, yScale, 0.026f);

            Assert.True(close < 0.6f);
            // On screen, the close label covers exactly the cap.
            Assert.Equal(0.026f, close * yScale / (2f * 4f), 4);
            Assert.Equal(0.6f, far);
        }

        [Fact]
        public void testDefaultNametagFadeStartsWellInsideTheCullDistance() {
            LabelCulling culling = new LabelCulling();

            Assert.True(culling.getFadeDistance() < culling.getCullDistance());
            Assert.True(culling.getCullDistance() <= 100f);
        }

        [Fact]
        public void testNoNightLightsAtNoon() {
            NightLighting.Light[] lights = new NightLighting.Light[NightLighting.LightCount];

            Assert.Equal(0f, NightLighting.getDarkness(0.5f, 35f), 3);
            Assert.Equal(0, NightLighting.compute(0.5f, 35f, Vector3.Zero, lights));
        }

        [Fact]
        public void testAtMidnightALanternAndMoonlightFollowThePlayer() {
            NightLighting.Light[] lights = new NightLighting.Light[NightLighting.LightCount];
            Vector3 player = new Vector3(10f, 1f, -20f);

            int count = NightLighting.compute(0f, 35f, player, lights);

            Assert.Equal(1f, NightLighting.getDarkness(0f, 35f), 3);
            Assert.Equal(NightLighting.LightCount, count);
            foreach (NightLighting.Light light in lights) {
                Assert.Equal(player.X, light.Position.X);
                Assert.Equal(player.Z, light.Position.Z);
                Assert.True(light.Position.Y > player.Y);
                Assert.True(light.Intensity > 0f);
                Assert.True(light.Range > 0f);
            }
        }

        [Fact]
        public void testDuskIsDarkerThanNoonAndLighterThanMidnight() {
            float dusk = NightLighting.getDarkness(0.75f, 35f);

            Assert.InRange(dusk, 0.05f, 0.95f);
        }

        [Fact]
        public void testUiScaleOptionParses() {
            Assert.Equal(1.5f, GameOptions.parse(new string[] { "--ui-scale", "1.5" }).UiScale);
            Assert.Equal(0f, GameOptions.parse(new string[0]).UiScale);
            Assert.Equal(ScreenType.CONTROLS, GameOptions.parse(new string[] { "--start-screen", "controls" }).StartScreen);
        }
    }
}
