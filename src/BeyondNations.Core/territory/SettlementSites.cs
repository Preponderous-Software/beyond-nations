using System.Numerics;

namespace beyondnations {

    /**
    * Where a settlement may be founded, and which way to walk when it may
    * not. A refusal that only said "Too close to another settlement." sent
    * playtesters sprinting in random directions (seven of nine sessions):
    * the answer now names the settlement in the way, how far off it is, the
    * minimum spacing, and the nearest open ground.
    *
    * Open ground is found by walking out from the player along the sixteen
    * points of the compass, a step at a time, and taking the nearest point
    * that is clear of every settlement and outside every other nation's
    * claim. It runs only when a founding is refused, so the search is cheap
    * enough as it is.
    */
    public static class SettlementSites {
        /** How far out the search for open ground looks before giving up. */
        public const float SEARCH_RADIUS = 1000f;
        public const float SEARCH_STEP = 10f;
        private const int SEARCH_DIRECTIONS = 16;

        /** The nearest settlement closer than minDistance to the position, or null. Measured as FoundSettlementCommand measures. */
        public static Settlement findTooClose(EntityRepository entityRepository, Vector3 position, int minDistance) {
            Settlement nearest = null;
            int nearestDistance = int.MaxValue;
            foreach (Settlement settlement in entityRepository.getEntitiesOfType(EntityType.SETTLEMENT)) {
                int distance = (int) Vector3.Distance(position, settlement.getPosition());
                if (distance < minDistance && distance < nearestDistance) {
                    nearest = settlement;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        /**
        * Whether founder could found a settlement here: no settlement within
        * minDistance on the ground, and not in another nation's claim.
        */
        public static bool isOpen(EntityRepository entityRepository, Vector3 position, int minDistance, NationId founder) {
            foreach (Settlement settlement in entityRepository.getEntitiesOfType(EntityType.SETTLEMENT)) {
                // the ground distance never exceeds the distance the command checks, so a site clear here is clear there
                if (Territory.groundDistance(position, settlement.getPosition()) < minDistance) {
                    return false;
                }
            }
            Settlement claim = Territory.findClaimAt(entityRepository, position);
            return claim == null || claim.getNationId() == founder;
        }

        /**
        * The nearest open site to from, within SEARCH_RADIUS, or null when
        * there is none. Of the open sites at the same reach, the one with the
        * most room from its nearest settlement: straight away from the
        * settlement in the way rather than glancing past it.
        */
        public static Vector3? findNearestOpenSite(EntityRepository entityRepository, Vector3 from, int minDistance, NationId founder) {
            for (float reach = SEARCH_STEP; reach <= SEARCH_RADIUS; reach += SEARCH_STEP) {
                Vector3? best = null;
                float bestClearance = -1f;
                for (int i = 0; i < SEARCH_DIRECTIONS; i++) {
                    float angle = i * (2f * System.MathF.PI / SEARCH_DIRECTIONS);
                    Vector3 candidate = new Vector3(from.X + System.MathF.Cos(angle) * reach, from.Y, from.Z + System.MathF.Sin(angle) * reach);
                    if (!isOpen(entityRepository, candidate, minDistance, founder)) {
                        continue;
                    }
                    float clearance = getClearance(entityRepository, candidate);
                    if (clearance > bestClearance) {
                        best = candidate;
                        bestClearance = clearance;
                    }
                }
                if (best != null) {
                    return best;
                }
            }
            return null;
        }

        /** The ground distance from position to the nearest settlement; MaxValue when there is none. */
        private static float getClearance(EntityRepository entityRepository, Vector3 position) {
            float nearest = float.MaxValue;
            foreach (Settlement settlement in entityRepository.getEntitiesOfType(EntityType.SETTLEMENT)) {
                nearest = System.MathF.Min(nearest, Territory.groundDistance(position, settlement.getPosition()));
            }
            return nearest;
        }

        /** "Open ground lies about 90 to the south-west." or, when none is near, "No open ground lies within 1000." */
        public static string describeNearestOpenSite(EntityRepository entityRepository, Vector3 from, int minDistance, NationId founder) {
            Vector3? site = findNearestOpenSite(entityRepository, from, minDistance, founder);
            if (site == null) {
                return "No open ground lies within " + (int) SEARCH_RADIUS + ".";
            }
            int distance = (int) System.MathF.Ceiling(Territory.groundDistance(from, site.Value));
            return "Open ground lies about " + distance + " to the " + Compass.describe(from, site.Value) + ".";
        }

        /**
        * "Too close to the Camp of Aland: 175 away, and settlements must be
        * 250 apart. Open ground lies about 80 to the west."
        */
        public static string describeTooClose(EntityRepository entityRepository, Vector3 position, Settlement tooClose, int minDistance, NationId founder) {
            int distance = (int) Vector3.Distance(position, tooClose.getPosition());
            return "Too close to the " + tooClose.getDisplayName() + ": " + distance + " away, and settlements must be " + minDistance + " apart. "
                + describeNearestOpenSite(entityRepository, position, minDistance, founder);
        }
    }

    /** The eight points of the compass, as the world lays them out: east is +X and north is +Z. */
    public static class Compass {
        private static readonly string[] Points = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

        /** Which way to from lies, e.g. "south-west"; "here" when the two are the same point on the ground. */
        public static string describe(Vector3 from, Vector3 to) {
            float dx = to.X - from.X;
            float dz = to.Z - from.Z;
            if (dx * dx + dz * dz < 0.0001f) {
                return "here";
            }
            float angle = System.MathF.Atan2(dz, dx);
            int index = (int) System.MathF.Round(angle / (System.MathF.PI / 4f));
            return Points[((index % Points.Length) + Points.Length) % Points.Length];
        }
    }
}
