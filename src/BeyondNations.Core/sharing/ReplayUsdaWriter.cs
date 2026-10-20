using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace beyondnations {

    /**
    * Writes a RunRecorder's samples as a usd-replay/1 replay: one plain-text
    * OpenUSD layer (.usda) that usd-recorder's `replay validate` accepts and
    * any usd-replay/1 player -- usd-web's browser player among them -- can
    * show. See docs/SHARING.md, and usd-recorder's docs/REPLAY.md for the
    * contract itself.
    *
    * Core has no USD library and takes no package, so the layer is written
    * as text, in the layout usd-recorder's own ReplayWriter produces:
    *
    *   /Replay            Xform, the default prim; replay:stateHash and the
    *                      replay:stat:* counters sampled at every sample tick
    *   /Replay/Entities   one Points prim per set (pawns, settlements, the
    *                      player, chickens), ids and points at every sample
    *                      tick; pawns and settlements carry their nation's
    *                      colour per point
    *   /Replay/Scene      the trees standing when the replay was exported,
    *                      as static points
    *
    * Z-up, one time code per tick. Colours are written linear, as USD's
    * displayColor is, from the sRGB the game draws.
    */
    public static class ReplayUsdaWriter {
        public const string Schema = "usd-replay/1";
        public const string Sim = "beyond-nations";
        public const string StateHashAlgorithm =
            "in-game RunRecorder FNV-1a/64: tick, day, nation count, and each recorded entity's id, "
            + "position in thousandths and nation colour, set by set";

        private sealed class SetSpec {
            public readonly string name;
            public readonly string label;
            public readonly string shape;
            public readonly float width;
            public readonly string colour; // #rrggbb when constant; null for one per point

            public SetSpec(string name, string label, string shape, float width, string colour) {
                this.name = name;
                this.label = label;
                this.shape = shape;
                this.width = width;
                this.colour = colour;
            }
        }

        // Drawn sizes are exaggerated, as in tools/history-to-replay.py, so
        // they read from far enough away to see the map (a tile is 15 wide).
        private static readonly SetSpec[] Sets = {
            new SetSpec("pawns", "Pawns", "sphere", 28f, null),
            new SetSpec("settlements", "Settlements", "cube", 70f, null),
            new SetSpec("player", "You", "sphere", 40f, "#f2c94c"),
            new SetSpec("chickens", "Chickens", "sphere", 10f, "#f2efe6"),
        };
        private const float TreeWidth = 14f;
        private const string TreeColour = "#2f7d32";

        /** The drawn diameter of a RunRecorder set. */
        public static float widthOf(int set) {
            return Sets[set].width;
        }

        /** A file name for a replay of this world: "beyond-nations-seed-42-tick-9000.usda". */
        public static string fileName(int seed, int tick) {
            return "beyond-nations-seed-" + seed.ToString(CultureInfo.InvariantCulture).Replace('-', 'm')
                + "-tick-" + tick.ToString(CultureInfo.InvariantCulture) + ".usda";
        }

        /**
        * The replay of everything recorder holds, ending with a sample at the
        * simulation's current tick (taken now if there is none there yet).
        */
        public static string write(RunRecorder recorder, Simulation simulation, string simVersion) {
            recorder.recordNow(simulation);
            IReadOnlyList<RunRecorder.Sample> samples = recorder.getSamples();
            if (samples.Count == 0) {
                throw new InvalidOperationException("nothing has been recorded");
            }
            int seed = simulation.getRandom().getSeed();
            int ticksPerSecond = simulation.getGameConfig().getTicksPerSecond();
            int first = samples[0].tick;
            int last = samples[samples.Count - 1].tick;
            // Ten samples a second of playback, whatever the interval became.
            double playbackRate = recorder.getInterval() * 10.0;

            StringBuilder usda = new StringBuilder(64 * 1024);
            usda.Append("#usda 1.0\n(\n");
            usda.Append("    customLayerData = {\n");
            usda.Append("        dictionary usdReplay = {\n");
            usda.Append("            string description = ").Append(quote(
                "Recorded in play: ticks " + first + " to " + last + " (day " + (samples[0].day + 1) + " to day "
                + (samples[samples.Count - 1].day + 1) + "), a sample every " + recorder.getInterval() + " ticks.")).Append('\n');
            usda.Append("            dictionary params = {\n");
            usda.Append("                int firstTick = ").Append(first).Append('\n');
            usda.Append("                int interval = ").Append(recorder.getInterval()).Append('\n');
            usda.Append("                int lastTick = ").Append(last).Append('\n');
            usda.Append("                int samples = ").Append(samples.Count).Append('\n');
            usda.Append("                int worldSeed = ").Append(seed).Append('\n');
            usda.Append("            }\n");
            usda.Append("            string schema = ").Append(quote(Schema)).Append('\n');
            // usd-replay/1 wants the seed as decimal digits; a negative seed
            // (a fresh one from the clock can be) is written as its unsigned
            // 32-bit value and kept as it is in params.worldSeed.
            usda.Append("            string seed = ").Append(quote(((uint) seed).ToString(CultureInfo.InvariantCulture))).Append('\n');
            usda.Append("            string sim = ").Append(quote(Sim)).Append('\n');
            usda.Append("            string simVersion = ").Append(quote(string.IsNullOrEmpty(simVersion) ? "unknown" : simVersion)).Append('\n');
            usda.Append("            string source = ").Append(quote("Beyond Nations: Share > Export replay")).Append('\n');
            usda.Append("            string stateHashAlgorithm = ").Append(quote(StateHashAlgorithm)).Append('\n');
            usda.Append("            double tickRate = ").Append(ticksPerSecond).Append('\n');
            usda.Append("            string title = ").Append(quote("Beyond Nations, seed " + seed)).Append('\n');
            usda.Append("        }\n");
            usda.Append("    }\n");
            usda.Append("    defaultPrim = \"Replay\"\n");
            usda.Append("    endTimeCode = ").Append(last).Append('\n');
            usda.Append("    framesPerSecond = ").Append(number(playbackRate)).Append('\n');
            usda.Append("    metersPerUnit = 1\n");
            usda.Append("    startTimeCode = ").Append(first).Append('\n');
            usda.Append("    timeCodesPerSecond = ").Append(number(playbackRate)).Append('\n');
            usda.Append("    upAxis = \"Z\"\n");
            usda.Append(")\n\n");

            usda.Append("def Xform \"Replay\"\n{\n");
            writeStat(usda, "day", samples, s => s.day + 1);
            writeStat(usda, "pawns", samples, s => s.count(RunRecorder.Pawns));
            writeStat(usda, "settlements", samples, s => s.count(RunRecorder.Settlements));
            writeStat(usda, "nations", samples, s => s.nations);
            writeStat(usda, "chickens", samples, s => s.count(RunRecorder.Chickens));
            usda.Append("    custom string replay:stateHash\n");
            usda.Append("    string replay:stateHash.timeSamples = {\n");
            foreach (RunRecorder.Sample sample in samples) {
                usda.Append("        ").Append(sample.tick).Append(": ").Append(quote(sample.stateHash)).Append(",\n");
            }
            usda.Append("    }\n\n");

            usda.Append("    def Scope \"Entities\"\n    {\n");
            for (int s = 0; s < Sets.Length; s++) {
                writeSet(usda, s, samples);
            }
            usda.Append("    }\n");

            List<Vector3> trees = standingTrees(simulation);
            if (trees.Count > 0) {
                usda.Append("\n    def Scope \"Scene\"\n    {\n");
                usda.Append("        def Points \"trees\"\n        {\n");
                usda.Append("            point3f[] points = ");
                appendPoints(usda, trees);
                usda.Append('\n');
                usda.Append("            color3f[] primvars:displayColor = [").Append(linearColour(TreeColour)).Append("] (\n");
                usda.Append("                interpolation = \"constant\"\n            )\n");
                usda.Append("            custom string replay:label = ").Append(quote("Trees (when exported)")).Append('\n');
                usda.Append("            custom token replay:shape = \"sphere\"\n");
                usda.Append("            float[] widths = [").Append(number(TreeWidth)).Append("] (\n");
                usda.Append("                interpolation = \"constant\"\n            )\n");
                usda.Append("        }\n");
                usda.Append("    }\n");
            }
            usda.Append("}\n");
            return usda.ToString();
        }

        private static void writeStat(StringBuilder usda, string name, IReadOnlyList<RunRecorder.Sample> samples, Func<RunRecorder.Sample, int> value) {
            usda.Append("    custom double replay:stat:").Append(name).Append('\n');
            usda.Append("    double replay:stat:").Append(name).Append(".timeSamples = {\n");
            foreach (RunRecorder.Sample sample in samples) {
                usda.Append("        ").Append(sample.tick).Append(": ").Append(value(sample)).Append(",\n");
            }
            usda.Append("    }\n");
        }

        private static void writeSet(StringBuilder usda, int set, IReadOnlyList<RunRecorder.Sample> samples) {
            SetSpec spec = Sets[set];
            usda.Append("        def Points \"").Append(spec.name).Append("\"\n        {\n");
            usda.Append("            int64[] ids.timeSamples = {\n");
            foreach (RunRecorder.Sample sample in samples) {
                usda.Append("                ").Append(sample.tick).Append(": [");
                int[] ids = sample.sets[set].ids;
                for (int i = 0; i < ids.Length; i++) {
                    if (i > 0) usda.Append(", ");
                    usda.Append(ids[i]);
                }
                usda.Append("],\n");
            }
            usda.Append("            }\n");
            usda.Append("            point3f[] points.timeSamples = {\n");
            foreach (RunRecorder.Sample sample in samples) {
                usda.Append("                ").Append(sample.tick).Append(": ");
                appendPoints(usda, sample.sets[set].points);
                usda.Append(",\n");
            }
            usda.Append("            }\n");
            if (spec.colour != null) {
                usda.Append("            color3f[] primvars:displayColor = [").Append(linearColour(spec.colour)).Append("] (\n");
                usda.Append("                interpolation = \"constant\"\n            )\n");
            } else {
                usda.Append("            color3f[] primvars:displayColor (\n");
                usda.Append("                interpolation = \"vertex\"\n            )\n");
                usda.Append("            color3f[] primvars:displayColor.timeSamples = {\n");
                foreach (RunRecorder.Sample sample in samples) {
                    usda.Append("                ").Append(sample.tick).Append(": [");
                    byte[] rgb = sample.sets[set].colours;
                    for (int i = 0; i + 2 < rgb.Length; i += 3) {
                        if (i > 0) usda.Append(", ");
                        usda.Append('(').Append(colourNumber(srgbToLinear(rgb[i] / 255.0))).Append(", ")
                            .Append(colourNumber(srgbToLinear(rgb[i + 1] / 255.0))).Append(", ")
                            .Append(colourNumber(srgbToLinear(rgb[i + 2] / 255.0))).Append(')');
                    }
                    usda.Append("],\n");
                }
                usda.Append("            }\n");
            }
            usda.Append("            custom string replay:label = ").Append(quote(spec.label)).Append('\n');
            usda.Append("            custom token replay:shape = ").Append(quote(spec.shape)).Append('\n');
            usda.Append("            float[] widths = [").Append(number(spec.width)).Append("] (\n");
            usda.Append("                interpolation = \"constant\"\n            )\n");
            usda.Append("        }\n");
            if (set < Sets.Length - 1) {
                usda.Append('\n');
            }
        }

        private static List<Vector3> standingTrees(Simulation simulation) {
            List<Vector3> trees = new List<Vector3>();
            EntityRepository repository = simulation.getEntityRepository();
            int count = repository.getEntityCount();
            for (int i = 0; i < count; i++) {
                Entity entity = repository.getEntityAt(i);
                if (entity == null || entity.isMarkedForDeletion() || !entity.isVisible()) {
                    continue;
                }
                if (entity.getType() == EntityType.TREE) {
                    trees.Add(RunRecorder.toReplaySpace(entity.getPosition(), TreeWidth));
                }
            }
            return trees;
        }

        // --- text -----------------------------------------------------------

        private static void appendPoints(StringBuilder usda, IList<Vector3> points) {
            usda.Append('[');
            for (int i = 0; i < points.Count; i++) {
                if (i > 0) usda.Append(", ");
                Vector3 p = points[i];
                usda.Append('(').Append(number(p.X)).Append(", ").Append(number(p.Y)).Append(", ").Append(number(p.Z)).Append(')');
            }
            usda.Append(']');
        }

        /** Three decimals, invariant culture, no exponent: what a .usda float reads back as. */
        private static string number(double value) {
            if (double.IsNaN(value) || double.IsInfinity(value)) {
                return "0";
            }
            return Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string colourNumber(double value) {
            return Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);
        }

        /** sRGB (0..1) to the linear value USD's displayColor holds. */
        public static double srgbToLinear(double c) {
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        private static string linearColour(string hex) {
            int r = int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return "(" + colourNumber(srgbToLinear(r / 255.0)) + ", " + colourNumber(srgbToLinear(g / 255.0)) + ", "
                + colourNumber(srgbToLinear(b / 255.0)) + ")";
        }

        /** A USD string literal: double quotes, with quotes, backslashes and control characters escaped. */
        public static string quote(string text) {
            StringBuilder quoted = new StringBuilder(text.Length + 2);
            quoted.Append('"');
            foreach (char c in text) {
                switch (c) {
                    case '"': quoted.Append("\\\""); break;
                    case '\\': quoted.Append("\\\\"); break;
                    case '\n': quoted.Append("\\n"); break;
                    case '\r': quoted.Append("\\r"); break;
                    case '\t': quoted.Append("\\t"); break;
                    default:
                        if (c < ' ') {
                            quoted.Append(' ');
                        } else {
                            quoted.Append(c);
                        }
                        break;
                }
            }
            quoted.Append('"');
            return quoted.ToString();
        }
    }
}
