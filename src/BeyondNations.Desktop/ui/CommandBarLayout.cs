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
    * - The pawn buttons (CommandGroup.People) start a row of their own, so
    *   a button before them going away -- a caravan leaving -- cannot slide
    *   Talk under the cursor, and the tool shortcuts (Craft Axe) come last
    *   of all, after them, so their going moves nothing. Each pawn button is
    *   measured with a
    *   stand-in name (WorldCommand.getWidthLabel), so one pawn's long name
    *   or warming tier moves nothing; a longer label is shortened with
    *   "..." (fitLabel). Playtest 0.6.2 s02, finding 17.
    * - Inside a settlement, where residents walk into and out of reach all
    *   the time, the pawn rows are kept whether or not a pawn is there
    *   (Settings.ReservedPeopleRows), and the tool shortcuts and Eat start a
    *   row of their own above them: a pawn row coming and going moved Eat
    *   under a click meant for the market, and the boxes and the caravan
    *   panel with the bar's top (0.8.0 re-run s05, #486).
    * - Nothing goes left of Left or right of Right: the caller keeps Right
    *   short of the right-hand column of boxes, and a label too long for the
    *   bar is clipped to it rather than running off the window.
    * - A bar given at most MaxRows rows (a phone, whose HUD leaves the bar
    *   two or three rows above the touch controls) that needs more is shown
    *   a page at a time behind a More button, which always sits first, at
    *   the bottom left (#466: in a market town Trade at Market ran under
    *   the HUD boxes). The pages keep the bar's order, the drastic buttons
    *   start a page of their own, so they are still apart from the rest,
    *   the pawn buttons come last, on pages of their own, so a resident
    *   coming or going moves no other button (#486, in place of the kept
    *   rows), and every page takes the same rows, so turning one moves no box.
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
            /**
            * Rows kept for the pawn buttons whether or not a pawn is in reach
            * (0: none kept). With rows kept, the groups after the pawns
            * (Craft: Eat, Craft Axe) start a row of their own above them, and
            * the bar's top counts them, so a resident walking into or out of
            * reach moves no button and no box (#486).
            */
            public int ReservedPeopleRows;
            /**
            * The most rows the bar may take, the drastic row included (0:
            * as many as it needs). A bar that needs more is paged behind a
            * More button.
            */
            public int MaxRows;
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
            /** One per command, in the order given; empty for one not on the page shown. */
            public Rect[] Buttons;
            /** Whether each command is on the page shown (every one, when the bar is not paged). */
            public bool[] Shown;
            /** The top of the highest row, or Bottom when there are no buttons. */
            public float Top;
            /** How many pages the bar has: 1 when everything fits. */
            public int PageCount = 1;
            /** The page shown, 0 to PageCount - 1. */
            public int Page;
            /** Where the More button goes; only when PageCount > 1. */
            public Rect More;

            public bool isPaged() {
                return PageCount > 1;
            }
        }

        public const string MoreLabel = "More";

        /** "More 1/3": which page is showing, and that tapping shows the next. */
        public static string getMoreLabel(int page, int pageCount) {
            return MoreLabel + " " + (page + 1) + "/" + pageCount;
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
            float text = measure(padCounts(command.getWidthLabel()));
            if (command.needsConfirm()) {
                text = System.MathF.Max(text, measure(padCounts(ConfirmPrefix + command.getWidthLabel())));
            }
            float width = System.MathF.Max(settings.MinButtonWidth, text + settings.TextPadding);
            if (settings.WidthStep > 0f) {
                width = System.MathF.Ceiling(width / settings.WidthStep - 0.001f) * settings.WidthStep;
            }
            return System.MathF.Max(1f, System.MathF.Min(width, settings.Right - settings.Left));
        }

        public const string Ellipsis = "...";

        /**
        * The label shortened with "..." until it fits textWidth, or the label
        * itself when it already fits. A pawn button is as wide as its
        * stand-in name allows (WorldCommand.getWidthLabel), and a longer
        * name is cut rather than widening the button.
        */
        public static string fitLabel(string label, System.Func<string, float> measure, float textWidth) {
            if (measure(label) <= textWidth) {
                return label;
            }
            for (int length = label.Length - 1; length > 0; length--) {
                string cut = label.Substring(0, length).TrimEnd() + Ellipsis;
                if (measure(cut) <= textWidth) {
                    return cut;
                }
            }
            return Ellipsis;
        }

        /**
        * Flows the drastic commands, or the others, left to right in rows
        * stacking upwards from bottom. Returns the top of the highest row,
        * or bottom when there were none.
        */
        private static float flow(List<WorldCommand> commands, bool drastic, float bottom, System.Func<string, float> measure,
                Settings settings, Rect[] buttons) {
            float rowY = bottom - settings.ButtonHeight;
            float step = settings.ButtonHeight + settings.SpacingY;
            float x = settings.Left;
            bool rowEmpty = true;
            bool any = false;
            bool inPeople = false;
            // the kept pawn rows (ReservedPeopleRows), placed once the groups before them are
            int reserve = drastic ? 0 : System.Math.Max(0, settings.ReservedPeopleRows);
            bool reserved = false;
            bool pastPeople = false;
            float peopleTop = float.MaxValue;
            float top = bottom;
            for (int i = 0; i < commands.Count; i++) {
                if ((commands[i].getGroup() == CommandGroup.Drastic) != drastic) {
                    continue;
                }
                float width = buttonWidth(commands[i], measure, settings);
                CommandGroup group = commands[i].getGroup();
                if (reserve > 0 && !reserved && group >= CommandGroup.People) {
                    // where the pawn rows start: here, on a row of their own
                    float peopleRow = rowEmpty ? rowY : rowY - step;
                    peopleTop = peopleRow - (reserve - 1) * step;
                    top = System.MathF.Min(top, peopleTop);
                    reserved = true;
                }
                if (reserve > 0 && !pastPeople && group > CommandGroup.People) {
                    // after the pawns: a row of its own above the kept rows (or
                    // above the pawns' own buttons, should they ever need more)
                    rowY = System.MathF.Min(peopleTop - step, rowEmpty ? rowY : rowY - step);
                    x = settings.Left;
                    rowEmpty = true;
                    pastPeople = true;
                }
                // the pawn buttons start a row of their own
                bool firstPerson = !inPeople && group == CommandGroup.People;
                inPeople = inPeople || firstPerson;
                if (!rowEmpty && (firstPerson || x + settings.SpacingX + width > settings.Right)) {
                    rowY -= step;
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
            if (reserve > 0 && !reserved) {
                // no pawn and nothing after them: the kept rows still count toward the top
                float peopleRow = rowEmpty ? rowY : rowY - step;
                top = System.MathF.Min(top, peopleRow - (reserve - 1) * step);
                reserved = true;
            }
            float highest = any ? rowY : bottom;
            return reserved ? System.MathF.Min(highest, top) : highest;
        }

        /**
        * How many rows these commands take on a bar of these settings when
        * they start a row of their own: what ReservedPeopleRows is worked out
        * from, with the pawn buttons a leader could see (WorldCommandBar.getPawnStandIns).
        */
        public static int countRows(List<WorldCommand> commands, System.Func<string, float> measure, Settings settings) {
            int rows = 0;
            float x = settings.Left;
            bool rowEmpty = true;
            foreach (WorldCommand command in commands) {
                float width = buttonWidth(command, measure, settings);
                if (rowEmpty || x + settings.SpacingX + width > settings.Right) {
                    rows++;
                    x = settings.Left + width;
                    rowEmpty = false;
                    continue;
                }
                x += settings.SpacingX + width;
            }
            return rows;
        }

        /**
        * Lays out commands, which should already be in order(). page is the
        * page to show when the bar has to be paged (Settings.MaxRows); it is
        * wrapped round, so a caller can keep counting up.
        */
        public static Result compute(List<WorldCommand> commands, System.Func<string, float> measure, Settings settings, int page = 0) {
            Result result = new Result();
            result.Buttons = new Rect[commands.Count];
            result.Shown = new bool[commands.Count];
            result.Top = settings.Bottom;
            if (commands.Count == 0) {
                return result;
            }
            // the drastic row along the bottom, then the rest above it, apart
            float top = flow(commands, true, settings.Bottom, measure, settings, result.Buttons);
            float restBottom = top < settings.Bottom ? top - settings.SpacingY - settings.DrasticGap : settings.Bottom;
            float restTop = flow(commands, false, restBottom, measure, settings, result.Buttons);
            result.Top = restTop < restBottom ? restTop : top;
            // the kept pawn rows count, empty or not (ReservedPeopleRows)
            int rows = rowsSpanned(top, settings.Bottom, settings) + rowsSpanned(restTop, restBottom, settings);
            if (settings.MaxRows > 0 && rows > settings.MaxRows) {
                return paged(commands, measure, settings, page);
            }
            for (int i = 0; i < commands.Count; i++) {
                result.Shown[i] = true;
            }
            return result;
        }

        /**
        * The first page of a paged bar (as compute's page 0) for commands in
        * any order: a phone's bar in its front-first order
        * (WorldCommandBar.frontFirst), whose other pages are the More sheet
        * (CommandSheet, #512). The order is the caller's, apart from the
        * drastic and pawn buttons, which still never share the first page
        * with the rest.
        *
        * Each button goes on the lowest row it fits on, not only the row
        * the last one went on: in a camp, Feed Residents is too wide to go
        * beside More and took the row above, and More was left alone on
        * the bottom row with room for Settlement and Exit Settlement.
        * Buttons are placed in order, so one coming or going later in the
        * list never moves one before it; one that fits nowhere is left for
        * the sheet. More and the bar's top are where paging puts them.
        */
        public static Result firstPage(List<WorldCommand> commands, System.Func<string, float> measure, Settings settings) {
            Result result = paged(commands, measure, settings, 0);
            int rows = System.Math.Max(1, settings.MaxRows);
            float rowStep = settings.ButtonHeight + settings.SpacingY;
            // where each row has got to; the bottom one starts after More
            float[] rowRight = new float[rows];
            for (int r = 0; r < rows; r++) {
                rowRight[r] = settings.Left;
            }
            rowRight[0] = result.More.Right;
            bool anyPlaced = false;
            for (int i = 0; i < commands.Count; i++) {
                result.Shown[i] = false;
                result.Buttons[i] = new Rect();
                CommandGroup group = commands[i].getGroup();
                if (group == CommandGroup.Drastic || group == CommandGroup.People) {
                    continue;
                }
                float width = buttonWidth(commands[i], measure, settings);
                for (int r = 0; r < rows; r++) {
                    float x = rowRight[r] > settings.Left ? rowRight[r] + settings.SpacingX : rowRight[r];
                    float fitted = width;
                    // a button too wide to go beside More on a bar of one row is narrowed to fit, as paging does
                    if (rows == 1 && !anyPlaced) {
                        fitted = System.MathF.Max(1f, System.MathF.Min(width, settings.Right - x));
                    }
                    if (x + fitted > settings.Right + 0.01f) {
                        continue;
                    }
                    result.Buttons[i] = new Rect { X = x, Y = settings.Bottom - settings.ButtonHeight - r * rowStep, Width = fitted, Height = settings.ButtonHeight };
                    result.Shown[i] = true;
                    rowRight[r] = x + fitted;
                    anyPlaced = true;
                    break;
                }
            }
            return result;
        }

        /** How many rows fit between top and bottom (0 when top is not above bottom). */
        private static int rowsSpanned(float top, float bottom, Settings settings) {
            if (top >= bottom) {
                return 0;
            }
            return (int) System.MathF.Round((bottom - top + settings.SpacingY) / (settings.ButtonHeight + settings.SpacingY));
        }

        /**
        * The bar a page at a time: More first, at the bottom left, then the
        * commands in order, rows stacking upwards, a new page whenever the
        * next would need a row past MaxRows. The drastic commands start a
        * page of their own, and the pawn buttons come last of all, on pages
        * of their own.
        *
        * ReservedPeopleRows does not apply here. It keeps rows empty so a
        * pawn walking into reach moves nothing (#486); a paged bar keeps
        * still another way: every page takes the same MaxRows rows, and with
        * the pawns' pages last, a pawn coming or going adds or removes pages
        * after every other button, so Eat, the tools and the market stay on
        * the same page in the same place. Kept rows here would only be an
        * empty page, or fewer buttons a page, when no one is in reach. (They
        * do count toward MaxRows when compute decides whether to page.) The
        * pawns' pages also come after the drastic page, so a pawn walking
        * away while their page shows wraps the bar round to the first page,
        * never onto Declare War.
        */
        private static Result paged(List<WorldCommand> commands, System.Func<string, float> measure, Settings settings, int page) {
            int count = commands.Count;
            // the flowing commands first, then the drastic ones, then the pawns'
            List<int> order = new List<int>(count);
            foreach (bool drasticPass in new[] { false, true }) {
                for (int i = 0; i < count; i++) {
                    CommandGroup group = commands[i].getGroup();
                    if ((group == CommandGroup.Drastic) == drasticPass && group != CommandGroup.People) {
                        order.Add(i);
                    }
                }
            }
            for (int i = 0; i < count; i++) {
                if (commands[i].getGroup() == CommandGroup.People) {
                    order.Add(i);
                }
            }
            float barWidth = settings.Right - settings.Left;
            // as wide as "More 9/9" from the start, so the count changing moves nothing
            float moreWidth = System.MathF.Max(settings.MinButtonWidth, measure(getMoreLabel(8, 9)) + settings.TextPadding);
            if (settings.WidthStep > 0f) {
                moreWidth = System.MathF.Ceiling(moreWidth / settings.WidthStep - 0.001f) * settings.WidthStep;
            }
            moreWidth = System.MathF.Min(moreWidth, barWidth);
            float rowStep = settings.ButtonHeight + settings.SpacingY;
            float firstX = settings.Left + moreWidth;

            int[] pageOf = new int[count];
            Rect[] rects = new Rect[count];
            int pageIndex = 0;
            int row = 0;
            float x = firstX;
            bool pageEmpty = true;
            bool inPeople = false;
            bool inDrastic = false;
            foreach (int i in order) {
                WorldCommand command = commands[i];
                float buttonW = buttonWidth(command, measure, settings);
                bool firstDrastic = !inDrastic && command.getGroup() == CommandGroup.Drastic;
                bool firstPerson = !inPeople && command.getGroup() == CommandGroup.People;
                inDrastic = inDrastic || firstDrastic;
                inPeople = inPeople || firstPerson;
                if ((firstDrastic || firstPerson) && !pageEmpty) {
                    pageIndex++;
                    row = 0;
                    x = firstX;
                    pageEmpty = true;
                }
                bool rowStarted = x > settings.Left;
                if (rowStarted && x + settings.SpacingX + buttonW > settings.Right) {
                    // a new row, or a new page when the rows are used up
                    if (row + 1 < settings.MaxRows) {
                        row++;
                        x = settings.Left;
                    }
                    else if (!pageEmpty) {
                        pageIndex++;
                        row = 0;
                        x = firstX;
                        pageEmpty = true;
                    }
                }
                if (x > settings.Left) {
                    x += settings.SpacingX;
                }
                // a button too wide to go beside More on a page of one row is narrowed to fit
                buttonW = System.MathF.Max(1f, System.MathF.Min(buttonW, settings.Right - x));
                rects[i] = new Rect { X = x, Y = settings.Bottom - settings.ButtonHeight - row * rowStep, Width = buttonW, Height = settings.ButtonHeight };
                pageOf[i] = pageIndex;
                x += buttonW;
                pageEmpty = false;
            }

            Result result = new Result();
            result.Buttons = new Rect[count];
            result.Shown = new bool[count];
            result.PageCount = pageIndex + 1;
            result.Page = ((page % result.PageCount) + result.PageCount) % result.PageCount;
            for (int i = 0; i < count; i++) {
                if (pageOf[i] == result.Page) {
                    result.Buttons[i] = rects[i];
                    result.Shown[i] = true;
                }
            }
            result.More = new Rect { X = settings.Left, Y = settings.Bottom - settings.ButtonHeight, Width = moreWidth, Height = settings.ButtonHeight };
            // every page takes the same rows, so turning one moves nothing above the bar
            result.Top = settings.Bottom - settings.ButtonHeight - (settings.MaxRows - 1) * rowStep;
            return result;
        }

        /**
        * The rows a bar between ceiling and bottom has room for, at least
        * one: Settings.MaxRows for a bar that must stay under the boxes above it.
        */
        public static int rowsBetween(float ceiling, float bottom, float buttonHeight, float spacingY) {
            return System.Math.Max(1, (int) System.MathF.Floor((bottom - ceiling + spacingY) / (buttonHeight + spacingY)));
        }
    }
}
