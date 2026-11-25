using System.Collections.Generic;

namespace beyondnations.desktop.ui.hud {

    /**
    * Turns changes in the world into overlay items: an objective done, a
    * season turning, a new era, the player's chronicle news. Each watcher
    * remembers what it last saw; the first look (a new world, or a loaded
    * save) only takes note, so nothing already true is announced (FishE's
    * old-save rule). Session memory only: nothing here is saved.
    *
    * Pure: it is handed plain values, so it is checked by unit tests
    * (TestHudFeed) without a simulation or a window.
    */
    public class HudFeed {
        private bool seasonSeen;
        private string lastSeason;
        private int lastYear;
        private bool eraSeen;
        private string lastEra;
        private bool objectiveSeen;
        private int lastCompletedTick;
        private bool chronicleSeen;
        private int lastSequence;
        private bool landSeen;
        private string lastLand;
        private string lastRegion;
        private int crossings;
        private string landKey;
        private bool landPending;
        private string pendingLand;
        private string pendingRegion;
        private double pendingSince;
        private readonly Dictionary<string, double> landLastSeen = new Dictionary<string, double>();

        /**
        * How long, in seconds, new land or a new region must stay underfoot
        * before it is announced, so a few steps over a border and back say
        * nothing.
        */
        public const double LandSettleSeconds = 2.0;

        /**
        * Land and region the player stood on within this many seconds are
        * not announced again, so walking along a border does not repeat the
        * same two toasts.
        */
        public const double LandRecentSeconds = 45.0;

        /** Forget what was seen (a new world or a load): the next look only takes note. */
        public void reset() {
            seasonSeen = false;
            eraSeen = false;
            objectiveSeen = false;
            chronicleSeen = false;
            landSeen = false;
            landKey = null;
            landPending = false;
            landLastSeen.Clear();
        }

        /**
        * The season as it stands; a toast when it has turned. Autumn's says
        * winter is coming, winter's that nothing grows.
        */
        public OverlayItem season(string season, int year) {
            if (!seasonSeen) {
                seasonSeen = true;
                lastSeason = season;
                lastYear = year;
                return null;
            }
            if (season == lastSeason && year == lastYear) {
                return null;
            }
            lastSeason = season;
            lastYear = year;
            return OverlayItem.toast("season:" + season + ":" + year, season + " has come", seasonNote(season));
        }

        /** The line under a season's toast. */
        public static string seasonNote(string season) {
            switch (season) {
                case "Autumn": return "Winter follows: gather food for the cold days.";
                case "Winter": return "Nothing grows. Eat from your stores.";
                case "Spring": return "The trees bear fruit again.";
                default: return "Long days for gathering and building.";
            }
        }

        /**
        * The era as it stands; a card when a new one is reached, with what it
        * unlocks (one short phrase each).
        */
        public OverlayItem era(string era, IList<string> unlocks) {
            if (!eraSeen) {
                eraSeen = true;
                lastEra = era;
                return null;
            }
            if (era == lastEra) {
                return null;
            }
            lastEra = era;
            string body = unlocks == null || unlocks.Count == 0
                ? "Your nation has grown into a new age."
                : "Unlocks: " + string.Join(", ", unlocks) + ".";
            return OverlayItem.unlock("era:" + era, "A new era: " + era, body);
        }

        /**
        * The last objective completed (its tick, -1 for none); a toast when a
        * new one has been, with its reward.
        */
        public OverlayItem objective(int completedTick, string title, string reward) {
            if (!objectiveSeen) {
                objectiveSeen = true;
                lastCompletedTick = completedTick;
                return null;
            }
            if (completedTick == lastCompletedTick || completedTick < 0 || title == null) {
                lastCompletedTick = completedTick;
                return null;
            }
            lastCompletedTick = completedTick;
            string body = string.IsNullOrEmpty(reward) ? title : title + "\n" + reward;
            return OverlayItem.toast("objective:" + completedTick, "Objective complete!", body);
        }

