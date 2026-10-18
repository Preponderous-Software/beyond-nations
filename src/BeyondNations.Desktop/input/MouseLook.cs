using System;
using System.Numerics;

namespace beyondnations.desktop.input {

    /**
    * Accumulates mouse movement into a yaw/pitch look direction for camera
    * control.
    *
    * The Unity version never had this: it turned the player with A/D
    * (Input.GetAxis("Horizontal") feeding Player.setMovementInput, still
    * wired that way -- see PlayerInputController), and had no independent
    * camera. Player.getYaw() therefore stays keyboard-driven so the README's
    * "A / Left Arrow -> turn left" and "D / Right Arrow -> turn right" keep
    * working exactly as documented. This class exists so the input layer has
    * somewhere to put mouse motion for the camera #219 will add; it does not
    * touch Player or drive movement.
    */
    public class MouseLook {
        private readonly float sensitivityDegreesPerPixel;
        private readonly float minPitchDegrees;
        private readonly float maxPitchDegrees;

        private float yawDegrees;
        private float pitchDegrees;

        public MouseLook(float sensitivityDegreesPerPixel = 0.15f, float minPitchDegrees = -89f, float maxPitchDegrees = 89f) {
            this.sensitivityDegreesPerPixel = sensitivityDegreesPerPixel;
            this.minPitchDegrees = minPitchDegrees;
            this.maxPitchDegrees = maxPitchDegrees;
        }

        public void apply(Vector2 mouseDelta) {
            yawDegrees += mouseDelta.X * sensitivityDegreesPerPixel;
            pitchDegrees -= mouseDelta.Y * sensitivityDegreesPerPixel;
            pitchDegrees = Math.Clamp(pitchDegrees, minPitchDegrees, maxPitchDegrees);
        }

        public float getYawDegrees() {
            return yawDegrees;
        }

        public float getPitchDegrees() {
            return pitchDegrees;
        }
    }
}
