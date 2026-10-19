using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.input;

namespace beyondnations.desktop.ui {

    /**
    * Draws the world's Chronicle over the world.
    *
    * Recent mode is a compact, translucent strip at the top centre, under the
    * status line and between the HUD's left and right box columns: the last
    * few entries, one line each, cut with "..." when they do not fit, and
    * fading as they age so the newest stands out. Full mode is a centred,
    * scrollable window with every kept entry, wrapped, and a filter for the
    * player's own nation. Entries about the player or the player's nation are
    * drawn in gold with a leading "*" in both views, so the highlight does not
    * rest on colour alone.
    *
    * Self-contained on purpose: it takes the screen size and places itself,
    * so the rest of the HUD's layout can change around it. The geometry and
    * the fade are pure functions, tested without a window.
    */
    public class ChroniclePanel {
        public const int RecentCount = 5;

        /** Ticks an entry stays at full strength in the recent strip (30 s at 50 tps). */
        public const int FreshTicks = 1500;
        /** Ticks after which an entry has faded as far as it will (3 min at 50 tps). */
        public const int FadedTicks = 9000;
        /** How faint an old entry gets: still readable, never gone. */
        public const float MinAlpha = 0.45f;

        /** Room left for the HUD's 200 px box columns and their margins. */
        public const float SideColumnReserve = 220f;
        public const float MinCompactWidth = 220f;
        public const float MaxCompactWidth = 480f;
        public const float ScreenMargin = 10f;
        /** Below the status line, which sits at y = 20. */
        public const float CompactTop = 52f;
        public const float StackGap = 4f;

        public const string Ellipsis = "...";
        public const string MineMarker = "* ";

        private static readonly Vector4 TextColour = new Vector4(0.93f, 0.93f, 0.93f, 1f);
        private static readonly Vector4 MineColour = new Vector4(1.00f, 0.82f, 0.35f, 1f);
        private static readonly Vector4 StampColour = new Vector4(0.62f, 0.70f, 0.78f, 1f);
        private static readonly Vector4 HintColour = new Vector4(0.62f, 0.62f, 0.62f, 1f);

        private const ImGuiWindowFlags CompactFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoInputs;

        private const ImGuiWindowFlags FullFlags =
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav;

        private int lastSeenTotal = -1;

        // --- pure layout ------------------------------------------------------

        /**
        * Opacity of an entry ageTicks old in the recent strip: 1 while fresh,
        * then linear down to MinAlpha, then held there.
        */
        public static float fadeAlpha(int ageTicks) {
            if (ageTicks <= FreshTicks) {
                return 1f;
            }
            if (ageTicks >= FadedTicks) {
                return MinAlpha;
            }
            float t = (float) (ageTicks - FreshTicks) / (FadedTicks - FreshTicks);
            return 1f - t * (1f - MinAlpha);
        }

        /**
        * Width of the recent strip: the gap between the HUD's box columns,
        * kept between MinCompactWidth and MaxCompactWidth, and never wider
        * than the screen less a margin either side.
        */
        public static float compactWidth(float screenWidth) {
            float between = screenWidth - 2f * SideColumnReserve;
            float width = Math.Clamp(between, MinCompactWidth, MaxCompactWidth);
            return Math.Max(0f, Math.Min(width, screenWidth - 2f * ScreenMargin));
        }

        /**
        * Where the recent strip starts: under the status line, or under
        * whatever sits below it in the top-centre column (the hunger warning),
        * whose bottom edge is aboveBottom (0 when nothing is there).
        */
        public static float compactTop(float aboveBottom) {
            return Math.Max(CompactTop, aboveBottom + StackGap);
        }

        /** Size of the full window: most of the screen, capped so lines stay readable. */
        public static Vector2 fullSize(float screenWidth, float screenHeight) {
            float width = Math.Max(Math.Min(screenWidth - 2f * ScreenMargin, 640f), 0f);
            float height = Math.Max(Math.Min(screenHeight - 140f, 520f), Math.Min(200f, screenHeight - 2f * ScreenMargin));
            return new Vector2(width, Math.Max(height, 0f));
        }

        /**
        * text if it fits in maxWidth, otherwise the longest prefix that fits
        * with "..." after it. measure gives a string's drawn width.
        */
        public static string fitToWidth(string text, float maxWidth, Func<string, float> measure) {
            if (string.IsNullOrEmpty(text) || measure(text) <= maxWidth) {
                return text;
            }
            int low = 0;
            int high = text.Length;
            while (low < high) {
                int mid = (low + high + 1) / 2;
                if (measure(text.Substring(0, mid).TrimEnd() + Ellipsis) <= maxWidth) {
                    low = mid;
                }
                else {
                    high = mid - 1;
                }
            }
            return text.Substring(0, low).TrimEnd() + Ellipsis;
        }

        /** The line as the HUD shows it, marked when it concerns the player. */
        public static string displayLine(ChronicleEntry entry, Player player) {
            return (entry.concerns(player) ? MineMarker : "") + entry.getLine();
        }

        /** Entries to list, oldest first; only the player's own when onlyMine. */
        public static List<ChronicleEntry> select(IReadOnlyList<ChronicleEntry> entries, Player player, bool onlyMine) {
            List<ChronicleEntry> selected = new List<ChronicleEntry>();
            foreach (ChronicleEntry entry in entries) {
                if (!onlyMine || entry.concerns(player)) {
                    selected.Add(entry);
                }
            }
            return selected;
        }

