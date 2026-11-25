using System.Collections.Generic;

namespace beyondnations.desktop.ui {

    /**
    * The More sheet of a phone's command bar (#512).
    *
    * A phone has room for two or three rows of buttons under the HUD, and in
    * a market town the bar used to page through six pages (ten, before the
    * HUD left it more rows) behind a "More 1/6" button, with Trade at
    * Market on the second and a pawn's Talk on the fifth. Now the bar keeps
    * one page, the buttons used most where the player stands first
    * (WorldCommandBar.frontFirst), and More opens this sheet over the HUD
    * with every other button, under headings by group (CommandGroup), in
    * the bar's group order: the drastic ones last and apart, still asking
    * twice. Close sits where More was, so the same spot opens and shuts it.
    * Every button the desktop's bar has is on the phone's bar or in here.
    *
    * The deciding and the placing are plain numbers, for the tests; the
    * drawing is WorldHud's. A desktop bar is never paged, so never has this.
    */
    public static class CommandSheet {

        /** The bar's button that opens the sheet. */
        public const string MoreLabel = "More";

        /** The same button while the sheet is open. */
        public const string CloseLabel = "Close";

        /** What the sheet says above its buttons until one is tapped. */
        public const string Intro = "Everything else you can do here:";

        public class Section {
            public CommandGroup Group;
            public string Title;
            /** Indexes into the bar's command list, in its order. */
            public List<int> Commands = new List<int>();
        }

        public class Placement {
            /** Where each section's heading goes, as its top, from the top of the scrolling list. */
            public float[] HeadingTops;
            /** One per command in the bar's list; empty for one on the bar. */
            public CommandBarLayout.Rect[] Buttons;
            /** How tall the list is, for scrolling. */
            public float Height;
        }

        private static bool open;
        private static string note;

        public static bool isOpen() {
            return open;
        }

        public static void setOpen(bool isOpen) {
            open = isOpen;
            note = null;
        }

        public static void close() {
            setOpen(false);
        }

        /** The line over the buttons: why the last tapped one cannot be used, or that it needs a second tap. */
        public static string getNote() {
            return note ?? Intro;
        }

        public static void setNote(string text) {
            note = text;
        }

        /** The heading over a group's buttons in the sheet. */
        public static string titleOf(CommandGroup group) {
            switch (group) {
                case CommandGroup.Nation: return "Nation";
                case CommandGroup.Place: return "This place";
                case CommandGroup.Build: return "Build";
                case CommandGroup.Stall: return "Your stall";
                case CommandGroup.Trade: return "Trade and other nations";
                case CommandGroup.People: return "The person in front of you";
                case CommandGroup.Craft: return "Tools and food";
                default: return "Cannot be undone (tap twice)";
            }
        }

        /**
        * The sheet's sections: every command not on the bar's page
        * (onBar false), under its group's heading, the groups in the bar's
        * order (CommandGroup) and each group's buttons in the bar's order.
        * Groups with nothing in the sheet have no section.
        */
        public static List<Section> sections(List<WorldCommand> commands, bool[] onBar) {
            List<Section> sections = new List<Section>();
            foreach (CommandGroup group in System.Enum.GetValues(typeof(CommandGroup))) {
                Section section = null;
                for (int i = 0; i < commands.Count; i++) {
                    if (onBar[i] || commands[i].getGroup() != group) {
                        continue;
                    }
                    if (section == null) {
                        section = new Section { Group = group, Title = titleOf(group) };
                        sections.Add(section);
                    }
                    section.Commands.Add(i);
                }
            }
            return sections;
        }

        /**
        * Where the sheet's headings and buttons go in its scrolling list, a
        * section at a time from the top: the heading, then its buttons left
        * to right from settings.Left, wrapping at settings.Right, as wide as
        * on the bar (CommandBarLayout.buttonWidth) and as tall.
        */
        public static Placement layout(List<WorldCommand> commands, List<Section> sections, System.Func<string, float> measure,
                CommandBarLayout.Settings settings, float headingHeight) {
            Placement placement = new Placement();
            placement.HeadingTops = new float[sections.Count];
            placement.Buttons = new CommandBarLayout.Rect[commands.Count];
            float rowStep = settings.ButtonHeight + settings.SpacingY;
            float y = 0f;
            for (int s = 0; s < sections.Count; s++) {
                if (s > 0) {
                    y += settings.SpacingY * 2f;
                }
                placement.HeadingTops[s] = y;
                y += headingHeight + settings.SpacingY;
                float x = settings.Left;
                bool rowEmpty = true;
                foreach (int i in sections[s].Commands) {
                    float width = CommandBarLayout.buttonWidth(commands[i], measure, settings);
                    if (!rowEmpty && x + settings.SpacingX + width > settings.Right) {
                        y += rowStep;
                        x = settings.Left;
                        rowEmpty = true;
                    }
                    if (!rowEmpty) {
                        x += settings.SpacingX;
                    }
                    placement.Buttons[i] = new CommandBarLayout.Rect { X = x, Y = y, Width = width, Height = settings.ButtonHeight };
                    x += width;
                    rowEmpty = false;
                }
                y += rowEmpty ? 0f : settings.ButtonHeight;
            }
            placement.Height = y;
            return placement;
        }
    }
}
