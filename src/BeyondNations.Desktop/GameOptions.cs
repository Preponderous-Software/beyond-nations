using System;
using System.Globalization;

namespace beyondnations.desktop {

    /**
    * How the host should run. Everything here has a working default, so
    * `dotnet run --project src/BeyondNations.Desktop` needs no arguments.
    */
    public class GameOptions {
        public int Width = 1280;
        public int Height = 720;
        public bool VSync = true;

        /**
        * Simulation rate, independent of frame rate. Unity's default fixed step
        * was 0.02 seconds, which is 50 a second; that cadence is preserved so
        * tick-driven behaviour keeps its current timing.
        */
        public int TicksPerSecond = 50;

        /**
        * Upper bound on how many fixed steps one frame may run to catch up after
        * a stall, so a long pause cannot spiral.
        */
        public int MaxCatchUpSteps = 5;

        /**
        * Run this many frames and exit. Zero means run until closed. This is what
        * makes the host checkable without a person watching a window.
        */
        public int ExitAfterFrames = 0;

        /**
        * World seed. Zero means pick one, matching GameConfig. --seed takes a
        * number or any text, which WorldSeed hashes to one (docs/SHARING.md).
        */
        public int Seed = 0;

        /**
        * The seed as it was given, "Ironvale" or "42", which the New World
        * screen opens with. Null when no --seed was passed.
        */
        public string SeedText = null;

        /**
        * Resize the window part-way through the run. This exists so that resize
        * handling can be exercised without a person dragging a window corner.
        */
        public bool SmokeResize = false;

        /**
        * Take a screenshot once this many frames have rendered, then continue
        * running. Zero disables it. This is what makes the screenshot path
        * (glReadPixels -> PNG) checkable headlessly, without a person pressing
        * the screenshot key against a visible window. See #224.
        */
        public int ScreenshotAfterFrames = 0;

        /**
        * Record draw calls, frame times and render-loop allocation, and print
        * them on shutdown. The instanced renderer makes claims about scale and
        * about allocating nothing per object; this is how a run proves them.
        */
        public bool RenderStats = false;

        /**
        * Start at this render distance instead of the configured one, stepped
        * through the same increase and decrease Page Up and Page Down use and
        * so subject to the same bounds. Zero keeps the configured value. This
        * is what lets a run at fifty and a run at a thousand be compared
        * without anybody holding a key down. See #219.
        */
        public int RenderDistance = 0;

        /**
        * Submit everything, culling nothing. The counterfactual for the
        * culling claim: the same world drawn with the culler off says how much
        * work it was actually saving.
        */
        public bool NoCulling = false;

        /**
        * Skip world-space nametags. Exists so that a run with labels can be
        * compared against one without, which is how the label rendering is
        * shown to put pixels on screen at all. See #222.
        */
        public bool NoLabels = false;

        /**
        * Which screen the game opens on. The Unity build always opened on the
        * title screen; naming another is how each screen is rendered and
        * checked without a person clicking through to it. See #221.
        */
        public ScreenType StartScreen = ScreenType.TITLE;

        /**
        * Start with the debug overlay already on, which F1 otherwise toggles.
        */
        public bool DebugMode = false;

        /**
        * Open in first person rather than the trailing third-person view the
        * Unity build had. V toggles the same camera mid-run; this exists so a
        * screenshot of the first-person framing can be taken without a person
        * pressing it. See #173.
        */
        public bool FirstPerson = false;

        /**
        * Send nothing to trace this run, whatever settings.json says. The
        * one-run counterpart of "usageReporting": { "enabled": false }; see
        * UsageReporting and the README's "Usage reporting" section.
        */
        public bool NoUsageReporting = false;

        // --- #280 ---
        /**
        * The time of day a new world starts at, in [0, 1): 0.25 sunrise, 0.5
        * noon, 0.75 sunset. Negative keeps TimeOfDay's default.
        */
        public float TimeOfDay = -1f;
        // --- end #280 ---

        /**
        * Open on the first day of this season rather than of spring, by
        * moving the clock on before the world is generated
        * (SeasonCalendar.advanceClockTo). Null starts in spring.
        */
        public Season? StartSeason = null;

        /**
        * Open with a lived-in settlement already standing ahead of the player:
        * a nation, a full market, merchants and residents. Exists so that what
        * a town looks like can be screenshotted without an hour of play; see
        * SettlementShowcase.
        */
        public bool StageSettlement = false;

        /**
        * Replays the feedback effects every couple of seconds -- chips,
        * floating "+5 wood", buildings rising, the objective flash -- without
        * touching the world, so they can be screenshotted headless. See
        * FeelDirector.stageDemo.
        */
        public bool StageFeel = false;

        /**
        * How much larger than its design size to draw the UI. 0, the default,
        * uses the monitor's content scale (150 % in Windows display settings
        * is 1.5); anything from 1 to 4 overrides it.
        */
        public float UiScale = 0f;

