using System;
using System.Numerics;

namespace beyondnations.desktop.render {

    /**
    * The six planes of the camera's view volume, pulled straight out of the
    * view-projection matrix.
    *
    * Unity did this invisibly: a Renderer whose bounds fell outside the camera
    * frustum was simply never drawn, and the far clip plane the render distance
    * controlled was one of the six planes doing it. Nothing replaced that when
    * the engine went, so without this every ground tile in a two-hundred-chunk
    * world would be uploaded and drawn every frame regardless of whether the
    * camera pointed anywhere near it.
    *
    * The extraction is the standard one: for a clip-space point p = v * M, the
    * condition -w <= x <= w (and so on) rearranges into six plane equations
    * whose coefficients are sums and differences of the columns of M. The
    * matrices here come from System.Numerics, which maps depth to [0, 1]
    * rather than [-1, 1], so the near plane is the depth column on its own
    * rather than the depth column plus w.
    *
    * The planes are stored as Vector4 (xyz = unit normal pointing into the
    * volume, w = distance), and update() reuses the same array, so a frame that
    * culls costs no allocation.
    */
    public class ViewFrustum {

        public const int PlaneCount = 6;

        public const int Left = 0;
        public const int Right = 1;
        public const int Bottom = 2;
        public const int Top = 3;
        public const int Near = 4;
        public const int Far = 5;

        private readonly Vector4[] planes = new Vector4[PlaneCount];

        public void update(Matrix4x4 viewProjection) {
            // The columns of a row-vector matrix: clip.X is the dot of the
            // point with column one, and so on.
            Vector4 columnX = new Vector4(viewProjection.M11, viewProjection.M21, viewProjection.M31, viewProjection.M41);
            Vector4 columnY = new Vector4(viewProjection.M12, viewProjection.M22, viewProjection.M32, viewProjection.M42);
            Vector4 columnZ = new Vector4(viewProjection.M13, viewProjection.M23, viewProjection.M33, viewProjection.M43);
            Vector4 columnW = new Vector4(viewProjection.M14, viewProjection.M24, viewProjection.M34, viewProjection.M44);

            planes[Left] = normalize(columnW + columnX);
            planes[Right] = normalize(columnW - columnX);
            planes[Bottom] = normalize(columnW + columnY);
            planes[Top] = normalize(columnW - columnY);
            planes[Near] = normalize(columnZ);
            planes[Far] = normalize(columnW - columnZ);
        }

        public Vector4 getPlane(int index) {
            return planes[index];
        }

        /**
        * The signed distance from a point to one plane. Positive is inside.
        */
        public float distanceTo(int planeIndex, Vector3 point) {
            Vector4 plane = planes[planeIndex];
            return plane.X * point.X + plane.Y * point.Y + plane.Z * point.Z + plane.W;
        }

        public bool containsPoint(Vector3 point) {
            return containsSphere(point, 0f);
        }

        /**
        * Whether any part of a sphere is inside the volume.
        *
        * This is the cheap conservative test every frustum culler uses: a
        * sphere entirely behind one plane cannot be in the volume, and anything
        * else is treated as visible. It can keep a thing that is in fact just
        * outside a corner, which costs one wasted instance; it can never drop
        * something that should have been drawn, which would be a hole in the
        * world.
        */
        public bool containsSphere(Vector3 center, float radius) {
            for (int i = 0; i < PlaneCount; i++) {
                if (distanceTo(i, center) < -radius) {
                    return false;
                }
            }
            return true;
        }

        private static Vector4 normalize(Vector4 plane) {
            float length = MathF.Sqrt(plane.X * plane.X + plane.Y * plane.Y + plane.Z * plane.Z);
            if (length < 1e-12f) {
                return plane;
            }
            return plane / length;
        }
    }
}
