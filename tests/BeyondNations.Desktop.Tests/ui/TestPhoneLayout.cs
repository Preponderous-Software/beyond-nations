using System.Collections.Generic;
using Xunit;

using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The layout rules that keep the nations screen, the controls list and
    * the HUD readable and tappable on a phone (#467, #468, #469, #506, #511,
    * #513). Text is measured at a fixed 7 pixels a character, so the rules
    * can be checked without ImGui.
    */
    [Collection("TouchLayout")]
    public class TestPhoneLayout {

        private static float measure(string text) {
            return text.Length * 7f;
        }

        [Fact]
        public void aPhoneHeldUpright_SizesPanelTextByItsWidth() {
            // 390x844 counts as about 507 high, so text is 40% smaller than before
            Assert.Equal(390f * Overlay.NarrowTextAspect, Overlay.textHeightFor(390f, 844f));
        }

        [Theory]
        [InlineData(1280f, 720f)]
        [InlineData(960f, 540f)]
        [InlineData(844f, 390f)]
        [InlineData(1920f, 1080f)]
        public void desktopsAndAPhoneOnItsSide_KeepTheirTextSize(float width, float height) {
            Assert.Equal(height, Overlay.textHeightFor(width, height));
        }

        [Fact]
        public void panelButtonsAreFingertipSized_OnlyOnATouchScreen() {
            try {
                TouchLayout.reset();
                Assert.Equal(30f, Overlay.touchHeight(30f));
                TouchLayout.setState(TouchMode.On, true, 150f);
                Assert.True(Overlay.touchHeight(30f) >= TouchLayout.MinTarget);
                Assert.Equal(60f, Overlay.touchHeight(60f));
            } finally {
                TouchLayout.reset();
            }
        }

        [Fact]
        public void theListEndsAboveTheStatusBand() {
            // three status lines grow up from the footer at height - 1.6 lines,
            // 0.75 of a line apart; the highest one's top is under listBottom
            float height = 844f;
            float line = 26f;
            float highestStatusCentre = height - line * 1.6f - line * 0.75f * (NationScreen.StatusLines - 1);
            Assert.True(NationScreen.listBottom(height, line) <= highestStatusCentre - line * 0.4f);
        }

        [Fact]
        public void aHeaderThatFits_IsWrappedWhole() {
            List<string> header = new List<string> {
                "Your nation: Southernland, 5 members, 1 village. You lead it.",
                "Members: 5. Succession: the heir, then the longest serving member. Traders: 1 of 1."
            };
            List<string> lines = NationScreen.wrapHeader(header, 200f, measure, 20);
            string joined = string.Join(" ", lines);
            Assert.Contains("Traders: 1 of 1.", joined);
            Assert.All(lines, line => Assert.True(measure(line) <= 200f));
        }

        [Fact]
        public void aHeaderTooTall_IsCutWithAnEllipsis() {
            List<string> header = new List<string> {
                "Tribute 50 coins for +10 (and a truce from a stronger foe). An alliance needs +30. Hostile neighbours raid and besiege.",
                "Each dealing sends your envoy for 30 s of play before you can deal with that nation again."
            };
            List<string> lines = NationScreen.wrapHeader(header, 200f, measure, 3);
            Assert.Equal(3, lines.Count);
            Assert.EndsWith(CommandBarLayout.Ellipsis, lines[2]);
            Assert.True(measure(lines[2]) <= 200f);
        }

        [Fact]
        public void aHeaderWithNoRoomForARow_ScrollsWithTheList() {
            Assert.True(NationScreen.headerFits(4, 18f, 80f));
            Assert.False(NationScreen.headerFits(8, 18f, 80f));
            // with only the header lines in view, the count line says the nations are below
            string shown = NationScreen.describeShown(0, 0, 2, true);
            Assert.Contains("2 other nations below", shown);
            Assert.DoesNotContain("1-0", shown);

            // two header lines in front of three rows
            float[] items = NationScreen.withHeader(new[] { 50f, 60f, 70f }, 2, 18f);
            Assert.Equal(new[] { 18f, 18f, 50f, 60f, 70f }, items);
            int first;
            int count;
            NationScreen.shownRows(0, 3, 2, out first, out count);
            Assert.Equal(0, first);
            Assert.Equal(1, count);
            NationScreen.shownRows(3, 2, 2, out first, out count);
            Assert.Equal(1, first);
            Assert.Equal(2, count);
            NationScreen.shownRows(0, 2, 2, out first, out count);
            Assert.Equal(0, count);
            Assert.Equal("3 members below. Drag or tap Down.", NationScreen.describeBelow(3, "member", "members", true));
        }

        [Fact]
        public void aNarrowControlsList_WrapsEachRowIntoItsColumns() {
            List<ControlsScreen.Row> rows = new List<ControlsScreen.Row> {
                new ControlsScreen.Row("Touch screen", "Button row", "Bag, Craft, Realm (nations), News, View, ?, Menu"),
                new ControlsScreen.Row("Touch screen", "Use", "Gather")
            };
            List<ControlsScreen.Row> wrapped = ControlsScreen.wrapRows(rows, 100f, 160f, measure);
            Assert.True(wrapped.Count > 2);
            Assert.Equal("Button row", wrapped[0].Keys);
            Assert.False(wrapped[0].Continues);
            Assert.True(wrapped[1].Continues);
            Assert.All(wrapped, row => Assert.True(measure(row.Action) <= 160f));
            ControlsScreen.Row last = wrapped[wrapped.Count - 1];
            Assert.Equal("Use", last.Keys);
            Assert.False(last.Continues);
            // nothing is lost
            string actions = string.Join(" ", wrapped.FindAll(r => r.Keys != "Use").ConvertAll(r => r.Action));
            Assert.Equal(rows[0].Action, actions);
        }

        [Fact]
        public void aWrappedControlsRow_IsNotSplitAcrossColumns() {
            List<ControlsScreen.Row> rows = new List<ControlsScreen.Row> {
                new ControlsScreen.Row("S", "A", "one"),
                new ControlsScreen.Row("S", "B", "two"),
                new ControlsScreen.Row("S", "C", "three"),
                new ControlsScreen.Row("S", "", "three, continued", true),
            };
            // heading + A + B fill a column of 3; C and its continuation go on together
            int[] column = ControlsScreen.assignColumns(rows, 4);
            Assert.Equal(column[2], column[3]);
            Assert.Equal(column[0], column[1]);
            Assert.NotEqual(column[1], column[2]);
        }

        [Fact]
        public void upright_TheLeftColumnTakesTheWidthThePlayerBoxLeaves() {
            WorldHud.Layout upright = WorldHud.Layout.compute(390f, 844f, 1f);
            Assert.True(WorldHud.isStackCentre(390f, upright, true));
            float left = WorldHud.getLeftColumnWidth(upright, true);
            Assert.Equal(upright.RightX - upright.Gap - upright.LeftX, left);
            Assert.True(left > upright.BoxWidth * 1.5f);
            // on its side and on the desktop the columns keep a box's width
            WorldHud.Layout landscape = WorldHud.Layout.compute(844f, 390f, 1f);
            Assert.Equal(landscape.BoxWidth, WorldHud.getLeftColumnWidth(landscape, WorldHud.isStackCentre(844f, landscape, true)));
            WorldHud.Layout desktop = WorldHud.Layout.compute(1280f, 720f, 1f);
            Assert.Equal(desktop.BoxWidth, WorldHud.getLeftColumnWidth(desktop, false));
        }

        [Fact]
        public void theInventoryBoxIsLeftOut_WhenItWouldCoverTheBar() {
            Assert.True(WorldHud.fitsAbove(100f, 400f, 120f));
            Assert.False(WorldHud.fitsAbove(350f, 400f, 120f));
        }
    }
}
