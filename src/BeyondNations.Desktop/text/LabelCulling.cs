using System;
using System.Numerics;

namespace beyondnations.desktop.text {

    /**
    * Decides which world-space labels are worth drawing, and how solid each one
    * should be.
    *
    * Three things are ruled out here. A label behind the camera, which would
    * otherwise be projected back through the eye and appear mirrored somewhere
    * it has no business being. A label outside the sides of the frustum, which
    * costs a quad per glyph for pixels nobody sees. And a label further away
    * than the cull distance, which is the one the issue actually asks for:
    * nametags are meant to disappear with the thing that owns them, rather than
    * accumulating into an unreadable smear across the horizon.
    *
    * Between the fade distance and the cull distance the alpha ramps to zero,
    * so a label thins out instead of vanishing between one frame and the next.
    *
    * There is no OpenGL in this file. All of it is decided from a position, a
    * view matrix and a projection matrix, which is what makes it testable.
    */
    public class LabelCulling {

        /**
        * Beyond this, nothing is drawn. The world is built from locations
        * fifteen units across and the default render distance is two hundred,
        * so a nametag that survived to the render distance would be a couple of
        * pixels tall and would still cost a draw.
        */
        private float cullDistance = 140f;

        /**
        * Where fading starts. Must be below the cull distance for the ramp to
        * have anywhere to happen.
        */
        private float fadeDistance = 90f;

        /**
        * How far outside the frustum a label may stray before it is dropped.
        * One is exactly the frustum edge; a little over one keeps a nametag
        * whose anchor has just left the screen from popping out while half its
        * glyphs are still visible.
        */
        private float frustumSlack = 1.3f;

        public float getCullDistance() { return cullDistance; }
        public float getFadeDistance() { return fadeDistance; }
        public float getFrustumSlack() { return frustumSlack; }

        public void setCullDistance(float cullDistance) { this.cullDistance = cullDistance; }
        public void setFadeDistance(float fadeDistance) { this.fadeDistance = fadeDistance; }
        public void setFrustumSlack(float frustumSlack) { this.frustumSlack = frustumSlack; }

        /**
        * Whether this label should be drawn, and at what opacity.
        *
        * alpha is only meaningful when this returns true; it is set to zero
        * otherwise so a caller that ignores the return value draws nothing
        * visible rather than something wrong.
        */
        public bool tryAccept(
                Vector3 labelPosition,
                Matrix4x4 view,
                Matrix4x4 projection,
                out float alpha) {
            alpha = 0f;

            Vector3 eye = LabelBillboard.getEyePosition(view);
            Vector3 forward = LabelBillboard.getForward(view);

            Vector3 toLabel = labelPosition - eye;

            // Strictly behind the eye plane, or exactly on it, where the
            // projection divides by zero.
            float alongView = Vector3.Dot(toLabel, forward);
            if (alongView <= 0f) {
                return false;
            }

            float distance = toLabel.Length();
            if (distance >= cullDistance) {
                return false;
            }

            if (!isInsideFrustumSides(labelPosition, view, projection)) {
                return false;
            }

            alpha = getAlphaForDistance(distance);
            return alpha > 0f;
        }

        /**
        * The fade ramp on its own. Fully opaque up to the fade distance, then
        * linearly down to nothing at the cull distance.
        */
        public float getAlphaForDistance(float distance) {
            if (distance >= cullDistance) {
                return 0f;
            }
            if (distance <= fadeDistance) {
                return 1f;
            }
            float span = cullDistance - fadeDistance;
            if (span <= 0f) {
                return 1f;
            }
            return 1f - ((distance - fadeDistance) / span);
        }

        /**
        * Whether the label's anchor falls within the left, right, top and
        * bottom planes of the frustum, widened by the slack.
        *
        * The near and far planes are deliberately not tested here: distance and
        * the behind-the-eye test already cover them, and testing clip-space z
        * as well would make a label pop out at the far plane at a different
        * moment than the fade expects.
        */
        public bool isInsideFrustumSides(Vector3 labelPosition, Matrix4x4 view, Matrix4x4 projection) {
            Vector4 clip = Vector4.Transform(new Vector4(labelPosition, 1f), view * projection);
            if (clip.W <= 0f) {
                return false;
            }
            float limit = clip.W * frustumSlack;
            return Math.Abs(clip.X) <= limit && Math.Abs(clip.Y) <= limit;
        }
    }
}
