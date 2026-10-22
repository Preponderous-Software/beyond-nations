using System;
using System.Numerics;

namespace beyondnations.desktop.playtest {

    /**
    * Angles for the playtest state file (--playtest-out): which way the
    * player faces, and where something lies relative to that.
    *
    * The player's forward is (sin yaw, 0, cos yaw) and a rising yaw turns
    * left (Player.turn, PlayerInputController), so a target whose own angle
    * is below the player's yaw lies to the right. Bearings are reported with
    * + meaning right, because that is the way a harness has to turn (D, or a
    * rightward right-button drag) to face it.
    */
    public static class PlaytestBearing {
        private const float RadiansToDegrees = 180f / MathF.PI;

        /** An angle in degrees brought into (-180, 180]. */
        public static float normalise(float degrees) {
            float wrapped = degrees % 360f;
            if (wrapped <= -180f) {
                wrapped += 360f;
            } else if (wrapped > 180f) {
                wrapped -= 360f;
            }
            return wrapped;
        }

        /** The player's yaw as a heading in degrees, [0, 360). */
        public static float headingDegrees(float yawRadians) {
            float degrees = (yawRadians * RadiansToDegrees) % 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }

        /**
        * Where target lies relative to the way the player at from faces, in
        * degrees in (-180, 180]: 0 straight ahead, + to the right, - to the
        * left, 180 behind. A target on top of the player is 0.
        */
        public static float relativeDegrees(Vector3 from, float yawRadians, Vector3 target) {
            float dx = target.X - from.X;
            float dz = target.Z - from.Z;
            if (dx * dx + dz * dz < 1e-8f) {
                return 0f;
            }
            float targetYaw = MathF.Atan2(dx, dz);
            return normalise((yawRadians - targetYaw) * RadiansToDegrees);
        }
    }
}
