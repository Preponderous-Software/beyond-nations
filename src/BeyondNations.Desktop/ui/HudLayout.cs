using System;
using System.Collections.Generic;

namespace beyondnations.desktop.ui {

    /** A rectangle on the screen, in the HUD's pixels; the core of HudLayout's geometry. */
    public struct HudRect {
        public float X;
        public float Y;
        public float W;
        public float H;

        public HudRect(float x, float y, float w, float h) {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public float Right { get { return X + W; } }
        public float Bottom { get { return Y + H; } }

        /** True when the two share some area (touching edges do not count). */
        public bool overlaps(HudRect other) {
            return X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
        }

        public bool contains(float x, float y) {
            return x >= X && x < Right && y >= Y && y < Bottom;
        }

        public bool isEmpty() {
            return W <= 0f || H <= 0f;
        }

        public override string ToString() {
            return "(" + X + ", " + Y + ", " + W + "x" + H + ")";
        }
    }

    /** What a HUD chip or meter says needs a look: nothing, something new, a warning or a danger. */
    public enum HudBadge { None, New, Warning, Danger }

    /**
    * The pure geometry that keeps the HUD's boxes and chips off what they
    * must never cover: the command bar and the touch controls (the no-go
    * rects WorldHud hands HudPanels each frame). No ImGui here, so all of it
    * is checked by unit tests (TestHudLayout).
    */
    public static class HudLayout {
        /** The box moved, not resized, so it lies on the screen; a box larger than the screen keeps its top left corner on it. */
        public static HudRect clampToScreen(HudRect rect, float screenWidth, float screenHeight) {
            rect.X = Math.Max(0f, Math.Min(rect.X, screenWidth - rect.W));
            rect.Y = Math.Max(0f, Math.Min(rect.Y, screenHeight - rect.H));
            return rect;
        }

        /**
        * The box moved the least distance that keeps it on the screen and off
        * every obstacle (the command bar, the touch controls, boxes already
        * placed): out above, below, left or right of one obstacle and, when
        * that lands on another, of that one in turn. With no such place, the
        * candidate overlapping the obstacles least, so a box too big for a
        * tiny window at least does not sit squarely on the bar.
        */
        public static HudRect avoid(HudRect rect, float screenWidth, float screenHeight, IReadOnlyList<HudRect> obstacles) {
            rect = clampToScreen(rect, screenWidth, screenHeight);
            if (obstacles == null || clearOf(rect, obstacles)) {
                return rect;
            }
            List<HudRect> candidates = new List<HudRect>();
            List<HudRect> firstStep = pushOut(rect, screenWidth, screenHeight, obstacles);
            candidates.AddRange(firstStep);
            foreach (HudRect step in firstStep) {
                if (!clearOf(step, obstacles)) {
                    candidates.AddRange(pushOut(step, screenWidth, screenHeight, obstacles));
                }
            }
            HudRect best = rect;
            float bestDistance = float.MaxValue;
            bool found = false;
            foreach (HudRect candidate in candidates) {
                if (!clearOf(candidate, obstacles)) {
                    continue;
                }
                float distance = Math.Abs(candidate.X - rect.X) + Math.Abs(candidate.Y - rect.Y);
                if (distance < bestDistance) {
                    best = candidate;
                    bestDistance = distance;
                    found = true;
                }
            }
            if (found) {
                return best;
            }
            float leastOverlap = overlapArea(rect, obstacles);
            foreach (HudRect candidate in candidates) {
                float overlap = overlapArea(candidate, obstacles);
                if (overlap < leastOverlap) {
                    best = candidate;
                    leastOverlap = overlap;
                }
            }
            return best;
        }

        private static List<HudRect> pushOut(HudRect rect, float screenWidth, float screenHeight, IReadOnlyList<HudRect> obstacles) {
            List<HudRect> moved = new List<HudRect>();
            foreach (HudRect obstacle in obstacles) {
                if (obstacle.isEmpty() || !rect.overlaps(obstacle)) {
                    continue;
                }
                moved.Add(clampToScreen(new HudRect(rect.X, obstacle.Y - rect.H, rect.W, rect.H), screenWidth, screenHeight));
                moved.Add(clampToScreen(new HudRect(rect.X, obstacle.Bottom, rect.W, rect.H), screenWidth, screenHeight));
                moved.Add(clampToScreen(new HudRect(obstacle.X - rect.W, rect.Y, rect.W, rect.H), screenWidth, screenHeight));
                moved.Add(clampToScreen(new HudRect(obstacle.Right, rect.Y, rect.W, rect.H), screenWidth, screenHeight));
            }
            return moved;
        }

        /** True when rect overlaps none of the obstacles. */
        public static bool clearOf(HudRect rect, IReadOnlyList<HudRect> obstacles) {
            foreach (HudRect obstacle in obstacles) {
                if (!obstacle.isEmpty() && rect.overlaps(obstacle)) {
                    return false;
                }
            }
            return true;
        }

        private static float overlapArea(HudRect rect, IReadOnlyList<HudRect> obstacles) {
            float area = 0f;
            foreach (HudRect obstacle in obstacles) {
                float w = Math.Min(rect.Right, obstacle.Right) - Math.Max(rect.X, obstacle.X);
                float h = Math.Min(rect.Bottom, obstacle.Bottom) - Math.Max(rect.Y, obstacle.Y);
                if (w > 0f && h > 0f) {
                    area += w * h;
                }
            }
            return area;
        }
    }

}