        // --- drawing ----------------------------------------------------------

        public void draw(float width, float height, Simulation simulation, ChronicleView view, float top = CompactTop) {
            Chronicle chronicle = simulation.getChronicle();
            if (chronicle == null || view == null) {
                return;
            }
            switch (view.getMode()) {
                case ChronicleMode.Recent:
                    drawRecent(width, top, chronicle, simulation.getPlayer(), simulation.getTickCounter().getTotalTicks());
                    break;
                case ChronicleMode.Full:
                    drawFull(width, height, chronicle, simulation.getPlayer(), view);
                    break;
            }
            if (view.getMode() != ChronicleMode.Full) {
                lastSeenTotal = -1;
            }
        }

        private void drawRecent(float width, float top, Chronicle chronicle, Player player, int nowTick) {
            float panelWidth = compactWidth(width);
            if (panelWidth <= 0f) {
                return;
            }
            ImGui.SetNextWindowPos(new Vector2(width / 2f, top), ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowSize(new Vector2(panelWidth, 0f), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.6f);
            if (ImGui.Begin("##chronicle-recent", CompactFlags)) {
                Overlay.textColoured(HintColour, "Chronicle  [" + KeyNames.describe(KeyBindings.ToggleChronicle) + ": more]");
                List<ChronicleEntry> recent = chronicle.getRecent(RecentCount);
                if (recent.Count == 0) {
                    Overlay.textColoured(HintColour, "Nothing worth telling has happened yet.");
                }
                float lineWidth = ImGui.GetContentRegionAvail().X;
                for (int i = recent.Count - 1; i >= 0; i--) {
                    // Newest first, so the strip reads top-down from now.
                    ChronicleEntry entry = recent[i];
                    float alpha = fadeAlpha(nowTick - entry.getTick());
                    bool mine = entry.concerns(player);
                    string line = fitToWidth(displayLine(entry, player), lineWidth, s => ImGui.CalcTextSize(s).X);
                    drawStampedLine(line, mine ? MineMarker.Length + entry.getTimestamp().Length : entry.getTimestamp().Length, mine, alpha);
                }
            }
            ImGui.End();
        }

        private void drawFull(float width, float height, Chronicle chronicle, Player player, ChronicleView view) {
            Vector2 size = fullSize(width, height);
            ImGui.SetNextWindowPos(new Vector2(width / 2f, height / 2f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(size, ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.88f);
            if (ImGui.Begin("Chronicle##chronicle-full", FullFlags)) {
                bool onlyMine = view.isOnlyMyNation();
                if (ImGui.Checkbox("Only my nation", ref onlyMine)) {
                    view.setOnlyMyNation(onlyMine);
                }
                ImGui.SameLine();
                if (ImGui.Button("Close")) {
                    view.setMode(ChronicleMode.Hidden);
                }
                ImGui.SameLine();
                Overlay.textColoured(HintColour, KeyNames.describe(KeyBindings.ToggleChronicle) + " cycles views");

                int kept = chronicle.getCount();
                int total = chronicle.getTotalRecorded();
                string counts = total > kept
                    ? total + " entries; the latest " + kept + " are kept."
                    : total + (total == 1 ? " entry." : " entries.");
                Overlay.textColoured(HintColour, counts);
                ImGui.Separator();

                if (ImGui.BeginChild("##chronicle-scroll", Vector2.Zero)) {
                    List<ChronicleEntry> entries = select(chronicle.getEntries(), player, onlyMine);
                    if (entries.Count == 0) {
                        Overlay.textColoured(HintColour, onlyMine
                            ? "Nothing about you or your nation yet."
                            : "Nothing worth telling has happened yet.");
                    }
                    bool atBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f;
                    ImGui.PushTextWrapPos(0f);
                    foreach (ChronicleEntry entry in entries) {
                        bool mine = entry.concerns(player);
                        Overlay.textColoured(StampColour, (mine ? MineMarker : "") + entry.getTimestamp());
                        ImGui.SameLine();
                        Overlay.textColoured(mine ? MineColour : TextColour, entry.getText());
                    }
                    ImGui.PopTextWrapPos();
                    // Follow the newest entry on opening and as entries
                    // arrive, unless the player has scrolled up to read.
                    if (lastSeenTotal < 0 || (total != lastSeenTotal && atBottom)) {
                        ImGui.SetScrollHereY(1f);
                    }
                    lastSeenTotal = total;
                }
                ImGui.EndChild();
            }
            ImGui.End();
        }

        /**
        * Draws "stamp - text" with the stamp dimmed. stampLength is how many
        * leading characters of line are the stamp (and marker); a line cut
        * short inside the stamp is drawn all in the stamp colour.
        */
        private static void drawStampedLine(string line, int stampLength, bool mine, float alpha) {
            Vector4 stamp = withAlpha(StampColour, alpha);
            Vector4 text = withAlpha(mine ? MineColour : TextColour, alpha);
            if (line.Length <= stampLength) {
                Overlay.textColoured(stamp, line);
                return;
            }
            Overlay.textColoured(stamp, line.Substring(0, stampLength));
            ImGui.SameLine(0f, 0f);
            Overlay.textColoured(text, line.Substring(stampLength));
        }

        private static Vector4 withAlpha(Vector4 colour, float alpha) {
            return new Vector4(colour.X, colour.Y, colour.Z, colour.W * alpha);
        }
    }
}
