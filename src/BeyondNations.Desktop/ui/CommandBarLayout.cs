using System.Collections.Generic;

namespace beyondnations.desktop.ui {

    /**
    * Where each command bar button goes, as plain numbers, so the rules can
    * be checked for any window without ImGui.
    *
    * Playtesters clicked one button and got another, because the bar re-laid
    * itself out under the cursor: a caravan came into range and Raid took
    * the place Enter Settlement had been, a Buy Axe button pushed Exit
    * Settlement into Purchase Food's slot, two Build buttons swapped. The bar
    * was centred, so every new button moved all the others, and it wrapped
    * from the top down, so a new row moved every row below it. Now:
    *
    * - Drastic buttons (raid, denounce, leave the nation) are on a row of
    *   their own along the bottom, apart from the rest by a gap, and every
    *   click on one is confirmed by a second (ConfirmGate). Leave Nation is
    *   there whenever the player is in a nation, so the row is always there
    *   and a raid appearing beside it moves nothing.
    * - The other buttons flow left to right from a fixed left edge above
    *   it, in the bar's group order (CommandGroup), with rows stacking
    *   upwards, so a button added at the end -- the groups that come and go
    *   are last -- never moves the ones before it, and no harmless button
    *   is ever on the drastic row.
    * - Widths are rounded up to a step, counts are measured as three digits,
    *   and a button that is confirmed by a second click is as wide as its
    *   "Confirm ..." label from the start, so a count ticking up or a button
    *   arming does not nudge its neighbours.
    * - Nothing goes left of Left or right of Right: the caller keeps Right
    *   short of the right-hand column of boxes, and a label too long for the
    *   bar is clipped to it rather than running off the window.
    */
    public static class CommandBarLayout {
        public const string ConfirmPrefix = "Confirm ";

        public struct Settings {
            public float Left;
            public float Right;
            public float Bottom;
            public float ButtonHeight;
            public float SpacingX;
            public float SpacingY;
            public float TextPadding;
            public float MinButtonWidth;
            /** Widths are rounded up to a multiple of this (0: not rounded). */
            public float WidthStep;
            /** Extra space between the drastic row and the rows under it. */
            public float DrasticGap;
        }

        public struct Rect {
            public float X;
            public float Y;
            public float Width;
            public float Height;

            public float Right { get { return X + Width; } }
            public float Bottom { get { return Y + Height; } }

            public bool overlaps(Rect other) {
                return X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
            }
        }

        public class Result {
            /** One per command, in the order given. */
            public Rect[] Buttons;
            /** The top of the highest row, or Bottom when there are no buttons. */
            public float Top;
        }

        /** What a button says while it waits for the second click. */
        public static string getArmedLabel(WorldCommand command) {
            return ConfirmPrefix + command.getLabel();
        }

        /**
        * The order the bar shows commands in: by group, and within a group in
        * the order they were offered. Stable, unlike List.Sort.
        */
        public static List<WorldCommand> order(List<WorldCommand> commands) {
            List<WorldCommand> ordered = new List<WorldCommand>(commands.Count);
            foreach (CommandGroup group in System.Enum.GetValues(typeof(CommandGroup))) {
                foreach (WorldCommand command in commands) {
                    if (command.getGroup() == group) {
                        ordered.Add(command);
                    }
                }
            }
            return ordered;
        }

        /** "Collect Coins (7)" measured as "Collect Coins (007)": room for a count to grow. */
        private static string padCounts(string label) {
            return System.Text.RegularExpressions.Regex.Replace(label, "[0-9]+", match => match.Value.PadLeft(3, '0'));
        }

        public static float buttonWidth(WorldCommand command, System.Func<string, float> measure, Settings settings) {
            float text = measure(padCounts(command.getLabel()));
            if (command.needsConfirm()) {
                text = System.MathF.Max(text, measure(padCounts(getArmedLabel(command))));
            }
            float width = System.MathF.Max(settings.MinButtonWidth, text + settings.TextPadding);
            if (settings.WidthStep > 0f) {
                width = System.MathF.Ceiling(width / settings.WidthStep - 0.001f) * settings.WidthStep;
            }
            return System.MathF.Max(1f, System.MathF.Min(width, settings.Right - settings.Left));
        }

        /**
        * Flows the drastic commands, or the others, left to right in rows
        * stacking upwards from bottom. Returns the top of the highest row,
        * or bottom when there were none.
        */
        private static float flow(List<WorldCommand> commands, bool drastic, float bottom, System.Func<string, float> measure,
                Settings settings, Rect[] buttons) {
            float rowY = bottom - settings.ButtonHeight;
            float x = settings.Left;
            bool rowEmpty = true;
            bool any = false;
            for (int i = 0; i < commands.Count; i++) {
                if ((commands[i].getGroup() == CommandGroup.Drastic) != drastic) {
                    continue;
                }
                float width = buttonWidth(commands[i], measure, settings);
                if (!rowEmpty && x + settings.SpacingX + width > settings.Right) {
                    rowY -= settings.ButtonHeight + settings.SpacingY;
                    x = settings.Left;
                    rowEmpty = true;
                }
                if (!rowEmpty) {
                    x += settings.SpacingX;
                }
                buttons[i] = new Rect { X = x, Y = rowY, Width = width, Height = settings.ButtonHeight };
                x += width;
                rowEmpty = false;
                any = true;
            }
            return any ? rowY : bottom;
        }

        /** Lays out commands, which should already be in order(). */
        public static Result compute(List<WorldCommand> commands, System.Func<string, float> measure, Settings settings) {
            Result result = new Result();
            result.Buttons = new Rect[commands.Count];
            result.Top = settings.Bottom;
            if (commands.Count == 0) {
                return result;
            }
            // the drastic row along the bottom, then the rest above it, apart
            float top = flow(commands, true, settings.Bottom, measure, settings, result.Buttons);
            float restBottom = top < settings.Bottom ? top - settings.SpacingY - settings.DrasticGap : settings.Bottom;
            float restTop = flow(commands, false, restBottom, measure, settings, result.Buttons);
            result.Top = restTop < restBottom ? restTop : top;
            return result;
        }
    }
}
