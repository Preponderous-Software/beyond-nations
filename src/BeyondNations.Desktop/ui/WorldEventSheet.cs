using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations;

namespace beyondnations.desktop.ui {

    /**
    * The choice sheet for a world event (worldevents/WorldEvents): a panel
    * rising from the bottom of the screen over the dimmed world, with what
    * has happened and one big button per choice. The host stops advancing
    * the world while it is up, so a choice is never made against the clock.
    *
    * Every choice is a full-width button at least 48 design pixels tall, so
    * it is an easy target for a thumb on a phone; a choice that cannot be
    * taken is drawn greyed out with the reason in place of a button, rather
    * than hidden, so the trade-off is still visible. There is no hover-only
    * information anywhere on it.
    *
    * What it shows is worked out by getView, which needs no window, so the
    * text is checked in a unit test and only the drawing needs a screen.
    * Coloured text goes through Overlay.textColoured (the browser build
    * cannot call ImGui's variadic text functions).
    */
    public class WorldEventSheet {
        public const string Heading = "WORLD EVENT";
        public const string PausedNote = "The world waits while you decide.";
        public const float MinButtonHeight = 48f;

        private const ImGuiWindowFlags SheetFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBackground;

        public class Row {
            public string Label;
            public string Detail;
            public bool Available;
        }

        public class View {
            public string Title;
            public string Body;
            public List<Row> Rows = new List<Row>();
        }

        /** The sheet for the pending event, or null when nothing is waiting on the player. */
        public static View getView(Simulation simulation) {
            WorldEvents events = simulation == null ? null : simulation.getWorldEvents();
            IWorldEvent worldEvent = events == null ? null : events.getPendingEvent();
            if (worldEvent == null) {
                return null;
            }
            View view = new View();
            view.Title = worldEvent.getTitle();
            view.Body = events.describePending(simulation);
            foreach (WorldEventChoice choice in events.getPendingChoices(simulation)) {
                view.Rows.Add(new Row {
                    Label = choice.getLabel(),
                    Detail = choice.isAvailable() ? choice.getConsequence() : choice.getUnavailableReason() + " -- " + choice.getConsequence(),
                    Available = choice.isAvailable(),
                });
            }
            return view;
        }

