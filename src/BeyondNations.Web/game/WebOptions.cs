using System;
using System.Collections.Generic;
using System.Globalization;

namespace beyondnations.web {

    /**
    * The page's URL query, turned into the command line GameOptions.parse
    * already understands, so the browser takes the desktop's options by the
    * same names:
    *
    *   ?ticks-per-second=20&seed=42&start-screen=world&debug-mode
    *
    * is `--ticks-per-second 20 --seed 42 --start-screen world --debug-mode`.
    *
    * GameOptions.parse ends the process on a value it cannot read, which in
    * a browser would take the runtime down with it, so every pair is checked
    * here first and anything unknown or malformed is dropped with a warning
    * instead. Options that mean nothing in a page (window size, vsync, the
    * resize smoke test, usage reporting) are dropped the same way.
    */
    public static class WebOptions {

        private static readonly HashSet<string> IntOptions = new HashSet<string> {
            "ticks-per-second", "exit-after-frames", "screenshot-after-frames", "seed", "render-distance",
        };

        private static readonly HashSet<string> FlagOptions = new HashSet<string> {
            "render-stats", "no-culling", "no-labels", "debug-mode", "first-person", "stage-diplomacy",
        };

        private static readonly Dictionary<string, string[]> ChoiceOptions = new Dictionary<string, string[]> {
            { "start-screen", new[] { "title", "main-menu", "config", "world", "pause", "inventory", "controls", "nations", "crafting" } },
        };

        /** Returns the argument list; warnings go to warn. */
        public static List<string> toArguments(string query, Action<string> warn) {
            List<string> args = new List<string>();
            if (string.IsNullOrEmpty(query)) {
                return args;
            }
            string trimmed = query[0] == '?' ? query.Substring(1) : query;
            foreach (string pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries)) {
                int equals = pair.IndexOf('=');
                string name = Uri.UnescapeDataString((equals < 0 ? pair : pair.Substring(0, equals)).Replace('+', ' ')).Trim();
                string value = equals < 0 ? "" : Uri.UnescapeDataString(pair.Substring(equals + 1).Replace('+', ' ')).Trim();
                if (name.StartsWith("--")) {
                    name = name.Substring(2);
                }
                name = name.ToLowerInvariant();

                if (IntOptions.Contains(name)) {
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
                            && (name != "ticks-per-second" || number > 0)) {
                        args.Add("--" + name);
                        args.Add(number.ToString(CultureInfo.InvariantCulture));
                    } else {
                        warn("ignoring " + name + "=" + value + ": not a usable number");
                    }
                } else if (FlagOptions.Contains(name)) {
                    if (value == "" || value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                        args.Add("--" + name);
                    } else if (!(value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))) {
                        warn("ignoring " + name + "=" + value + ": expected nothing, 1/true or 0/false");
                    }
                } else if (ChoiceOptions.TryGetValue(name, out string[] choices)) {
                    string lower = value.ToLowerInvariant();
                    if (Array.IndexOf(choices, lower) >= 0) {
                        args.Add("--" + name);
                        args.Add(lower);
                    } else {
                        warn("ignoring " + name + "=" + value + ": expected one of " + string.Join(", ", choices));
                    }
                } else if (name == "time-of-day") {
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fraction)
                            && !float.IsNaN(fraction) && fraction >= 0f && fraction < 1f) {
                        args.Add("--time-of-day");
                        args.Add(fraction.ToString("R", CultureInfo.InvariantCulture));
                    } else {
                        warn("ignoring time-of-day=" + value + ": expected a number in [0, 1)");
                    }
                } else {
                    warn("ignoring unknown option " + name);
                }
            }
            return args;
        }
    }
}