        /**
        * Opens the game with the player leading a nation that has two
        * neighbours, one friendly and one hostile and besieging it, and the
        * player standing at the border. See DiplomacyShowcase.
        */
        public bool StageDiplomacy = false;
        // open with a world event's choice sheet up (worldevents/)
        public bool StageEvent = false;

        /**
        * As StageDiplomacy, with the player on the point of starving, so the
        * first tick passes the rule to an heir and opens the succession
        * screen (realm/Succession).
        */
        public bool StageSuccession = false;

        /**
        * Opens the game with the player leading a nation whose village can
        * send caravans, a trading neighbour, and one of its caravans on the
        * road just ahead of the player. See TradeShowcase.
        */
        public bool StageTrade = false;

        /**
        * Neither save nor offer to load this run: no autosave, no save on
        * quit, no Continue or Load. For automated runs that should leave a
        * player's saves alone. See docs/SAVES.md.
        */
        public bool NoSaves = false;

        /**
        * Open straight into the most recently saved world, as Continue on the
        * main menu does. Falls back to the main menu, saying why, when there
        * is no save to continue.
        */
        public bool Continue = false;

        public static GameOptions parse(string[] args) {
            GameOptions options = new GameOptions();
            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                switch (arg) {
                    case "--width":            options.Width = intAfter(args, ref i, options.Width); break;
                    case "--height":           options.Height = intAfter(args, ref i, options.Height); break;
                    case "--ticks-per-second": options.TicksPerSecond = intAfter(args, ref i, options.TicksPerSecond); break;
                    case "--exit-after-frames":options.ExitAfterFrames = intAfter(args, ref i, options.ExitAfterFrames); break;
                    case "--screenshot-after-frames": options.ScreenshotAfterFrames = intAfter(args, ref i, options.ScreenshotAfterFrames); break;
                    case "--seed":
                        options.SeedText = textAfter(args, ref i);
                        options.Seed = WorldSeed.fromText(options.SeedText);
                        break;
                    case "--no-vsync":         options.VSync = false; break;
                    case "--smoke-resize":     options.SmokeResize = true; break;
                    case "--render-stats":     options.RenderStats = true; break;
                    case "--render-distance":  options.RenderDistance = intAfter(args, ref i, options.RenderDistance); break;
                    case "--no-culling":       options.NoCulling = true; break;
                    case "--no-labels":        options.NoLabels = true; break;
                    case "--debug-mode":       options.DebugMode = true; break;
                    case "--stage-settlement": options.StageSettlement = true; break;
                    case "--stage-diplomacy":  options.StageDiplomacy = true; break;
                    case "--stage-feel":       options.StageFeel = true; break;
                    case "--stage-succession": options.StageDiplomacy = true; options.StageSuccession = true; break;
                    case "--stage-event":      options.StageEvent = true; break;
                    case "--stage-trade":      options.StageTrade = true; break;
                    case "--first-person":     options.FirstPerson = true; break;
                    case "--no-usage-reporting": options.NoUsageReporting = true; break;
                    case "--no-saves":         options.NoSaves = true; break;
                    case "--continue":         options.Continue = true; break;
                    case "--start-screen":     options.StartScreen = screenAfter(args, ref i); break;
                    case "--time-of-day":      options.TimeOfDay = timeOfDayAfter(args, ref i); break;
                    case "--season":           options.StartSeason = seasonAfter(args, ref i); break;
                    case "--ui-scale":         options.UiScale = uiScaleAfter(args, ref i); break;
                    case "--help":
                    case "-h":
                        printUsage();
                        System.Environment.Exit(0);
                        break;
                    default:
                        Console.Error.WriteLine("unknown argument: " + arg);
                        printUsage();
                        System.Environment.Exit(2);
                        break;
                }
            }
            return options;
        }

