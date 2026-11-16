using System;
using System.Numerics;

namespace beyondnations.desktop.text {

    /**
    * Where a large label (a settlement's name, Appearance.getLabelScale
    * above 1) goes on screen. Standing in a town, its name floats high
    * over the plaza: it ran off the top of the screen and sat under the
    * HUD's status line and chronicle strip (the README's town and winter
    * shots). Such a label keeps its top below the HUD's top band, sliding
    * down the screen when it would rise into it. Where the band reaches
    * further down than MaxTopReserve (a phone held upright, whose HUD
    * stacks its boxes, status line, chronicle and Season box down most of
    * the screen) there is no clear place for it, and a name that would
    * rise into the band is left out instead. Ordinary nametags stay on
    * whoever they name.
    *
    * No OpenGL here: the drop is worked out from the view and projection
    * alone, so the desktop and browser renderers share it and the tests
    * can check it.
    */
    public static class LabelPlacement {

        /** A large label is never pushed further down the screen than this; past it, it is left out. */
        public const float MaxTopReserve = 0.45f;

        /** Room kept above a label even when no HUD is drawn (a flyover), as a fraction of the height. */
        public const float TopMargin = 0.02f;

        // the fraction of the screen's height the HUD covers from the top,
        // in the middle (WorldHud: the status line, warnings and chronicle
        // strip), as last drawn; 0 when no HUD is up
        private static float topReserve;

        public static float getTopReserve() { return topReserve; }

        /**
        * Set by the HUD every frame it is drawn, from the bottom of its top
        * band over the screen's height; 1 (the whole screen) by the UI host
        * while another screen is drawn over the world, whose title the
        * name overlapped (the settlement screen's "Camp of Farmwood").
        */
        public static void setTopReserve(float fraction) {
            topReserve = float.IsNaN(fraction) ? 0f : Math.Clamp(fraction, 0f, 1f);
        }

        /** Whether a label of this scale keeps clear of the top band (large ones only). */
        public static bool keepsClearOfTop(float labelScale) {
            return labelScale > 1f;
        }

        /**
        * Where a large label goes: false when it is left out (it would rise
        * into a band deeper than MaxTopReserve), else true with drop, how
        * far to lower its anchor (getDrop).
        */
        public static bool tryPlace(Vector3 anchor, float height, Matrix4x4 view, Matrix4x4 projection, float topFraction, out float drop) {
            drop = getDrop(anchor, height, view, projection, Math.Min(topFraction, MaxTopReserve));
            if (topFraction > MaxTopReserve && getDrop(anchor, height, view, projection, topFraction) > 0f) {
                drop = 0f;
                return false;
            }
            return true;
        }

        /**
        * How far, in world units along the camera's up axis, to lower a
        * label's anchor (its bottom centre) so that its top, height world
        * units above the anchor, is at or below the top band
        * (topFraction of the screen's height, plus TopMargin). 0 when it is
        * already clear, or behind the camera.
        *
        * Sliding along the camera's up axis keeps the label's depth, so
        * the shift is linear: one world unit at depth w moves it
        * M22 / w in normalised device coordinates.
        */
        public static float getDrop(Vector3 anchor, float height, Matrix4x4 view, Matrix4x4 projection, float topFraction) {
            if (height <= 0f || projection.M22 <= 0f) {
                return 0f;
            }
            Matrix4x4 viewProjection = view * projection;
            Vector3 up = LabelBillboard.getUp(view);
            Vector4 top = Vector4.Transform(new Vector4(anchor + up * height, 1f), viewProjection);
            if (top.W <= 0.0001f) {
                return 0f;
            }
            float limit = 1f - 2f * (Math.Clamp(topFraction, 0f, 1f) + TopMargin);
            float topY = top.Y / top.W;
            if (topY <= limit) {
                return 0f;
            }
            return (topY - limit) * top.W / projection.M22;
        }
    }
}
