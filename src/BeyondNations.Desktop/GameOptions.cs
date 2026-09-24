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
        * World seed. Zero means pick one, matching GameConfig.
        */
        public int Seed = 0;

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
                    case "--seed":             options.Seed = intAfter(args, ref i, options.Seed); break;
                    case "--no-vsync":         options.VSync = false; break;
                    case "--smoke-resize":     options.SmokeResize = true; break;
                    case "--render-stats":     options.RenderStats = true; break;
                    case "--render-distance":  options.RenderDistance = intAfter(args, ref i, options.RenderDistance); break;
                    case "--no-culling":       options.NoCulling = true; break;
                    case "--no-labels":        options.NoLabels = true; break;
                    case "--debug-mode":       options.DebugMode = true; break;
                    case "--first-person":     options.FirstPerson = true; break;
                    case "--no-usage-reporting": options.NoUsageReporting = true; break;
                    case "--start-screen":     options.StartScreen = screenAfter(args, ref i); break;
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
            }
            Console.Error.WriteLine("unknown screen: " + args[i] + " (expected title, main-menu, config, world or pause)");
            System.Environment.Exit(2);
            return ScreenType.TITLE;
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
                "  --seed N                world seed; 0 picks one",
                "  --no-vsync              do not wait for vertical sync",
                "  --smoke-resize          resize part-way through, to exercise resize handling",
                "  --render-stats          report draw calls, frame times and allocation on exit",
                "  --render-distance N     start at this render distance; 0 keeps the configured one",
                "  --no-culling            submit every primitive, culling nothing",
                "  --no-labels             draw no world-space nametags",
                "  --first-person          open in first person; V toggles the view mid-run",
                "  --no-usage-reporting    send nothing to trace this run (see README, Usage reporting)",
                "  --debug-mode            open with the F1 debug overlay already on",
                "  --start-screen NAME     open on title, main-menu, config, world or pause",
                "  --help, -h              print this and exit"
            });
        }

        private static void printUsage() {
            Console.WriteLine(getUsageText());
        }
    }
}