        private static Season seasonAfter(string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                Console.Error.WriteLine("missing value after " + args[i]);
                System.Environment.Exit(2);
            }
            i++;
            switch (args[i].ToLowerInvariant()) {
                case "spring": return Season.SPRING;
                case "summer": return Season.SUMMER;
                case "autumn": return Season.AUTUMN;
                case "winter": return Season.WINTER;
            }
            Console.Error.WriteLine("unknown season: " + args[i] + " (expected spring, summer, autumn or winter)");
            System.Environment.Exit(2);
            return Season.SPRING;
        }

        private static ScreenType screenAfter(string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                Console.Error.WriteLine("missing value after " + args[i]);
                System.Environment.Exit(2);
            }
            i++;
            switch (args[i].ToLowerInvariant()) {
                case "title":     return ScreenType.TITLE;
                case "main-menu": return ScreenType.MAIN_MENU;
                case "config":    return ScreenType.CONFIG;
                case "world":     return ScreenType.WORLD;
                case "pause":     return ScreenType.PAUSE;
                case "inventory": return ScreenType.INVENTORY;
                case "controls":  return ScreenType.CONTROLS;
                case "nations":   return ScreenType.NATIONS;
                case "crafting":  return ScreenType.CRAFTING;
                case "load":      return ScreenType.LOAD;
                case "laws":
                    // the nations screen, opened on its Laws tab
                    beyondnations.desktop.ui.screens.NationScreen.startOnLaws = true;
                    return ScreenType.NATIONS;
                case "new-world": return ScreenType.NEW_WORLD;
                case "share":     return ScreenType.SHARE;
            }
            Console.Error.WriteLine("unknown screen: " + args[i] + " (expected title, main-menu, config, world, pause, inventory, controls, nations, laws, crafting, load, new-world or share)");
            System.Environment.Exit(2);
            return ScreenType.TITLE;
        }

        // --- #280 ---
        private static float timeOfDayAfter(string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                Console.Error.WriteLine("missing value after " + args[i]);
                System.Environment.Exit(2);
            }
            i++;
            float parsed;
            if (!float.TryParse(args[i], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                || float.IsNaN(parsed) || parsed < 0f || parsed >= 1f) {
                Console.Error.WriteLine("time of day must be a number in [0, 1): " + args[i]);
                System.Environment.Exit(2);
            }
            return parsed;
        }
        // --- end #280 ---

        private static float uiScaleAfter(string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                Console.Error.WriteLine("missing value after " + args[i]);
                System.Environment.Exit(2);
            }
            i++;
            float parsed;
            if (!float.TryParse(args[i], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                    || float.IsNaN(parsed) || parsed < 1f || parsed > 4f) {
                Console.Error.WriteLine("ui scale must be a number from 1 to 4: " + args[i]);
                System.Environment.Exit(2);
            }
            return parsed;
        }

        private static string textAfter(string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                Console.Error.WriteLine("missing value after " + args[i]);
                System.Environment.Exit(2);
            }
            i++;
            return args[i];
        }

        private static int intAfter(string[] args, ref int i, int fallback) {
            if (i + 1 >= args.Length) {
                Console.Error.WriteLine("missing value after " + args[i]);
                System.Environment.Exit(2);
            }
            i++;
            int parsed;
            if (!int.TryParse(args[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)) {
                Console.Error.WriteLine("not a number: " + args[i]);
                System.Environment.Exit(2);
            }
            return parsed;
        }

        /**
        * The text --help prints, returned rather than written so that it can be
        * read back and checked against the switch block above. It used to be a
        * run of Console.WriteLine calls with no way to assert on it, and four
        * options had gone missing from it unnoticed (#249).
        */
        public static string getUsageText() {
            return string.Join(System.Environment.NewLine, new string[] {
                "Beyond Nations",
                "",
                "  --width N               window width          (default 1280)",
                "  --height N              window height         (default 720)",
                "  --ticks-per-second N    simulation rate       (default 50)",
                "  --exit-after-frames N   render N frames then exit; 0 runs until closed",
                "  --screenshot-after-frames N  take a screenshot once N frames have rendered; 0 disables",
                "  --seed S                world seed: a number, or any text (hashed to one); 0 picks one",
                "  --no-vsync              do not wait for vertical sync",
                "  --smoke-resize          resize part-way through, to exercise resize handling",
                "  --render-stats          report draw calls, frame times and allocation on exit",
                "  --render-distance N     start at this render distance; 0 keeps the configured one",
                "  --no-culling            submit every primitive, culling nothing",
                "  --no-labels             draw no world-space nametags",
                "  --first-person          open in first person; V toggles the view mid-run",
                "  --no-usage-reporting    send nothing to trace this run (see README, Usage reporting)",
                "  --no-saves              do not autosave, save on quit or offer saved worlds this run",
                "  --continue              open in the most recently saved world (Continue on the main menu)",
                "  --debug-mode            open with the F1 debug overlay already on",
                "  --stage-settlement      open with a lived-in settlement standing ahead of the player",
                "  --stage-diplomacy       open leading a nation between a friendly and a hostile neighbour",
                "  --stage-feel            replay the feedback effects (chips, +5 wood, rising buildings) for screenshots",
                "  --stage-event           open with a world event waiting on a choice (with --stage-diplomacy, one for your nation)",
                "  --stage-succession      as --stage-diplomacy, with the player about to die and an heir to take over",
                "  --start-screen NAME     open on title, main-menu, config, world, pause, inventory, controls, nations, laws, crafting, load, new-world or share",
                "  --stage-trade           open beside a trade caravan, leading a nation that can send its own",
                "  --time-of-day F         start the world at this time of day, 0..1 (0.25 sunrise, 0.5 noon; default 0.3)",
                "  --season NAME           start on the first day of spring, summer, autumn or winter (default spring)",
                "  --ui-scale F            draw the UI F times its design size, 1..4 (default: the monitor's scale)",
                "  --help, -h              print this and exit"
            });
        }

        private static void printUsage() {
            Console.WriteLine(getUsageText());
        }
    }
}
