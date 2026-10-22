using System.Text.Json;
using System;
using System.Collections.Generic;

namespace beyondnations {

    /**
    * The standing between every pair of nations: one score per pair, from
    * -100 to 100, starting at zero, plus whether the pair has agreed an
    * alliance and whether one has denounced the other.
    *
    * The score is symmetric -- Xland's view of Yshire is Yshire's view of
    * Xland -- so it is stored once per unordered pair. Named states are read
    * off it: Hostile at -50 and below, Rival at -20 and below, Friendly at 20
    * and above, Neutral between, and Allied while an alliance stands.
    *
    * What moves the score lives with whatever causes it (Diplomacy for borders
    * and shared enemies, the commands for gifts, denunciations, trade and
    * poaching); this only keeps the numbers, and reports through the event
    * producer whenever a pair's named state changes, so the record of the
    * world shows alliances forming and rivalries hardening.
    */
    public class NationRelations {
        public const int MIN_SCORE = -100;
        public const int MAX_SCORE = 100;
        public const int FRIENDLY_AT = 20;
        public const int RIVAL_AT = -20;
        public const int HOSTILE_AT = -50;

        /** The score at which a nation will agree an alliance. */
        public const int ALLIANCE_REQUIRES = 30;

        private readonly Dictionary<string, int> scores = new Dictionary<string, int>();
        private readonly HashSet<string> alliances = new HashSet<string>();
        private readonly HashSet<string> denounced = new HashSet<string>();
        private EventProducer eventProducer;

        public void setEventProducer(EventProducer eventProducer) {
            this.eventProducer = eventProducer;
        }

        /**
        * The same key for (a, b) and (b, a). NationIds are Guids, so ordering
        * them says nothing about the world; it only has to be consistent.
        */
        private static string key(Nation a, Nation b) {
            string x = a.getId().ToString();
            string y = b.getId().ToString();
            return string.CompareOrdinal(x, y) <= 0 ? x + "|" + y : y + "|" + x;
        }

        public int getScore(Nation a, Nation b) {
            if (a == null || b == null || a == b) {
                return 0;
            }
            int score;
            return scores.TryGetValue(key(a, b), out score) ? score : 0;
        }

        public bool isAllied(Nation a, Nation b) {
            return a != null && b != null && a != b && alliances.Contains(key(a, b));
        }

        public static NationRelationState stateFor(int score, bool allied) {
            if (allied) {
                return NationRelationState.ALLIED;
            }
            if (score <= HOSTILE_AT) {
                return NationRelationState.HOSTILE;
            }
            if (score <= RIVAL_AT) {
                return NationRelationState.RIVAL;
            }
            if (score >= FRIENDLY_AT) {
                return NationRelationState.FRIENDLY;
            }
            return NationRelationState.NEUTRAL;
        }

        public NationRelationState getState(Nation a, Nation b) {
            return stateFor(getScore(a, b), isAllied(a, b));
        }

        /** "Rival (-27)": the state, and the score with its sign. */
        public string describe(Nation a, Nation b) {
            int score = getScore(a, b);
            return NationRelationStates.getName(getState(a, b)) + " (" + (score > 0 ? "+" : "") + score + ")";
        }

        /**
        * Moves the pair's score by delta, clamped to the range. An alliance
        * does not survive the score falling below zero. Returns the state the
        * pair is in afterwards. why, when given, is told with any change of
        * named state ("over their border", RelationCause).
        */
        public NationRelationState adjust(Nation a, Nation b, int delta, string why = null) {
            if (a == null || b == null || a == b || delta == 0) {
                return getState(a, b);
            }
            NationRelationState before = getState(a, b);
            int score = Math.Max(MIN_SCORE, Math.Min(MAX_SCORE, getScore(a, b) + delta));
            scores[key(a, b)] = score;

            if (isAllied(a, b) && score < 0) {
                breakAlliance(a, b);
            }
            // a denunciation is forgotten once relations have recovered
            if (score > RIVAL_AT) {
                denounced.Remove(key(a, b));
            }

            NationRelationState after = getState(a, b);
            if (after != before && after != NationRelationState.ALLIED && before != NationRelationState.ALLIED) {
                eventProducer?.produceNationRelationChangeEvent(a, b, before, after, why);
            }
            return after;
        }

        /** Agrees an alliance. Returns false if the pair is already allied. */
        public bool formAlliance(Nation a, Nation b) {
            if (a == null || b == null || a == b || isAllied(a, b)) {
                return false;
            }
            NationRelationState before = getState(a, b);
            alliances.Add(key(a, b));
            denounced.Remove(key(a, b));
            eventProducer?.produceNationRelationChangeEvent(a, b, before, NationRelationState.ALLIED);
            return true;
        }

        public void breakAlliance(Nation a, Nation b) {
            if (!isAllied(a, b)) {
                return;
            }
            alliances.Remove(key(a, b));
            eventProducer?.produceNationRelationChangeEvent(a, b, NationRelationState.ALLIED, getState(a, b));
        }

        public bool hasDenounced(Nation a, Nation b) {
            return a != null && b != null && denounced.Contains(key(a, b));
        }

        public void markDenounced(Nation a, Nation b) {
            if (a != null && b != null && a != b) {
                denounced.Add(key(a, b));
            }
        }

        /** Drops every record involving a nation that no longer exists. */
        public void forget(Nation nation) {
            string id = nation.getId().ToString();
            List<string> stale = new List<string>();
            foreach (string pair in scores.Keys) {
                if (pair.StartsWith(id + "|") || pair.EndsWith("|" + id)) {
                    stale.Add(pair);
                }
            }
            foreach (string pair in stale) {
                scores.Remove(pair);
            }
            alliances.RemoveWhere(pair => pair.StartsWith(id + "|") || pair.EndsWith("|" + id));
            denounced.RemoveWhere(pair => pair.StartsWith(id + "|") || pair.EndsWith("|" + id));
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.beginArray("scores");
            foreach (KeyValuePair<string, int> entry in scores) {
                writer.beginArray();
                writer.writeValue(entry.Key);
                writer.writeValue(entry.Value);
                writer.endArray();
            }
            writer.endArray();
            writer.writeStringList("alliances", alliances);
            writer.writeStringList("denounced", denounced);
        }

        internal void load(JsonElement element) {
            scores.Clear();
            foreach (JsonElement entry in SaveReader.property(element, "scores").EnumerateArray()) {
                scores[entry[0].GetString()] = entry[1].GetInt32();
            }
            alliances.Clear();
            alliances.UnionWith(SaveReader.getStringList(element, "alliances"));
            denounced.Clear();
            denounced.UnionWith(SaveReader.getStringList(element, "denounced"));
        }
    }
}
