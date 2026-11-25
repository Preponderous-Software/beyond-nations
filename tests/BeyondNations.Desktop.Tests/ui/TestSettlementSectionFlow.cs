using System.Collections.Generic;
using Xunit;

using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The settlement screen's sections flow down the first column and on
    * into the second. Each column used to stop at its foot, so at 1280x800
    * the Buildings section lost its "Upgrade a House to level 1" lines
    * with nothing to say they were there.
    */
    public class TestSettlementSectionFlow {

        private const float Heading = 20f;
        private const float Text = 17f;
        private const float Gap = 6f;

        /** A heading then n lines, for each n given. */
        private static List<bool> sections(params int[] lines) {
            List<bool> rows = new List<bool>();
            foreach (int n in lines) {
                rows.Add(true);
                for (int i = 0; i < n; i++) {
                    rows.Add(false);
                }
            }
            return rows;
        }

        /** No line wrapped: every row a line of its own. */
        private static List<bool> single(List<bool> rows) {
            return new List<bool>(new bool[rows.Count]);
        }

        [Fact]
        public void aWrappedLineIsNotSplitBetweenColumns() {
            // a heading, a line, then a line wrapped over three rows that would straddle the foot
            List<bool> rows = new List<bool> { true, false, false, false, false };
            List<bool> continues = new List<bool> { false, false, false, true, true };
            List<SettlementScreen.RowPlace> placed = SettlementScreen.layoutRows(rows, continues, 0, Text * 4, 2, Text, Text, 0f);
            Assert.Equal(rows.Count, placed.Count);
            Assert.Equal(0, placed[1].column);
            Assert.All(placed.GetRange(2, 3), place => Assert.Equal(1, place.column));
            Assert.Equal(0f, placed[2].y);
        }

        [Fact]
        public void rowsThatFitStayInTheFirstColumn() {
            List<bool> rows = sections(2, 1);
            List<SettlementScreen.RowPlace> placed = SettlementScreen.layoutRows(rows, single(rows), 0, 500f, 2, Heading, Text, Gap);
            Assert.Equal(rows.Count, placed.Count);
            Assert.All(placed, place => Assert.Equal(0, place.column));
            // the second heading has the gap above it
            Assert.Equal(Heading + Text * 2 + Gap, placed[3].y);
        }

        [Fact]
        public void aLongSectionCarriesOverIntoTheSecondColumn() {
            // room for six text rows a column; the second section's last lines go right
            List<bool> rows = sections(2, 6);
            List<SettlementScreen.RowPlace> placed = SettlementScreen.layoutRows(rows, single(rows), 0, Text * 6, 2, Text, Text, 0f);
            Assert.Equal(rows.Count, placed.Count);
            Assert.Equal(0, placed[5].column);
            Assert.Equal(1, placed[6].column);
            Assert.Equal(0f, placed[6].y);
            Assert.Equal(1, placed[rows.Count - 1].column);
        }

        [Fact]
        public void aHeadingIsNeverLeftAtTheFootOfAColumn() {
            // the heading would fit as the column's last row, but its first line would not
            List<bool> rows = sections(4, 2);
            List<SettlementScreen.RowPlace> placed = SettlementScreen.layoutRows(rows, single(rows), 0, Text * 6, 2, Text, Text, 0f);
            Assert.Equal(1, placed[5].column);
            Assert.Equal(0f, placed[5].y);
        }

        [Fact]
        public void whatDoesNotFitIsLeftForTheScroll() {
            List<bool> rows = sections(10, 10);
            List<SettlementScreen.RowPlace> placed = SettlementScreen.layoutRows(rows, single(rows), 0, Text * 5, 2, Text, Text, 0f);
            Assert.Equal(10, placed.Count);
            // scrolled on, the flow starts at the first row asked for
            List<SettlementScreen.RowPlace> scrolled = SettlementScreen.layoutRows(rows, single(rows), 15, Text * 5, 2, Text, Text, 0f);
            Assert.Equal(15, scrolled[0].row);
            Assert.Equal(rows.Count - 15, scrolled.Count);
        }

        [Fact]
        public void sectionsThatFitAreBalancedAndKeptWhole() {
            // Ownership 2, People 2, Buildings 3, Stores 2, Market 3: the left column takes the first three
            List<bool> rows = sections(2, 2, 3, 2, 3);
            List<SettlementScreen.RowPlace> placed = SettlementScreen.layoutSections(rows, single(rows), 0, 500f, 2, Heading, Text, Gap);
            Assert.Equal(rows.Count, placed.Count);
            int stores = 3 + 3 + 4;
            Assert.All(placed.GetRange(0, stores), place => Assert.Equal(0, place.column));
            Assert.All(placed.GetRange(stores, rows.Count - stores), place => Assert.Equal(1, place.column));
            Assert.Equal(0f, placed[stores].y);
        }

        [Fact]
        public void aSectionTooLongToKeepWholeFlowsOn() {
            // a Buildings section taller than a column: it carries over instead of being cut off
            List<bool> rows = sections(2, 9, 2);
            List<SettlementScreen.RowPlace> placed = SettlementScreen.layoutSections(rows, single(rows), 0, Text * 8, 2, Text, Text, 0f);
            Assert.Equal(rows.Count, placed.Count);
            Assert.Equal(1, placed[rows.Count - 1].column);
            Assert.Contains(placed, place => place.row == 12 && place.column == 1);
        }

        [Fact]
        public void onePhoneColumnStopsAtItsFoot() {
            List<bool> rows = sections(10);
            List<SettlementScreen.RowPlace> placed = SettlementScreen.layoutRows(rows, single(rows), 0, Text * 4, 1, Text, Text, 0f);
            Assert.Equal(4, placed.Count);
            Assert.All(placed, place => Assert.Equal(0, place.column));
        }
    }
}
