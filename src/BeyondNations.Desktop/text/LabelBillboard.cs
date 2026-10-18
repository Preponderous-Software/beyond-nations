using System;
using System.Numerics;

namespace beyondnations.desktop.text {

    /**
    * The maths that turns a flat run of glyph quads into quads standing in the
    * world, facing whoever is looking at them.
    *
    * None of this touches OpenGL, on purpose. Billboarding is the part of text
    * rendering that is easy to get subtly wrong -- a sign flip leaves every
    * nametag mirrored, and the mistake is invisible on a name like "MOM" -- so
    * it lives here where it can be asserted against rather than eyeballed.
    *
    * The camera basis is recovered from the view matrix rather than being
    * passed in alongside it. #219 owns the camera and is replacing the
    * placeholder; taking only a view matrix means nothing here has to know
    * which camera produced it, or be changed when that camera does.
    */
    public static class LabelBillboard {

        /**
        * The camera's right axis in world space.
        *
        * A view matrix is the inverse of the camera's world transform, and for
        * a rigid transform the inverse of the rotation is its transpose. So the
        * camera's world-space right axis, which the view matrix stores as its
        * first column, reads back out of the first row of the transpose: the
        * (M11, M21, M31) entries.
        */
        public static Vector3 getRight(Matrix4x4 view) {
            return new Vector3(view.M11, view.M21, view.M31);
        }

        /**
        * The camera's up axis in world space, by the same argument.
        */
        public static Vector3 getUp(Matrix4x4 view) {
            return new Vector3(view.M12, view.M22, view.M32);
        }

        /**
        * The direction the camera is looking, in world space.
        *
        * The third column of a view matrix is the camera's backward axis -- it
        * points from the target towards the eye -- so looking forward is its
        * negation.
        */
        public static Vector3 getForward(Matrix4x4 view) {
            return new Vector3(-view.M13, -view.M23, -view.M33);
        }

        /**
        * Where the camera is, in world space.
        *
        * Inverting is used rather than the -dot(axis, eye) entries, so this
        * stays correct for any view matrix rather than only for one built by
        * Matrix4x4.CreateLookAt. If the matrix is singular -- which a real view
        * matrix never is -- the origin is returned instead of throwing, since a
        * frame that draws the labels in the wrong place is a better failure
        * than a frame that does not draw at all.
        */
        public static Vector3 getEyePosition(Matrix4x4 view) {
            Matrix4x4 inverse;
            if (!Matrix4x4.Invert(view, out inverse)) {
                return Vector3.Zero;
            }
            return inverse.Translation;
        }

        /**
        * Places one corner of a glyph quad in the world.
        *
        * The glyph arrives in pen space: x runs right across the line and y
        * runs *down* it, because that is how every text layout engine measures.
        * The world's up axis runs the other way, hence the negation on y.
        *
        * anchor is where the bottom centre of the whole label should sit.
        */
        public static Vector3 toWorld(
                Vector3 anchor,
                Vector3 right,
                Vector3 up,
                float penX,
                float penY,
                float worldUnitsPerPixel) {
            return anchor
                + right * (penX * worldUnitsPerPixel)
                - up * (penY * worldUnitsPerPixel);
        }

        /**
        * How far to shift a label in pen space so that it ends up centred over
        * its anchor and sitting above it rather than hanging off it.
        *
        * measuredSize is what the font reports for the whole string, including
        * every line of a multi-line nametag.
        */
        public static Vector2 getCenteringOffset(Vector2 measuredSize) {
            return new Vector2(-measuredSize.X * 0.5f, -measuredSize.Y);
        }
    }
}
