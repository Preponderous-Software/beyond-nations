using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations.desktop.render {

    /**
    * The camera path for --flyover-out: a glide, high and looking ahead and
    * down, from behind one settlement, over it and across the border until
    * a settlement of a different nation fills the view, so a README can show towns, borders
    * and people as the game draws them. It only chooses where the camera
    * goes; the world is the real one, run by the real simulation, and
    * nothing in it is moved.
    *
    * The pair is the closest two settlements that belong to different
    * nations (the first such pair in the list on a tie, so a seed always
    * picks the same place). The glide eases in and out.
    *
    * --flyover-orbit circles a point instead (orbit): once round the
    * thickest stand of trees, a little above the canopy and looking at its
    * middle, for showing trees (#140).
    */
    public class FlyoverDirector {

        public const float Height = 30f;
        // how far ahead of the eye, along the ground, the camera looks
        public const float LookAhead = 60f;
        // the glide starts this far behind the first settlement, and stops
        // this far short of the second, which is then in the middle of the view
        public const float LeadIn = 100f;
        public const float StopShort = 45f;
        // with nothing to fly between, glide this far from the player
        public const float FallbackLength = 200f;

        private readonly Vector3 start;
        private readonly Vector3 end;
        private readonly Vector3 direction;
        private readonly int frames;

        // --- orbit ---
        public const float OrbitRadius = 26f;
        public const float OrbitHeight = 11f;
        private readonly bool orbiting;
        private readonly Vector3 centre;

        /** Glides along the line from `from` to `to`, starting LeadIn before `from` and stopping StopShort of `to`. */
        public FlyoverDirector(Vector3 from, Vector3 to, int frames) {
            Vector3 flat = new Vector3(to.X - from.X, 0f, to.Z - from.Z);
            direction = flat.LengthSquared() < 1e-6f ? Vector3.UnitZ : Vector3.Normalize(flat);
            float ground = (from.Y + to.Y) / 2f;
            start = new Vector3(from.X, ground, from.Z) - direction * LeadIn;
            end = new Vector3(to.X, ground, to.Z) - direction * StopShort;
            this.frames = Math.Max(1, frames);
        }

        /**
        * Picks the pair from settlement positions and their nations (the two
        * lists run in step). With no such pair, glides out from the player.
        */
        public static FlyoverDirector between(IReadOnlyList<Vector3> settlements, IReadOnlyList<int> nations,
                                              Vector3 fallback, int frames) {
            int bestA = -1;
            int bestB = -1;
            float bestDistance = float.MaxValue;
            for (int a = 0; a < settlements.Count; a++) {
                for (int b = a + 1; b < settlements.Count; b++) {
                    if (nations[a] == nations[b]) {
                        continue;
                    }
                    float distance = flatDistance(settlements[a], settlements[b]);
                    if (distance < bestDistance) {
                        bestDistance = distance;
                        bestA = a;
                        bestB = b;
                    }
                }
            }
            if (bestA < 0) {
                return new FlyoverDirector(fallback, fallback + Vector3.UnitZ * FallbackLength, frames);
            }
            return new FlyoverDirector(settlements[bestA], settlements[bestB], frames);
        }

        private FlyoverDirector(Vector3 centre, int frames) {
            orbiting = true;
            this.centre = centre;
            this.frames = Math.Max(1, frames);
            start = centre + new Vector3(OrbitRadius, 0f, 0f);
            end = start;
            direction = -Vector3.UnitX;
        }

        /** Once round `centre`, OrbitRadius out and OrbitHeight up, always looking at it. */
        public static FlyoverDirector orbit(Vector3 centre, int frames) {
            return new FlyoverDirector(centre, frames);
        }

        /**
        * The tree with the most others within `radius` of it (the first on a
        * tie, so a seed always picks the same stand), or `fallback` with no
        * trees at all.
        */
        public static Vector3 thickestStand(IReadOnlyList<Vector3> trees, float radius, Vector3 fallback) {
            int best = -1;
            int bestCount = -1;
            for (int a = 0; a < trees.Count; a++) {
                int count = 0;
                for (int b = 0; b < trees.Count; b++) {
                    if (a != b && flatDistance(trees[a], trees[b]) <= radius) {
                        count++;
                    }
                }
                if (count > bestCount) {
                    bestCount = count;
                    best = a;
                }
            }
            return best < 0 ? fallback : trees[best];
        }

        public bool isOrbit() {
            return orbiting;
        }

        public Vector3 getStart() {
            return start;
        }

        public Vector3 getEnd() {
            return end;
        }

        public int getFrames() {
            return frames;
        }

        /** How far the camera must see: well past the look-ahead point, into the haze. */
        public float getViewDistance() {
            return 400f;
        }

        /** Where on the ground the camera is over on this frame, eased in and out. */
        public Vector3 getGroundPoint(int frame) {
            if (orbiting) {
                // a whole turn over the run, at an even pace, so the GIF loops
                float angle = 2f * MathF.PI * frame / frames;
                return centre + new Vector3(MathF.Cos(angle) * OrbitRadius, 0f, MathF.Sin(angle) * OrbitRadius);
            }
            float t = frames == 1 ? 0f : Math.Clamp((float) frame / (frames - 1), 0f, 1f);
            float eased = t * t * (3f - 2f * t);
            return Vector3.Lerp(start, end, eased);
        }

        public Vector3 getEye(int frame) {
            return getGroundPoint(frame) + new Vector3(0f, orbiting ? OrbitHeight : Height, 0f);
        }

        public Vector3 getTarget(int frame) {
            if (orbiting) {
                return centre + new Vector3(0f, 3f, 0f);
            }
            return getGroundPoint(frame) + direction * LookAhead;
        }

        private static float flatDistance(Vector3 a, Vector3 b) {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }
}
