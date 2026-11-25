namespace beyondnations.desktop.ui.journal {

    /**
    * Where everything on the Journal goes for a screen size: the title, the
    * row of five tabs, the summary strip, the grid of tiles and the detail
    * card a tapped tile opens. A pure function of the size, whether touch is
    * on and whether a card is open, so the tests can check it fits a phone
    * held either way (390x844, 844x390) without a window.
    *
    * Everything a finger presses is at least TouchLayout.MinTarget (44
    * pixels) each way, on the desktop too: the Journal is the same screen
    * for both.
    */
    public readonly struct JournalLayout {
        public const int TabCount = 5;

        public readonly float width;
        public readonly float height;
        public readonly float margin;
        public readonly float titleY;
        public readonly float titleHeight;
        public readonly float tabsX;
        public readonly float tabsY;
        public readonly float tabWidth;
        public readonly float tabHeight;
        public readonly float tabGap;
        public readonly float summaryY;
        public readonly float summaryHeight;
        public readonly float gridX;
        public readonly float gridY;
        public readonly float gridWidth;
        public readonly float gridHeight;
        public readonly float tile;
        public readonly float gap;
        public readonly int columns;
        public readonly bool detailBeside;
        public readonly float detailX;
        public readonly float detailY;
        public readonly float detailWidth;
        public readonly float detailHeight;
        public readonly float footerY;

        private JournalLayout(float width, float height, float margin, float titleY, float titleHeight,
                float tabsX, float tabsY, float tabWidth, float tabHeight, float tabGap,
                float summaryY, float summaryHeight, float gridX, float gridY, float gridWidth, float gridHeight,
                float tile, float gap, int columns, bool detailBeside,
                float detailX, float detailY, float detailWidth, float detailHeight, float footerY) {
            this.width = width;
            this.height = height;
            this.margin = margin;
            this.titleY = titleY;
            this.titleHeight = titleHeight;
            this.tabsX = tabsX;
            this.tabsY = tabsY;
            this.tabWidth = tabWidth;
            this.tabHeight = tabHeight;
            this.tabGap = tabGap;
            this.summaryY = summaryY;
            this.summaryHeight = summaryHeight;
            this.gridX = gridX;
            this.gridY = gridY;
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            this.tile = tile;
            this.gap = gap;
            this.columns = columns;
            this.detailBeside = detailBeside;
            this.detailX = detailX;
            this.detailY = detailY;
            this.detailWidth = detailWidth;
            this.detailHeight = detailHeight;
            this.footerY = footerY;
        }

        /** The design size of a tile before it is stretched to fill its row. */
        public const float DesignTile = 100f;
        public const float MaxTile = 140f;

        public static JournalLayout compute(float width, float height, bool touch, bool detailOpen) {
            float target = UiScale.px(TouchLayout.MinTarget);
            float margin = System.MathF.Max(UiScale.px(12f), width * 0.025f);
            float titleY = UiScale.px(8f);
            float titleHeight = UiScale.px(TouchLayout.MinTarget);

            // the tabs: one row of five, under the title (and the corner Close on touch)
            float tabGap = UiScale.px(6f);
            float tabHeight = System.MathF.Max(target, UiScale.px(48f));
            float tabsWidth = System.MathF.Min(width - margin * 2f, UiScale.px(680f));
            float tabWidth = (tabsWidth - tabGap * (TabCount - 1)) / TabCount;
            float tabsX = (width - tabsWidth) / 2f;
            float tabsY = titleY + titleHeight + UiScale.px(4f);
            if (touch) {
                // clear of the corner Close button (Overlay.touchCloseBottom)
                tabsY = System.MathF.Max(tabsY, UiScale.px(TouchLayout.MinTarget + 16f));
            }

            float summaryY = tabsY + tabHeight + UiScale.px(8f);
            float summaryHeight = touch ? target : UiScale.px(36f);

            // a line of help at the foot on the desktop; touch has its Close button instead
            float footerHeight = touch ? 0f : UiScale.px(22f);
            float footerY = height - footerHeight;
            float bottom = footerY - UiScale.px(6f);

            float gridX = margin;
            float gridY = summaryY + summaryHeight + UiScale.px(8f);
            float gridWidth = width - margin * 2f;
            float gridHeight = bottom - gridY;

            // the card: beside the grid on a wide screen, under it on a tall one
            bool beside = width >= height * 1.25f && width >= UiScale.px(640f);
            float detailX = 0f, detailY = 0f, detailWidth = 0f, detailHeight = 0f;
            if (detailOpen) {
                if (beside) {
                    detailWidth = System.MathF.Min(UiScale.px(360f), width * 0.38f);
                    detailX = width - margin - detailWidth;
                    detailY = gridY;
                    detailHeight = gridHeight;
                    gridWidth -= detailWidth + UiScale.px(10f);
                }
                else {
                    detailHeight = System.Math.Clamp(height * 0.28f, UiScale.px(150f), UiScale.px(260f));
                    detailX = margin;
                    detailWidth = width - margin * 2f;
                    detailY = bottom - detailHeight;
                    gridHeight -= detailHeight + UiScale.px(8f);
                }
            }

            float gap = UiScale.px(10f);
            // bigger on a bigger screen: about a sixth of its short side
            float design = System.Math.Clamp(System.MathF.Min(width, height) * 0.17f, UiScale.px(DesignTile), UiScale.px(MaxTile));
            int columns = System.Math.Max(1, (int) ((gridWidth + gap) / (design + gap)));
            float tile = System.MathF.Min(UiScale.px(MaxTile), (gridWidth - gap * (columns - 1)) / columns);
            tile = System.MathF.Max(tile, target);
            // a short window keeps at least one whole row of tiles
            if (gridHeight > target && tile > gridHeight) {
                tile = System.MathF.Max(target, gridHeight);
                columns = System.Math.Max(1, (int) ((gridWidth + gap) / (tile + gap)));
            }
            return new JournalLayout(width, height, margin, titleY, titleHeight, tabsX, tabsY, tabWidth, tabHeight, tabGap,
                summaryY, summaryHeight, gridX, gridY, gridWidth, gridHeight, tile, gap, columns, beside && detailOpen,
                detailX, detailY, detailWidth, detailHeight, footerY);
        }

        /** The left edge of tab i. */
        public float tabX(int i) {
            return tabsX + (tabWidth + tabGap) * i;
        }

        /** Where tile i sits in the grid before scrolling: its top left corner, relative to the grid's. */
        public void tileOffset(int i, out float x, out float y) {
            int row = i / columns;
            int column = i % columns;
            // the row is centred in the grid's width
            float used = columns * tile + (columns - 1) * gap;
            float indent = System.MathF.Max(0f, (gridWidth - used) / 2f);
            x = indent + column * (tile + gap);
            y = row * (tile + gap);
        }

        /** How tall count tiles stand in rows. */
        public float contentHeight(int count) {
            if (count <= 0) {
                return 0f;
            }
            int rows = (count + columns - 1) / columns;
            return rows * tile + (rows - 1) * gap;
        }

        /** How far the grid can scroll for count tiles. */
        public float maxScroll(int count) {
            return System.MathF.Max(0f, contentHeight(count) - gridHeight);
        }

        /** The tile under a point on the screen with the grid scrolled by scroll, or -1. */
        public int hitTest(int count, float scroll, float x, float y) {
            if (x < gridX || x > gridX + gridWidth || y < gridY || y > gridY + gridHeight) {
                return -1;
            }
            for (int i = 0; i < count; i++) {
                tileOffset(i, out float tx, out float ty);
                float left = gridX + tx;
                float top = gridY + ty - scroll;
                if (x >= left && x <= left + tile && y >= top && y <= top + tile) {
                    return i;
                }
            }
            return -1;
        }

        /** The tab under a point, or -1. */
        public int tabAt(float x, float y) {
            if (y < tabsY || y > tabsY + tabHeight) {
                return -1;
            }
            for (int i = 0; i < TabCount; i++) {
                if (x >= tabX(i) && x <= tabX(i) + tabWidth) {
                    return i;
                }
            }
            return -1;
        }
    }
}
