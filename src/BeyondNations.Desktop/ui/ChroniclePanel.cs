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
    * Both views list the player's news by default (ChronicleFocus: the
    * player, their nation, its neighbours and world news) and can show every
    * nation instead (ChronicleView.getScope). The strip's heading says which;
    * the full window has the switch as two buttons, a fingertip tall on a
    * touch screen, and KeyBindings.ToggleChronicleScope flips it from either
    * view. Why: in the 0.6.2 playtest (docs/playtests/0.6.2, s04) AI nations'
    * rivalries and raids filled 495 entries by day 2 and buried the player's.
    *
    * Self-contained on purpose: it takes the screen size and places itself,
    * so the rest of the HUD's layout can change around it. The geometry and
    * the fade are pure functions, tested without a window.
    */
    public class ChroniclePanel {
        public const int RecentCount = 5;

        /** The full view's help, with key tokens (ObjectiveKeyNames); the strip's heading is recentHeading. */
        public const string CycleHint = "{ToggleChronicle} cycles views";

        /** The full window's switch: "Showing: [Your news] [All nations]". */
        public const string ShowingLabel = "Showing:";
        public const string YourNewsLabel = "Your news";
        public const string AllNationsLabel = "All nations";

        private static readonly Vector4 ChosenColour = new Vector4(0.26f, 0.59f, 0.98f, 1f);
        private static readonly Vector4 UnchosenColour = new Vector4(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Vector4 UnchosenHoverColour = new Vector4(0.30f, 0.33f, 0.38f, 1f);

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
        private ChronicleScope lastSeenScope = ChronicleScope.YourNews;

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

        /** As displayLine, but marked by the focus, which knows the nations the player has lost as well. */
        public static string displayLine(ChronicleEntry entry, ChronicleFocus focus) {
            return (focus.isPlayers(entry) ? MineMarker : "") + entry.getLine();
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

        /**
        * Entries to list, oldest first: the player's news (focus) or every
        * entry, by scope, and of those only the player's own when onlyMine.
        * A null focus lists every entry.
        */
        public static List<ChronicleEntry> select(IReadOnlyList<ChronicleEntry> entries, Player player, ChronicleFocus focus, ChronicleScope scope, bool onlyMine) {
            List<ChronicleEntry> selected = new List<ChronicleEntry>();
            foreach (ChronicleEntry entry in entries) {
                if (inScope(entry, focus, scope) && (!onlyMine || (focus != null ? focus.isPlayers(entry) : entry.concerns(player)))) {
                    selected.Add(entry);
                }
            }
            return selected;
        }

        /** The last count entries in scope, oldest first, for the recent strip. */
        public static List<ChronicleEntry> recent(IReadOnlyList<ChronicleEntry> entries, ChronicleFocus focus, ChronicleScope scope, int count) {
            if (scope == ChronicleScope.AllNations || focus == null) {
                int start = Math.Max(0, entries.Count - Math.Max(0, count));
                List<ChronicleEntry> all = new List<ChronicleEntry>();
                for (int i = start; i < entries.Count; i++) {
                    all.Add(entries[i]);
                }
                return all;
            }
            return focus.recent(entries, count);
        }

        private static bool inScope(ChronicleEntry entry, ChronicleFocus focus, ChronicleScope scope) {
            return scope == ChronicleScope.AllNations || focus == null || focus.includes(entry);
        }

        /** The strip's heading, naming the scope, with key tokens: "Chronicle: your news  [{ToggleChronicle}: more]". */
        public static string recentHeading(ChronicleScope scope) {
            return "Chronicle: " + ChronicleView.scopeName(scope) + "  [{ToggleChronicle}: more]";
        }

        /** What the full window says when nothing is listed. */
        public static string emptyMessage(ChronicleScope scope, bool onlyMine) {
            if (onlyMine) {
                return "Nothing about you or your nation yet.";
            }
            return scope == ChronicleScope.YourNews
                ? "Nothing about you, your nation or its neighbours yet."
                : "Nothing worth telling has happened yet.";
        }

        /** The full window's count line: how many were told, kept and, when filtered, listed. */
        public static string countLine(int total, int kept, int listed) {
            string counts = total > kept
                ? total + " entries; the latest " + kept + " are kept."
                : total + (total == 1 ? " entry." : " entries.");
            if (listed < kept) {
                counts += " Listing " + listed + ".";
            }
            return counts;
        }

        // --- drawing ----------------------------------------------------------

        private ChronicleFocus focus;
        private int focusTick = -1;
        private Simulation focusSimulation;

        /**
        * The player's news as the world stands, worked out at most once a
        * tick: it walks every nation's frontier, and the HUD draws far more
        * often than the world changes.
        */
        private ChronicleFocus focusFor(Simulation simulation) {
            int tick = simulation.getTickCounter().getTotalTicks();
            if (focus == null || tick != focusTick || simulation != focusSimulation) {
                focus = ChronicleFocus.of(simulation);
                focusTick = tick;
                focusSimulation = simulation;
            }
            return focus;
        }

        /**
        * Draws the current view. Returns the bottom edge of the recent strip,
        * or top when no strip is drawn, so whatever the HUD centres under it
        * (the objective toast) can start there. maxWidth caps the strip at
        * the space the HUD's box columns leave in the middle.
        */
        public float draw(float width, float height, Simulation simulation, ChronicleView view, float top = CompactTop, float maxWidth = float.MaxValue) {
            Chronicle chronicle = simulation.getChronicle();
            if (chronicle == null || view == null) {
                return top;
            }
            float bottom = top;
            switch (view.getMode()) {
                case ChronicleMode.Recent:
                    bottom = drawRecent(width, top, maxWidth, chronicle, simulation.getPlayer(), simulation.getTickCounter().getTotalTicks(),
                        focusFor(simulation), view.getScope());
                    break;
                case ChronicleMode.Full:
                    drawFull(width, height, chronicle, simulation.getPlayer(), view, focusFor(simulation));
                    break;
            }
            if (view.getMode() != ChronicleMode.Full) {
                lastSeenTotal = -1;
            }
            return bottom;
        }

        private float drawRecent(float width, float top, float maxWidth, Chronicle chronicle, Player player, int nowTick, ChronicleFocus focus, ChronicleScope scope) {
            float panelWidth = Math.Min(compactWidth(width), maxWidth);
            if (panelWidth <= 0f) {
                return top;
            }
            float bottom = top;
            ImGui.SetNextWindowPos(new Vector2(width / 2f, top), ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowSize(new Vector2(panelWidth, 0f), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.6f);
            if (ImGui.Begin("##chronicle-recent", CompactFlags)) {
                float lineWidth = ImGui.GetContentRegionAvail().X;
                Overlay.textColoured(HintColour, fitToWidth(ObjectiveKeyNames.resolve(recentHeading(scope)), lineWidth, s => ImGui.CalcTextSize(s).X));
                List<ChronicleEntry> recent = ChroniclePanel.recent(chronicle.getEntries(), focus, scope, RecentCount);
                if (recent.Count == 0) {
                    Overlay.textColoured(HintColour, fitToWidth(emptyMessage(scope, false), lineWidth, s => ImGui.CalcTextSize(s).X));
                }
                for (int i = recent.Count - 1; i >= 0; i--) {
                    // Newest first, so the strip reads top-down from now.
                    ChronicleEntry entry = recent[i];
                    float alpha = fadeAlpha(nowTick - entry.getTick());
                    bool mine = focus != null ? focus.isPlayers(entry) : entry.concerns(player);
                    string line = fitToWidth(focus != null ? displayLine(entry, focus) : displayLine(entry, player), lineWidth, s => ImGui.CalcTextSize(s).X);
                    drawStampedLine(line, mine ? MineMarker.Length + entry.getTimestamp().Length : entry.getTimestamp().Length, mine, alpha);
                }
                bottom = ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y;
            }
            ImGui.End();
            return bottom;
        }

        private void drawFull(float width, float height, Chronicle chronicle, Player player, ChronicleView view, ChronicleFocus focus) {
            Vector2 size = fullSize(width, height);
            ImGui.SetNextWindowPos(new Vector2(width / 2f, height / 2f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(size, ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.88f);
            if (ImGui.Begin("Chronicle##chronicle-full", FullFlags)) {
                drawScopeSwitch(view);
                bool onlyMine = view.isOnlyMyNation();
                if (ImGui.Checkbox("Only my nation", ref onlyMine)) {
                    view.setOnlyMyNation(onlyMine);
                }
                ImGui.SameLine();
                if (ImGui.Button("Close")) {
                    view.setMode(ChronicleMode.Hidden);
                }
                ImGui.SameLine();
                Overlay.textColoured(HintColour, ObjectiveKeyNames.resolve(CycleHint));

                int kept = chronicle.getCount();
                int total = chronicle.getTotalRecorded();
                List<ChronicleEntry> entries = select(chronicle.getEntries(), player, focus, view.getScope(), onlyMine);
                Overlay.textColoured(HintColour, countLine(total, kept, entries.Count));
                ImGui.Separator();

                if (ImGui.BeginChild("##chronicle-scroll", Vector2.Zero)) {
                    if (entries.Count == 0) {
                        Overlay.textColoured(HintColour, emptyMessage(view.getScope(), onlyMine));
                    }
                    bool atBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f;
                    ImGui.PushTextWrapPos(0f);
                    foreach (ChronicleEntry entry in entries) {
                        bool mine = focus != null ? focus.isPlayers(entry) : entry.concerns(player);
                        Overlay.textColoured(StampColour, (mine ? MineMarker : "") + entry.getTimestamp());
                        ImGui.SameLine();
                        Overlay.textColoured(mine ? MineColour : TextColour, entry.getText());
                    }
                    ImGui.PopTextWrapPos();
                    // Follow the newest entry on opening and as entries
                    // arrive, unless the player has scrolled up to read; and
                    // on switching scope, which is a different list.
                    if (lastSeenTotal < 0 || (total != lastSeenTotal && atBottom) || view.getScope() != lastSeenScope) {
                        ImGui.SetScrollHereY(1f);
                    }
                    lastSeenTotal = total;
                    lastSeenScope = view.getScope();
                }
                ImGui.EndChild();
            }
            ImGui.End();
        }

        /**
        * "Showing: [> Your news] [All nations]", the chosen one filled and marked. Real
        * buttons rather than a hover or a checkbox, so a tap, a click and the
        * scope key all reach it, and a fingertip tall on a touch screen
        * (TouchLayout.MinTarget). The key is named only on a keyboard.
        */
        private static void drawScopeSwitch(ChronicleView view) {
            float buttonHeight = TouchLayout.isActive() ? Math.Max(UiScale.px(TouchLayout.MinTarget), Overlay.frameHeight()) : 0f;
            ImGui.AlignTextToFramePadding();
            Overlay.textColoured(HintColour, ShowingLabel);
            if (scopeButton(YourNewsLabel, "###chronicle-scope-yours", view.getScope() == ChronicleScope.YourNews, buttonHeight)) {
                view.setScope(ChronicleScope.YourNews);
            }
            if (scopeButton(AllNationsLabel, "###chronicle-scope-all", view.getScope() == ChronicleScope.AllNations, buttonHeight)) {
                view.setScope(ChronicleScope.AllNations);
            }
            if (!TouchLayout.isActive()) {
                ImGui.SameLine();
                Overlay.textColoured(HintColour, "(" + KeyNames.describe(KeyBindings.ToggleChronicleScope) + ")");
            }
        }

        /** The switch's label: the chosen one marked "> ", so the choice does not rest on colour alone. */
        public static string scopeButtonLabel(string label, bool chosen) {
            return (chosen ? "> " : "") + label;
        }

        private static bool scopeButton(string label, string id, bool chosen, float height) {
            ImGui.SameLine();
            Vector4 fill = chosen ? ChosenColour : UnchosenColour;
            ImGui.PushStyleColor(ImGuiCol.Button, fill);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, chosen ? ChosenColour : UnchosenHoverColour);
            bool pressed = ImGui.Button(scopeButtonLabel(label, chosen) + id, new Vector2(0f, height));
            ImGui.PopStyleColor(2);
            return pressed;
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
