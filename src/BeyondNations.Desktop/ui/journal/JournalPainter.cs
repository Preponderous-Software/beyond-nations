using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui.journal {

    /**
    * Draws the Journal's pictures, rings and badges onto an ImGui draw list.
    * Only fixed-signature draw-list calls (no printf-style text), so it runs
    * in the browser build too (docs/WEB.md).
    */
    public static class JournalPainter {

        /** Draws icon in a size x size square at topLeft; alpha below 1 dims it (a goal still ahead). */
        public static void icon(ImDrawListPtr drawList, JournalIcon icon, Vector2 topLeft, float size, float alpha = 1f) {
            float k = size / JournalIcons.Box;
            foreach (JournalIcons.Shape shape in JournalIcons.picture(icon)) {
                Vector4 colour = shape.colour;
                colour.W *= alpha;
                uint col = ImGui.GetColorU32(colour);
                float[] v = shape.v;
                switch (shape.kind) {
                    case JournalIcons.Kind.Circle:
                        drawList.AddCircleFilled(at(topLeft, k, v[0], v[1]), v[2] * k, col, 0);
                        break;
                    case JournalIcons.Kind.Ring:
                        drawList.AddCircle(at(topLeft, k, v[0], v[1]), v[2] * k, col, 0, System.MathF.Max(1f, v[3] * k));
                        break;
                    case JournalIcons.Kind.Rect:
                        drawList.AddRectFilled(at(topLeft, k, v[0], v[1]), at(topLeft, k, v[2], v[3]), col, v[4] * k);
                        break;
                    case JournalIcons.Kind.Triangle:
                        drawList.AddTriangleFilled(at(topLeft, k, v[0], v[1]), at(topLeft, k, v[2], v[3]), at(topLeft, k, v[4], v[5]), col);
                        break;
                    case JournalIcons.Kind.Quad:
                        drawList.AddQuadFilled(at(topLeft, k, v[0], v[1]), at(topLeft, k, v[2], v[3]), at(topLeft, k, v[4], v[5]), at(topLeft, k, v[6], v[7]), col);
                        break;
                    case JournalIcons.Kind.Line:
                        drawList.AddLine(at(topLeft, k, v[0], v[1]), at(topLeft, k, v[2], v[3]), col, System.MathF.Max(1f, v[4] * k));
                        break;
                }
            }
        }

        private static Vector2 at(Vector2 topLeft, float k, float x, float y) {
            return new Vector2(topLeft.X + x * k, topLeft.Y + y * k);
        }

        /** A progress ring: a dim full circle and, over it, an arc from the top round clockwise for fraction. */
        public static void ring(ImDrawListPtr drawList, Vector2 centre, float radius, float thickness, float fraction, Vector4 colour) {
            drawList.AddCircle(centre, radius, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.14f)), 0, thickness);
            fraction = System.Math.Clamp(fraction, 0f, 1f);
            if (fraction <= 0f) {
                return;
            }
            float start = -System.MathF.PI / 2f;
            float end = start + fraction * System.MathF.PI * 2f;
            int segments = System.Math.Max(8, (int) (48 * fraction));
            drawList.PathArcTo(centre, radius, start, end, segments);
            drawList.PathStroke(ImGui.GetColorU32(colour), ImDrawFlags.None, thickness);
        }

        /** A bar of fraction across a rounded track. */
        public static void bar(ImDrawListPtr drawList, Vector2 min, Vector2 max, float fraction, Vector4 colour) {
            float round = (max.Y - min.Y) / 2f;
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.12f)), round);
            fraction = System.Math.Clamp(fraction, 0f, 1f);
            if (fraction > 0f) {
                drawList.AddRectFilled(min, new Vector2(min.X + (max.X - min.X) * fraction, max.Y), ImGui.GetColorU32(colour), round);
            }
        }

        /**
        * The vine over the Goals page: a stem across the strip with one leaf
        * per goal of the first arc, green once reached and a bud before.
        */
        public static void vine(ImDrawListPtr drawList, Vector2 min, Vector2 max, int done, int total) {
            if (total <= 0) {
                return;
            }
            float midY = (min.Y + max.Y) / 2f;
            float leaf = System.MathF.Min((max.Y - min.Y) * 0.32f, (max.X - min.X) / (total * 2.4f));
            drawList.AddLine(new Vector2(min.X, midY), new Vector2(max.X, midY), ImGui.GetColorU32(JournalIcons.LeafDark), System.MathF.Max(2f, leaf * 0.35f));
            for (int i = 0; i < total; i++) {
                float x = min.X + (max.X - min.X) * (i + 0.5f) / total;
                bool up = i % 2 == 0;
                Vector2 centre = new Vector2(x, midY + (up ? -leaf * 0.7f : leaf * 0.7f));
                if (i < done) {
                    drawList.AddCircleFilled(centre, leaf, ImGui.GetColorU32(JournalIcons.Leaf), 0);
                    drawList.AddLine(new Vector2(x, midY), centre, ImGui.GetColorU32(JournalIcons.LeafDark), 1.5f);
                }
                else {
                    drawList.AddCircleFilled(new Vector2(x, midY), leaf * 0.38f, ImGui.GetColorU32(new Vector4(0.55f, 0.6f, 0.5f, 0.8f)), 0);
                }
            }
        }

        /** Text at an exact pixel size, with a soft shadow; left-aligned at topLeft. */
        public static void text(ImDrawListPtr drawList, string value, Vector2 topLeft, float size, Vector4 colour) {
            if (string.IsNullOrEmpty(value)) {
                return;
            }
            ImFontPtr font = ImGui.GetFont();
            drawList.AddText(font, size, topLeft + new Vector2(1f, 1f), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.6f * colour.W)), value);
            drawList.AddText(font, size, topLeft, ImGui.GetColorU32(colour), value);
        }

        /** How wide text is at a pixel size. */
        public static float measure(string text, float size) {
            if (string.IsNullOrEmpty(text)) {
                return 0f;
            }
            return ImGui.CalcTextSize(text).X * size / ImGui.GetFontSize();
        }

        /** text centred on centreX at a pixel size. */
        public static void textCentred(ImDrawListPtr drawList, string value, float centreX, float y, float size, Vector4 colour) {
            text(drawList, value, new Vector2(centreX - measure(value, size) / 2f, y), size, colour);
        }
    }
}
