using System;
using System.Numerics;

namespace beyondnations.desktop.render {

    /**
    * The camera the host draws through, and the replacement for the
    * UnityEngine.Camera the player used to create.
    *
    * The Unity arrangement was a single GameObject named "Camera", parented to
    * the player, sitting at the local offset (0, 5, -10) and pointed back at
    * whatever it was parented to. Because it was a child, it inherited the
    * player's yaw, so it always trailed the player at ten units behind and five
    * above. That is reproduced here exactly: the offset is rotated by the
    * player's yaw and the view looks back at the player. Nothing about the
    * framing changes, only where the arithmetic happens.
    *
    * The one property Unity code ever changed at runtime was farClipPlane,
    * driven by Page Up and Page Down. That value lives on Player, which clamps
    * it; this class only reads it and uses it as the far plane, so there is one
    * definition of the bounds and it is not this one.
    *
    * There is deliberately no GL here. Everything is System.Numerics, so the
    * matrices and the frustum they feed can be tested without a window.
    */
    public class PlayerCamera {

        /**
        * The Unity local offset, verbatim: ten units back along the player's
        * forward axis and five above.
        */
        public static readonly Vector3 ThirdPersonOffset = new Vector3(0f, 5f, -10f);

        /**
        * How far above the player's origin the eye sits in first person. The
        * player is a capsule of unit scale whose position is its centre, so
        * this is roughly head height.
        */
        public const float FirstPersonEyeHeight = 0.8f;

        /**
        * Third-person pitch is bounded more tightly than the look itself.
        * The offset already sits about 27 degrees above the target, so the
        * mouse only has to swing another 63 before the eye is directly
        * overhead and the up vector stops being usable.
        */
        private const float MinThirdPersonPitchDegrees = -50f;
        private const float MaxThirdPersonPitchDegrees = 50f;

        private const float DegreesToRadians = (float) Math.PI / 180f;

        private float fieldOfViewDegrees = 60f;
        private float nearPlane = 0.1f;
        private float farPlane = 200f;
        private float aspectRatio = 16f / 9f;

        private CameraMode mode = CameraMode.ThirdPerson;

        private Vector3 eyePosition = ThirdPersonOffset;
        private Vector3 targetPosition = Vector3.Zero;
        private Vector3 forward = Vector3.UnitZ;

        public CameraMode getMode() {
            return mode;
        }

        public void setMode(CameraMode mode) {
            this.mode = mode;
        }

        /**
        * What V does (#173): the view is a mode on one camera, so switching is
        * this call and nothing else. The host additionally drops the player
        * from the snapshot while the eye is on top of them.
        */
        public void toggleMode() {
            mode = mode == CameraMode.ThirdPerson ? CameraMode.FirstPerson : CameraMode.ThirdPerson;
        }

        public void setAspectRatio(int width, int height) {
            if (width <= 0 || height <= 0) {
                return;
            }
            aspectRatio = (float) width / height;
        }

        public float getAspectRatio() {
            return aspectRatio;
        }

        public float getFieldOfViewDegrees() {
            return fieldOfViewDegrees;
        }

        public float getNearPlane() {
            return nearPlane;
        }

        /**
        * The far plane, which is the render distance and nothing else. Page Up
        * and Page Down move it because they move Player.getRenderDistance,
        * which the host feeds in here every frame.
        */
        public float getRenderDistance() {
            return farPlane;
        }

        public void setRenderDistance(float renderDistance) {
            // Only a degenerate projection is refused here. The playable bounds
            // are Player's, and duplicating them would give the game two
            // answers to the same question.
            if (renderDistance <= nearPlane) {
                return;
            }
            farPlane = renderDistance;
        }

        public Vector3 getEyePosition() {
            return eyePosition;
        }

        public Vector3 getTargetPosition() {
            return targetPosition;
        }

        /**
        * The direction the camera looks in, which in third person is from the
        * trailing eye towards the player rather than the player's own facing.
        */
        public Vector3 getForward() {
            return forward;
        }

        /**
        * Places the camera for this frame.
        *
        * playerYawRadians is Player.getYaw, still keyboard-driven exactly as the
        * README documents. lookYawDegrees and lookPitchDegrees come from
        * MouseLook and are an offset on top of it, so a build with no mouse
        * movement is framed identically to the Unity one.
        */
        public void follow(Vector3 playerPosition, float playerYawRadians, float lookYawDegrees, float lookPitchDegrees) {
            float azimuth = playerYawRadians + lookYawDegrees * DegreesToRadians;

            if (mode == CameraMode.FirstPerson) {
                followFirstPerson(playerPosition, azimuth, lookPitchDegrees * DegreesToRadians);
                return;
            }
            followThirdPerson(playerPosition, azimuth, lookPitchDegrees);
        }

        private void followThirdPerson(Vector3 playerPosition, float azimuth, float lookPitchDegrees) {
            float pitchDegrees = Math.Clamp(lookPitchDegrees, MinThirdPersonPitchDegrees, MaxThirdPersonPitchDegrees);

            // Looking up has to swing the eye down and back, not up, or the
            // camera would climb towards the target as the view rose.
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(azimuth, -pitchDegrees * DegreesToRadians, 0f);

            targetPosition = playerPosition;
            eyePosition = playerPosition + Vector3.Transform(ThirdPersonOffset, rotation);
            forward = normalizeOrDefault(targetPosition - eyePosition, Vector3.UnitZ);
        }

        private void followFirstPerson(Vector3 playerPosition, float azimuth, float pitchRadians) {
            float cosPitch = MathF.Cos(pitchRadians);
            forward = new Vector3(
                MathF.Sin(azimuth) * cosPitch,
                MathF.Sin(pitchRadians),
                MathF.Cos(azimuth) * cosPitch);
            forward = normalizeOrDefault(forward, Vector3.UnitZ);

            eyePosition = playerPosition + new Vector3(0f, FirstPersonEyeHeight, 0f);
            targetPosition = eyePosition + forward;
        }

        public Matrix4x4 getViewMatrix() {
            // Straight up is unusable as an up vector when the view is looking
            // along it. The pitch clamps make that unreachable, but a view
            // matrix full of NaN is not worth risking on a clamp alone.
            Vector3 up = MathF.Abs(forward.Y) > 0.999f ? Vector3.UnitZ : Vector3.UnitY;
            return Matrix4x4.CreateLookAt(eyePosition, eyePosition + forward, up);
        }

        public Matrix4x4 getProjectionMatrix() {
            return Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfViewDegrees * DegreesToRadians,
                aspectRatio,
                nearPlane,
                farPlane);
        }

        /**
        * View then projection, which is what the frustum is extracted from.
        * System.Numerics multiplies row-vector first, so the order reads
        * backwards compared to the GLSL.
        */
        public Matrix4x4 getViewProjectionMatrix() {
            return getViewMatrix() * getProjectionMatrix();
        }

        private static Vector3 normalizeOrDefault(Vector3 value, Vector3 fallback) {
            float lengthSquared = value.LengthSquared();
            if (lengthSquared < 1e-12f) {
                return fallback;
            }
            return value / MathF.Sqrt(lengthSquared);
        }
    }
}