        /**
        * Draws the sheet if an event is waiting, and takes the choice tapped.
        * Returns true while the sheet is up (the world HUD under it should
        * not be acted on).
        */
        public bool draw(float width, float height, Simulation simulation) {
            View view = getView(simulation);
            if (view == null) {
                return false;
            }
            float scale = System.Math.Clamp(System.MathF.Min(width / 640f, height / 520f), 1f, 1.5f) * UiScale.get();
            float gutter = System.MathF.Max(16f, UiScale.px(16f));
            float panelWidth = System.MathF.Min(width - gutter * 2f, 640f * scale);
            float x0 = (width - panelWidth) * 0.5f;
            float padding = 14f * scale;
            float textWidth = panelWidth - padding * 2f;
            float fontScale = scale / UiScale.get();
            float lineHeight = Overlay.BaseFontSize * fontScale + 4f * scale;

            // Overlay.beginFullScreen's window never comes to the front, and the
            // HUD boxes and command bar would sit on top of the sheet and take
            // its taps; this one is focused every frame, so it is the top-most
            // window and every tap anywhere on screen lands on it.
            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
            ImGui.SetNextWindowFocus();
            ImGui.Begin("##world-event-sheet", SheetFlags);
            Overlay.dimBackground(width, height);

            // measure first, so the panel sits on the bottom edge however much it holds
            ImGui.SetWindowFontScale(fontScale);
            List<string> body = TextWrap.wrap(view.Body, textWidth, s => ImGui.CalcTextSize(s).X);
            float buttonTextWidth = textWidth - padding * 2f;
            List<List<string>> details = new List<List<string>>();
            float buttonsHeight = 0f;
            foreach (Row row in view.Rows) {
                ImGui.SetWindowFontScale(fontScale * 0.9f);
                List<string> detail = TextWrap.wrap(row.Detail, buttonTextWidth, s => ImGui.CalcTextSize(s).X);
                details.Add(detail);
                buttonsHeight += buttonHeight(detail.Count, lineHeight, scale) + 8f * scale;
            }
            float panelHeight = padding + lineHeight * 0.8f + lineHeight * 1.5f + body.Count * lineHeight + 10f * scale
                + buttonsHeight + lineHeight + padding;
            float top = System.MathF.Max(gutter, height - panelHeight - gutter * 0.5f);

            Overlay.fillLocalRect(new Vector2(x0, top), new Vector2(panelWidth, height - top), new Vector4(0.10f, 0.10f, 0.13f, 0.97f));
            Overlay.fillLocalRect(new Vector2(x0, top), new Vector2(panelWidth, 4f * scale), new Vector4(0.95f, 0.75f, 0.30f, 1f));

            float y = top + padding;
            ImGui.SetWindowFontScale(fontScale * 0.8f);
            ImGui.SetCursorPos(new Vector2(x0 + padding, y));
            Overlay.textColoured(new Vector4(0.95f, 0.75f, 0.30f, 1f), Heading);
            y += lineHeight * 0.8f;
            ImGui.SetWindowFontScale(fontScale * 1.4f);
            ImGui.SetCursorPos(new Vector2(x0 + padding, y));
            ImGui.TextUnformatted(view.Title);
            y += lineHeight * 1.5f;
            ImGui.SetWindowFontScale(fontScale);
            foreach (string line in body) {
                ImGui.SetCursorPos(new Vector2(x0 + padding, y));
                Overlay.textColoured(new Vector4(0.9f, 0.9f, 0.88f, 1f), line);
                y += lineHeight;
            }
            y += 10f * scale;

            int clicked = -1;
            for (int i = 0; i < view.Rows.Count; i++) {
                Row row = view.Rows[i];
                List<string> detail = details[i];
                float h = buttonHeight(detail.Count, lineHeight, scale);
                ImGui.SetWindowFontScale(fontScale);
                if (row.Available) {
                    if (Overlay.buttonAt("##world-event-choice-" + i, x0 + padding, y, textWidth, h)) {
                        clicked = i;
                    }
                } else {
                    Overlay.fillLocalRect(new Vector2(x0 + padding, y), new Vector2(textWidth, h), new Vector4(0.18f, 0.18f, 0.20f, 0.9f));
                }
                float ty = y + 8f * scale;
                ImGui.SetCursorPos(new Vector2(x0 + padding * 2f, ty));
                Overlay.textColoured(row.Available ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(0.55f, 0.55f, 0.55f, 1f), (i + 1) + ". " + row.Label);
                ty += lineHeight;
                ImGui.SetWindowFontScale(fontScale * 0.9f);
                foreach (string line in detail) {
                    ImGui.SetCursorPos(new Vector2(x0 + padding * 2f, ty));
                    Overlay.textColoured(row.Available ? new Vector4(0.85f, 0.85f, 0.7f, 1f) : new Vector4(1f, 0.6f, 0.35f, 1f), line);
                    ty += lineHeight * 0.9f;
                }
                y += h + 8f * scale;
            }

            ImGui.SetWindowFontScale(fontScale * 0.8f);
            ImGui.SetCursorPos(new Vector2(x0 + padding, y));
            Overlay.textColoured(new Vector4(0.7f, 0.7f, 0.7f, 1f), PausedNote);
            ImGui.SetWindowFontScale(1f);
            Overlay.end();

            // choose after drawing, so the sheet drawn this frame stays consistent
            if (clicked >= 0) {
                simulation.getWorldEvents().choose(simulation, clicked);
            }
            return true;
        }

        private static float buttonHeight(int detailLines, float lineHeight, float scale) {
            return System.MathF.Max(MinButtonHeight * scale, 16f * scale + lineHeight + detailLines * lineHeight * 0.9f);
        }
    }
}
