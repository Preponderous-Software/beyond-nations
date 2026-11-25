using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.feel;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * Inside a settlement the right-hand column holds four boxes. They used
    * to be judged by one row a line, so wrapped lines and a growing price
    * list made the column taller than the window and the season box was
    * drawn over the player box (#350, playtest 0.7.0 s09). They are now
    * measured as drawn, and given way step by step until they fit.
    */
    public class TestSettlementColumn {

        // ProggyClean, the ImGui default font, with ImGui's default style
        private static readonly InfoBox.Metrics Metrics = new InfoBox.Metrics {
            LineHeight = 17f,
            FrameRowHeight = 23f,
            PaddingX = 8f,
            PaddingY = 8f,
            Measure = text => text.Length * 7f,
        };

        private class LinesBox : InfoBox {
            private readonly List<string> lines;

            public LinesBox(params string[] lines) : base("Lines") {
                this.lines = new List<string>(lines);
            }

            public override List<string> getLines() {
                return lines;
            }
        }

        private class Column {
            public Simulation Simulation;
            public SettlementInfoBox Settlement;
            public MarketInfoBox Market;
            public SeasonInfoBox Season;
            public PlayerInfoBox Player;

            public InfoBox[] all() {
                return new InfoBox[] { Settlement, Market, Season, Player };
            }
        }

        /** The staged settlement, with every good on a stall, so the price list is as long as it gets. */
        private static Column stagedColumn() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Settlement settlement = SettlementShowcase.stage(simulation);
            Stall stall = settlement.getMarket().getStalls()[0];
            foreach (ItemType good in new[] { ItemType.CHICKEN_MEAT, ItemType.COOKED_MEAT, ItemType.PLANKS, ItemType.BRICKS }) {
                stall.getInventory().addItem(good, 5);
            }
            return new Column {
                Simulation = simulation,
                Settlement = new SettlementInfoBox(settlement, simulation.getNationRepository(), simulation.getEntityRepository()),
                Market = new MarketInfoBox(settlement.getMarket(), simulation.getEntityRepository()),
                Season = new SeasonInfoBox(simulation),
                Player = new PlayerInfoBox(simulation.getPlayer(), simulation.getNationRepository(), simulation.getEntityRepository()),
            };
        }

        [Fact]
        public void aLineTooLongForTheBoxIsCountedOnceForEachRowItWrapsTo() {
            // 200 wide leaves 184 for text: 26 characters a row
            LinesBox box = new LinesBox("Funds: 0", "Next (K): House, 20 wood, 15 stone");
            Assert.Equal(17f * (1 + 1 + 2) + 16f, box.estimateHeight(200f, Metrics));
            Assert.Equal(17f * (1 + 1 + 1) + 16f, box.estimateHeight(400f, Metrics));
        }

        [Fact]
        public void theEnergyBarIsMeasuredAsTheFramedRowItIs() {
            Column column = stagedColumn();
            column.Player.setCompact(true);
            // title, the bar, and "Nation: <name> (LEADER)" at 400 wide
            Assert.Equal(17f * 3 + (23f - 17f) + 16f, column.Player.estimateHeight(400f, Metrics));
        }

        [Fact]
        public void aShortMarketBox_ListsWhatFits_AndSaysHowManyMoreThereAre() {
            Column column = stagedColumn();
            MarketInfoBox market = column.Market;
            market.setCompact(true);
            int listed = market.getListedGoods().Count;
            float full = market.estimateHeight(200f, Metrics);
            market.fitHeight(full - Metrics.LineHeight * 3f, 200f, Metrics);
            int shown = market.getShownGoods().Count;
            Assert.InRange(shown, 1, listed - 1);
            Assert.True(market.estimateHeight(200f, Metrics) <= full - Metrics.LineHeight * 3f);
            List<string> lines = market.getLines();
            Assert.Equal(MarketInfoBox.describeMoreGoods(listed - shown), lines[lines.Count - 1]);
            // the staples lead, so they are the last to go
            Assert.Equal(ItemType.APPLE, market.getShownGoods()[0]);
            // more room puts everything back
            market.fitHeight(2000f, 200f, Metrics);
            Assert.Equal(listed, market.getShownGoods().Count);
        }

        [Fact]
        public void theMoreGoodsLineCountsOneOrMany() {
            Assert.Equal("+1 more good", MarketInfoBox.describeMoreGoods(1));
            Assert.Equal("+4 more goods", MarketInfoBox.describeMoreGoods(4));
        }

        /**
        * At 412x915 the bar's rows rose past 60 % of the height and the tip
        * card hid Build House; it now sits above the bar.
        */
        [Fact]
        public void theTipCardRisesAboveTheCommandBar_AndStaysPutWhenThereIsRoom() {
            // room under the card: unchanged
            Assert.Equal(432f, FeelDirector.hintCardTopAboveBar(432f, 80f, 580f, 10f, 144f));
            // the bar's rows start at 505 on a 412x915 window
            Assert.Equal(505f - 10f - 80f, FeelDirector.hintCardTopAboveBar(549f, 80f, 505f, 10f, 183f));
            // never above minTop
            Assert.Equal(183f, FeelDirector.hintCardTopAboveBar(549f, 80f, 200f, 10f, 183f));
            // no bar drawn
            Assert.Equal(549f, FeelDirector.hintCardTopAboveBar(549f, 80f, 0f, 10f, 183f));
        }

        /**
        * A phone held upright (412x915, touch): the tip card went under the
        * Season box and over the command bar's buttons. Numbers from the
        * real touch layout, in CSS pixels.
        */
        [Fact]
        public void onAnUprightPhoneTheTipCardCoversNeitherTheSeasonBoxNorTheBar() {
            // under the Season box when it fits above the bar
            Assert.Equal(553f, FeelDirector.hintCardTopStacked(553f, 296f, 444f, 0f, 0f, 5f));
            Assert.Equal(400f, FeelDirector.hintCardTopStacked(400f, 296f, 300f, 100f, 553f, 5f));
            // in a town (Season box 444 to 543, bar from 553): under the
            // status line, over the chronicle strip, clear of the Season box
            float top = FeelDirector.hintCardTopStacked(548f, 296f, 444f, 100f, 553f, 5f);
            Assert.Equal(296f, top);
            Assert.True(top + 100f + 5f <= 444f);
            // neither fits: as low as it can sit above the bar, never above the status line
            Assert.Equal(553f - 5f - 100f, FeelDirector.hintCardTopStacked(500f, 360f, 430f, 100f, 553f, 5f));
            Assert.Equal(360f, FeelDirector.hintCardTopStacked(500f, 360f, 430f, 300f, 553f, 5f));
            // no fallback given: where it was asked for
            Assert.Equal(548f, FeelDirector.hintCardTopStacked(548f, 0f, 444f, 100f, 553f, 5f));
        }
    }
}
