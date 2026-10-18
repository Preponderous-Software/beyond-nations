using System;
using System.Numerics;
using beyondnations;

namespace beyondnations.desktop.render {

    /**
    * Decides what is worth drawing, before anything is written into an
    * instance buffer.
    *
    * Two tests, cheapest first. Distance: anything further from the eye than
    * the render distance is dropped, which is the rule Page Up and Page Down
    * have always expressed and the one a player can feel. Frustum: anything
    * entirely outside the view volume is dropped, which is most of the world
    * most of the time, since a sixty-degree camera can see a fraction of a
    * circle.
    *
    * The distance test is redundant in principle -- the far plane is the render
    * distance, so the frustum would reject the same things -- but it is one
    * subtraction and a compare against six plane dots, and it rejects the bulk
    * of a large world before the more expensive test runs.
    *
    * Everything here is plain arithmetic on plain vectors: no GL, no snapshot,
    * no renderer, so the decision itself can be tested against known points.
    */
    public class RenderCuller {

        private readonly ViewFrustum frustum = new ViewFrustum();

        private Vector3 eyePosition;
        private float renderDistance;
        private bool enabled = true;

        private int considered;
        private int culledByDistance;
        private int culledByFrustum;

        public bool isEnabled() {
            return enabled;
        }

        /**
        * Turning this off draws everything. It exists so that a run with
        * culling and a run without it can be compared, which is the only
        * honest way to claim culling does anything.
        */
        public void setEnabled(bool enabled) {
            this.enabled = enabled;
        }

        public ViewFrustum getFrustum() {
            return frustum;
        }

        public Vector3 getEyePosition() {
            return eyePosition;
        }

        public float getRenderDistance() {
            return renderDistance;
        }

        public int getConsidered() {
            return considered;
        }

        public int getCulledByDistance() {
            return culledByDistance;
        }

        public int getCulledByFrustum() {
            return culledByFrustum;
        }

        public int getCulled() {
            return culledByDistance + culledByFrustum;
        }

        public void beginFrame(Vector3 eyePosition, float renderDistance, Matrix4x4 viewProjection) {
            this.eyePosition = eyePosition;
            this.renderDistance = renderDistance;
            frustum.update(viewProjection);
            considered = 0;
            culledByDistance = 0;
            culledByFrustum = 0;
        }

        /**
        * Convenience for the host, which has a camera rather than three loose
        * values.
        */
        public void beginFrame(PlayerCamera camera) {
            beginFrame(camera.getEyePosition(), camera.getRenderDistance(), camera.getViewProjectionMatrix());
        }

        /**
        * The radius of the sphere that contains a primitive of this scale.
        *
        * Scale is the full extent of the primitive, as Unity's localScale was,
        * and the position is its centre, so half the diagonal reaches every
        * corner. A ground tile is fifteen units across and would be culled at
        * its centre while a corner was still on screen if this were any
        * smaller.
        */
        public static float boundingRadius(Vector3 scale) {
            return 0.5f * scale.Length();
        }

        public bool shouldDraw(Vector3 position, Vector3 scale) {
            if (!enabled) {
                return true;
            }

            considered++;
            float radius = boundingRadius(scale);

            float reach = renderDistance + radius;
            if (Vector3.DistanceSquared(eyePosition, position) > reach * reach) {
                culledByDistance++;
                return false;
            }

            if (!frustum.containsSphere(position, radius)) {
                culledByFrustum++;
                return false;
            }

            return true;
        }

        public bool shouldDraw(ref RenderItem item) {
            return shouldDraw(item.position, item.scale);
        }
    }
}
