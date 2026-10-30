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
            float t = frames == 1 ? 0f : Math.Clamp((float) frame / (frames - 1), 0f, 1f);
            float eased = t * t * (3f - 2f * t);
            return Vector3.Lerp(start, end, eased);
        }

        public Vector3 getEye(int frame) {
            return getGroundPoint(frame) + new Vector3(0f, Height, 0f);
        }

        public Vector3 getTarget(int frame) {
            return getGroundPoint(frame) + direction * LookAhead;
        }

        private static float flatDistance(Vector3 a, Vector3 b) {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }
}
