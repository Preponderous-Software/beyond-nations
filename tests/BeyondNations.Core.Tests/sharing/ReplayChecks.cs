using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace beyondnationstests {

    /**
    * The parts of usd-recorder's `replay validate` (usd-replay/1) that can be
    * checked on the text of a .usda without a USD library: the required
    * metadata, a Z-up stage with /Replay as its default prim, a state hash
    * at every sample tick, and every entity set sampled at exactly those
    * ticks with as many points (and per-point colours) as ids. CI runs the
    * real validator on an exported replay too (docs/CI-CD.md).
    */
    public static class ReplayChecks {
        private static readonly Regex SampledAttr = new Regex(@"^\s*\S+\s+(\S+)\.timeSamples = \{$");
        private static readonly Regex SampleLine = new Regex(@"^\s*(-?\d+): (.*),$");
        private static readonly Regex PointsPrim = new Regex("^\\s*def Points \"([A-Za-z_][A-Za-z0-9_]*)\"$");

        public sealed class Parsed {
            public readonly Dictionary<string, string> meta = new Dictionary<string, string>();
            public readonly Dictionary<string, string> stage = new Dictionary<string, string>();
            // "prim.attr" -> tick -> raw value; the root's attributes are under "Replay"
            public readonly Dictionary<string, SortedDictionary<int, string>> samples = new Dictionary<string, SortedDictionary<int, string>>();
            public readonly List<string> sets = new List<string>();
            public readonly List<string> vertexColoured = new List<string>();
            public readonly List<string> scene = new List<string>();
        }

        public static Parsed parse(string usda) {
            Parsed parsed = new Parsed();
            string prim = "Replay";
            bool inScene = false;
            string[] lines = usda.Split('\n');
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                Match m;
                if ((m = Regex.Match(line, "^            (string|double) (\\w+) = (.*)$")).Success) {
                    parsed.meta[m.Groups[2].Value] = m.Groups[3].Value.Trim('"');
                } else if ((m = Regex.Match(line, "^    (defaultPrim|endTimeCode|startTimeCode|upAxis|framesPerSecond|timeCodesPerSecond) = (.*)$")).Success) {
                    parsed.stage[m.Groups[1].Value] = m.Groups[2].Value.Trim('"');
                } else if (line.Contains("def Scope \"Scene\"")) {
                    inScene = true;
                } else if (line.Contains("def Scope \"Entities\"")) {
                    inScene = false;
                } else if ((m = PointsPrim.Match(line)).Success) {
                    prim = m.Groups[1].Value;
                    (inScene ? parsed.scene : parsed.sets).Add(prim);
                } else if (line.Contains("interpolation = \"vertex\"")) {
                    parsed.vertexColoured.Add(prim);
                } else if ((m = SampledAttr.Match(line)).Success) {
                    SortedDictionary<int, string> values = new SortedDictionary<int, string>();
                    parsed.samples[prim + "." + m.Groups[1].Value] = values;
                    for (i++; i < lines.Length && lines[i].Trim() != "}"; i++) {
                        Match s = SampleLine.Match(lines[i]);
                        if (s.Success) {
                            values[int.Parse(s.Groups[1].Value, CultureInfo.InvariantCulture)] = s.Groups[2].Value;
                        }
                    }
                }
            }
            return parsed;
        }

        private static int countOf(string array, char open) {
            string inner = array.Trim().TrimStart('[').TrimEnd(']').Trim();
            if (inner.Length == 0) {
                return 0;
            }
            return open == ',' ? inner.Split(',').Length : inner.Count(c => c == open);
        }

        /** Every problem found, as text; empty when the replay holds to the contract as far as text can tell. */
        public static List<string> problems(string usda) {
            List<string> p = new List<string>();
            if (!usda.StartsWith("#usda 1.0\n")) {
                p.Add("not a .usda layer");
            }
            Parsed parsed = parse(usda);
            foreach (string key in new[] { "schema", "sim", "simVersion", "seed", "tickRate", "stateHashAlgorithm" }) {
                if (!parsed.meta.ContainsKey(key) || parsed.meta[key].Length == 0) {
                    p.Add("usdReplay: missing " + key);
                }
            }
            if (parsed.meta.TryGetValue("schema", out string schema) && schema != "usd-replay/1") {
                p.Add("schema is " + schema);
            }
            if (parsed.meta.TryGetValue("seed", out string seed) && !Regex.IsMatch(seed, "^[0-9]+$")) {
                p.Add("seed is not decimal digits: " + seed);
            }
            if (parsed.meta.TryGetValue("tickRate", out string rate) && !(double.Parse(rate, CultureInfo.InvariantCulture) > 0)) {
                p.Add("tickRate is not positive");
            }
            if (!parsed.stage.TryGetValue("upAxis", out string up) || up != "Z") p.Add("upAxis must be Z");
            if (!parsed.stage.TryGetValue("defaultPrim", out string def) || def != "Replay") p.Add("defaultPrim must be Replay");
            if (!usda.Contains("def Xform \"Replay\"")) p.Add("/Replay must be an Xform");
            if (!usda.Contains("def Scope \"Entities\"")) p.Add("/Replay/Entities missing");

            if (!parsed.samples.TryGetValue("Replay.replay:stateHash", out SortedDictionary<int, string> hashes) || hashes.Count == 0) {
                p.Add("no replay:stateHash samples");
                return p;
            }
            List<int> ticks = hashes.Keys.ToList();
            foreach (string hash in hashes.Values) {
                if (!Regex.IsMatch(hash.Trim('"'), "^[0-9a-f]{8,64}$")) {
                    p.Add("bad state hash " + hash);
                    break;
                }
            }
            if (parsed.stage["startTimeCode"] != ticks[0].ToString(CultureInfo.InvariantCulture)
                    || parsed.stage["endTimeCode"] != ticks[ticks.Count - 1].ToString(CultureInfo.InvariantCulture)) {
                p.Add("start/endTimeCode are not the first and last sample ticks");
            }
            foreach (KeyValuePair<string, SortedDictionary<int, string>> attr in parsed.samples) {
                if (attr.Key.StartsWith("Replay.replay:stat:") && attr.Value.Keys.Any(t => !hashes.ContainsKey(t))) {
                    p.Add(attr.Key + " is sampled off the sample ticks");
                }
            }
            foreach (string set in parsed.sets) {
                if (!parsed.samples.TryGetValue(set + ".ids", out SortedDictionary<int, string> ids)
                        || !parsed.samples.TryGetValue(set + ".points", out SortedDictionary<int, string> points)) {
                    p.Add(set + ": needs ids and points");
                    continue;
                }
                if (!ids.Keys.SequenceEqual(ticks) || !points.Keys.SequenceEqual(ticks)) {
                    p.Add(set + ": not sampled at exactly the sample ticks");
                    continue;
                }
                parsed.samples.TryGetValue(set + ".primvars:displayColor", out SortedDictionary<int, string> colours);
                bool vertex = parsed.vertexColoured.Contains(set);
                if (vertex && (colours == null || !colours.Keys.SequenceEqual(ticks))) {
                    p.Add(set + ": per-point colours not sampled at every sample tick");
                    continue;
                }
                foreach (int t in ticks) {
                    int n = countOf(ids[t], ',');
                    if (countOf(points[t], '(') != n) {
                        p.Add(set + "@" + t + ": " + n + " ids but " + countOf(points[t], '(') + " points");
                        break;
                    }
                    List<string> idList = ids[t].Trim('[', ']').Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                    if (idList.Distinct().Count() != idList.Count) {
                        p.Add(set + "@" + t + ": ids are not unique");
                        break;
                    }
                    if (vertex && countOf(colours[t], '(') != n) {
                        p.Add(set + "@" + t + ": needs one colour per point");
                        break;
                    }
                }
            }
            return p;
        }
    }
}