        /**
        * The newest chronicle entry's sequence number (-1 for none) and the
        * player's news among the entries newer than the last look, oldest
        * first; a toast for the newest of them, saying how many more there
        * were. Entries that are not the player's news pass silently.
        */
        public OverlayItem chronicle(int newestSequence, IList<KeyValuePair<int, string>> playersNews) {
            if (!chronicleSeen) {
                chronicleSeen = true;
                lastSequence = newestSequence;
                return null;
            }
            if (newestSequence <= lastSequence) {
                lastSequence = newestSequence;
                return null;
            }
            int since = lastSequence;
            lastSequence = newestSequence;
            KeyValuePair<int, string>? newest = null;
            int count = 0;
            if (playersNews != null) {
                foreach (KeyValuePair<int, string> entry in playersNews) {
                    if (entry.Key > since) {
                        newest = entry;
                        count++;
                    }
                }
            }
            if (newest == null) {
                return null;
            }
            string title = count > 1 ? "News (+" + (count - 1) + " more)" : "News";
            return OverlayItem.toast("chronicle:" + newest.Value.Key, title, newest.Value.Value);
        }

        /**
        * Whose land the player stands on (null for unclaimed land; own says
        * it is the player's nation's) and the region underfoot, at time now
        * (seconds); a short toast when either has changed: "Now on
        * Blackburg's land", "Unclaimed land", "Back on your own land", or
        * just the new region. Inside a settlement nothing is looked at, so
        * entering and leaving one is not a crossing.
        *
        * With hysteresis: the new land and region must stay underfoot for
        * LandSettleSeconds before they count, and land and region stood on
        * within the last LandRecentSeconds are taken note of silently, so
        * walking along a border does not repeat toasts.
        */
        public OverlayItem land(string owner, bool own, string region, string regionNote, bool inSettlement, double now) {
            if (inSettlement) {
                landPending = false;
                return null;
            }
            string land = owner == null ? null : (own ? "\u0001own" : owner);
            if (!landSeen) {
                landSeen = true;
                lastLand = land;
                lastRegion = region;
                landPending = false;
                landLastSeen[placeKey(land, region)] = now;
                return null;
            }
            if (land == lastLand && region == lastRegion) {
                landPending = false;
                landLastSeen[placeKey(land, region)] = now;
                return null;
            }
            if (!landPending || land != pendingLand || region != pendingRegion) {
                landPending = true;
                pendingLand = land;
                pendingRegion = region;
                pendingSince = now;
                return null;
            }
            if (now - pendingSince < LandSettleSeconds) {
                return null;
            }
            // settled: the new land and region count from here
            landPending = false;
            bool landChanged = land != lastLand;
            landLastSeen[placeKey(lastLand, lastRegion)] = now;
            lastLand = land;
            lastRegion = region;
            string key = placeKey(land, region);
            bool recent = landLastSeen.TryGetValue(key, out double seen) && now - seen <= LandRecentSeconds + LandSettleSeconds;
            landLastSeen[key] = now;
            if (recent) {
                return null;
            }
            string regionLine = region == null ? null
                : (string.IsNullOrEmpty(regionNote) ? region : region + ": " + regionNote);
            string title = landChanged ? landTitle(owner, own) : "Into the " + region;
            string body = landChanged ? regionLine : regionNote;
            crossings++;
            landKey = "land:" + crossings;
            return OverlayItem.toast(landKey, title, body ?? "");
        }

        private static string placeKey(string land, string region) {
            return (land ?? "\u0002wild") + "|" + (region ?? "");
        }

        /** "Now on Blackburg's land", "Back on your own land", or "Unclaimed land". */
        public static string landTitle(string owner, bool own) {
            if (owner == null) {
                return "Unclaimed land";
            }
            return own ? "Back on your own land" : "Now on " + owner + "'s land";
        }

        /** The key of the last land toast made (null for none), so a newer one can take its place. */
        public string getLandKey() {
            return landKey;
        }

        /** Whether entries newer than sequence need looking at (the chronicle has grown since the last look). */
        public bool chronicleChanged(int newestSequence) {
            return !chronicleSeen || newestSequence != lastSequence;
        }

        /** The last chronicle sequence looked at (-1 before the first look). */
        public int getLastSequence() {
            return chronicleSeen ? lastSequence : -1;
        }
    }
}
