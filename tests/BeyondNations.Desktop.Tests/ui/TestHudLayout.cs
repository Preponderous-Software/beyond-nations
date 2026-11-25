using System.Collections.Generic;
using Xunit;

using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The contextual HUD's geometry and badges: a chip or box is kept off the
    * no-go rects (the command bar and the touch controls) at any size, moved
    * the least distance; and a chip's badge marks what is urgent with text
    * as well as colour.
    */
    public class TestHudLayout {

        // --- no-go rects: the command bar and the touch controls --------------------

        [Theory]
        [InlineData(1280f, 720f)]
        [InlineData(390f, 844f)]
        [InlineData(844f, 390f)]
        public void aChip_keepsOffTheCommandBar_andTheTouchControls(float width, float height) {
            HudRect touchBand = new HudRect(0f, height - 120f, width, 120f);
            HudRect bar = new HudRect(10f, height - 120f - 100f, width - 20f, 90f);
            List<HudRect> noGo = new List<HudRect> { bar, touchBand };
            // a chip wanted right on the bar
            HudRect wanted = new HudRect(20f, bar.Y + 10f, 160f, 44f);
            Assert.False(HudLayout.clearOf(wanted, noGo));
            HudRect placed = HudLayout.avoid(wanted, width, height, noGo);
            Assert.True(HudLayout.clearOf(placed, noGo), placed.ToString());
            Assert.True(placed.Y >= 0f && placed.Bottom <= height);
            // the least move: up above the bar, not off to a far corner
            Assert.True(placed.Bottom <= bar.Y + 0.01f, placed.ToString());
            Assert.Equal(wanted.X, placed.X);
        }

        [Fact]
        public void avoid_leavesABoxThatIsAlreadyClear() {
            HudRect rect = new HudRect(10f, 10f, 100f, 40f);
            HudRect[] bar = { new HudRect(0f, 600f, 1280f, 120f) };
            Assert.Equal(rect.X, HudLayout.avoid(rect, 1280f, 720f, bar).X);
            Assert.Equal(rect.Y, HudLayout.avoid(rect, 1280f, 720f, bar).Y);
        }

        [Fact]
        public void avoid_withNoRoomAnywhere_overlapsAsLittleAsItCan() {
            HudRect everything = new HudRect(0f, 0f, 400f, 300f);
            HudRect placed = HudLayout.avoid(new HudRect(50f, 50f, 100f, 40f), 400f, 300f, new[] { everything });
            Assert.True(placed.X >= 0f && placed.Y >= 0f);
        }

        [Fact]
        public void avoid_keepsTheBoxOnTheScreen() {
            HudRect placed = HudLayout.avoid(new HudRect(1250f, 700f, 100f, 40f), 1280f, 720f, new List<HudRect>());
            Assert.True(placed.Right <= 1280f && placed.Bottom <= 720f, placed.ToString());
        }

        [Fact]
        public void aSecondChip_keepsOffTheFirst_asWellAsTheBar() {
            HudRect bar = new HudRect(0f, 600f, 1280f, 110f);
            List<HudRect> placed = new List<HudRect> { bar };
            HudRect first = HudLayout.avoid(new HudRect(10f, 100f, 200f, 30f), 1280f, 720f, placed);
            placed.Add(first);
            HudRect second = HudLayout.avoid(new HudRect(10f, 110f, 200f, 30f), 1280f, 720f, placed);
            Assert.True(HudLayout.clearOf(second, placed), second.ToString());
        }

        // --- badges: what is urgent is marked with text, not only colour ------------

        [Fact]
        public void theChipLabel_marksABadgeWithText_notOnlyColour() {
            Assert.Equal("Carrying 3 wood", HudPanels.chipLabel("Carrying 3 wood", HudBadge.None));
            Assert.Equal("Food: 9 of 84 !", HudPanels.chipLabel("Food: 9 of 84", HudBadge.Warning));
            // a line that ends in "!" already says so
            Assert.Equal("Winter in 1 day!", HudPanels.chipLabel("Winter in 1 day!", HudBadge.Warning));
            Assert.Equal("Besieged !", HudPanels.chipLabel("Besieged", HudBadge.Danger));
            Assert.Equal("News *", HudPanels.chipLabel("News", HudBadge.New));
        }

        // --- a chip too long for its room wraps rather than losing its end -----------

        private static float measure(string s) { return s.Length; }

        [Fact]
        public void aChipThatFits_isOneLine() {
            Assert.Equal(new List<string> { "Carrying 3 wood" }, HudPanels.chipLines("Carrying 3 wood", 40f, measure, HudPanels.ChipMaxLines));
        }

        [Fact]
        public void aLongChip_wrapsAndKeepsItsEnd() {
            string text = "Food: 52 of 56 for winter - Too little food, 22 rot";
            List<string> lines = HudPanels.chipLines(text, 26f, measure, HudPanels.ChipMaxLines);
            Assert.True(lines.Count > 1 && lines.Count <= HudPanels.ChipMaxLines);
            Assert.Equal(text, string.Join(" ", lines));
            Assert.All(lines, line => Assert.True(line.Length <= 26));
        }

        [Fact]
        public void aChipLongerThanItsLines_cutsOnlyItsLastLine() {
            string text = "one two three four five six seven eight nine ten eleven twelve";
            List<string> lines = HudPanels.chipLines(text, 10f, measure, 2);
            Assert.Equal(2, lines.Count);
            Assert.Equal("one two", lines[0]);
            Assert.EndsWith(ChroniclePanel.Ellipsis, lines[1]);
            Assert.True(lines[1].Length <= 10);
        }

        // --- a window held upright stacks like a phone; the market only when it fits ---

        [Fact]
        public void aPortraitWindow_stacksLikeAPhone() {
            Assert.True(WorldHud.isPortrait(390f, 844f));
            Assert.False(WorldHud.isPortrait(960f, 540f));
            Assert.True(WorldHud.boxFits(120f, 200f));
            Assert.False(WorldHud.boxFits(240f, 200f));
        }

        [Fact]
        public void hunger_marksThePlayersBadge() {
            Assert.Equal(HudBadge.None, PlayerInfoBox.badgeFor(80f));
            Assert.Equal(HudBadge.Warning, PlayerInfoBox.badgeFor(WorldHud.HungryEnergy - 1f));
            Assert.Equal(HudBadge.Danger, PlayerInfoBox.badgeFor(WorldHud.StarvingEnergy - 1f));
        }

        [Fact]
        public void winterComingOrFamine_marksTheSeasonBadge() {
            Assert.Equal(HudBadge.None, SeasonInfoBox.badgeFor(false, false));
            Assert.Equal(HudBadge.Warning, SeasonInfoBox.badgeFor(true, false));
            Assert.Equal(HudBadge.Warning, SeasonInfoBox.badgeFor(false, true));
        }

        [Fact]
        public void collapseOrSiege_marksTheRealmAlert() {
            Assert.Equal(HudBadge.None, EraBox.badgeFor(null, null, null));
            Assert.Equal(HudBadge.Warning, EraBox.badgeFor(null, null, "Sieges can begin in 4:12"));
            Assert.Equal(HudBadge.Danger, EraBox.badgeFor(null, "Your village: Besieged!", null));
            Assert.Equal(HudBadge.Danger, EraBox.badgeFor("Collapsing! 4:12 left", null, null));
        }
    }
}
