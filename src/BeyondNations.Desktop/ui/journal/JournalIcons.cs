using System.Collections.Generic;
using System.Numerics;

namespace beyondnations.desktop.ui.journal {

    /** The Journal's small pictures: one per tab, item, goal, era step and kind of chronicle entry. */
    public enum JournalIcon {
        Target,
        Scroll,
        Hourglass,
        Bag,
        Flag,
        Check,
        Lock,
        Log,
        Stone,
        Apple,
        Sapling,
        Meat,
        CookedMeat,
        Coin,
        Planks,
        Bricks,
        Axe,
        Pickaxe,
        House,
        Tower,
        Castle,
        Crown,
        Tent,
        Stall,
        People,
        Hammer,
        Star,
        Calendar,
        Leaf,
        Sun,
        Snow,
        Swords,
        Handshake,
        Shield,
        Skull,
        Cart,
        Heart,
        Warning,
        Globe,
        Feather,
        Compass
    }

    /**
    * The pictures themselves, as lists of shapes in a 32 x 32 box: drawn in
    * code from circles, rectangles, triangles, quads, lines and rings, the
    * way Primordial Rise's journal draws its badges (journal_icons.h). No
    * image files and no icon set: original shapes made for the game.
    *
    * Kept apart from drawing (JournalPainter) so a test can check every
    * icon has a picture and stays inside its box.
    */
    public static class JournalIcons {

        public enum Kind { Circle, Rect, Triangle, Quad, Line, Ring }

        /** One shape: its kind, its numbers in the 32 x 32 box and its colour. */
        public readonly struct Shape {
            public readonly Kind kind;
            public readonly float[] v;
            public readonly Vector4 colour;

            public Shape(Kind kind, Vector4 colour, params float[] v) {
                this.kind = kind;
                this.colour = colour;
                this.v = v;
            }
        }

        public const float Box = 32f;

        // the palette: warm and earthy, readable on the dark panel
        public static readonly Vector4 Gold = rgb(0xf2c94c);
        public static readonly Vector4 GoldDark = rgb(0xb8892a);
        public static readonly Vector4 Wood = rgb(0x9a6a3a);
        public static readonly Vector4 WoodDark = rgb(0x6a4422);
        public static readonly Vector4 WoodLight = rgb(0xc8955a);
        public static readonly Vector4 Grey = rgb(0x9aa3ad);
        public static readonly Vector4 GreyDark = rgb(0x5d6670);
        public static readonly Vector4 Red = rgb(0xd9533a);
        public static readonly Vector4 RedDark = rgb(0x8e2c1e);
        public static readonly Vector4 Leaf = rgb(0x5cb04c);
        public static readonly Vector4 LeafDark = rgb(0x2f6f30);
        public static readonly Vector4 Cream = rgb(0xefe4c8);
        public static readonly Vector4 Sky = rgb(0x6fb6e8);
        public static readonly Vector4 SkyDark = rgb(0x2f6f9e);
        public static readonly Vector4 White = rgb(0xf4f7ef);
        public static readonly Vector4 Ink = rgb(0x1a1f26);
        public static readonly Vector4 Pink = rgb(0xe88a7a);
        public static readonly Vector4 Amber = rgb(0xe8943a);

        private static Vector4 rgb(int hex) {
            return new Vector4(((hex >> 16) & 0xff) / 255f, ((hex >> 8) & 0xff) / 255f, (hex & 0xff) / 255f, 1f);
        }

        private static Shape C(Vector4 c, float x, float y, float r) { return new Shape(Kind.Circle, c, x, y, r); }
        private static Shape R(Vector4 c, float x0, float y0, float x1, float y1, float round = 0f) { return new Shape(Kind.Rect, c, x0, y0, x1, y1, round); }
        private static Shape T(Vector4 c, float x0, float y0, float x1, float y1, float x2, float y2) { return new Shape(Kind.Triangle, c, x0, y0, x1, y1, x2, y2); }
        private static Shape Q(Vector4 c, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3) { return new Shape(Kind.Quad, c, x0, y0, x1, y1, x2, y2, x3, y3); }
        private static Shape L(Vector4 c, float x0, float y0, float x1, float y1, float w) { return new Shape(Kind.Line, c, x0, y0, x1, y1, w); }
        private static Shape O(Vector4 c, float x, float y, float r, float w) { return new Shape(Kind.Ring, c, x, y, r, w); }

        private static readonly Dictionary<JournalIcon, Shape[]> Pictures = new Dictionary<JournalIcon, Shape[]> {
            { JournalIcon.Target, new[] { C(Red, 16, 16, 13), C(White, 16, 16, 9.5f), C(Red, 16, 16, 6), C(White, 16, 16, 2.5f) } },
            { JournalIcon.Scroll, new[] { R(Cream, 7, 6, 25, 26, 2), C(WoodLight, 7, 7, 3), C(WoodLight, 25, 25, 3),
                L(WoodDark, 11, 12, 21, 12, 1.5f), L(WoodDark, 11, 16, 21, 16, 1.5f), L(WoodDark, 11, 20, 18, 20, 1.5f) } },
            { JournalIcon.Hourglass, new[] { R(Wood, 7, 4, 25, 7, 1), R(Wood, 7, 25, 25, 28, 1),
                T(Cream, 9, 7, 23, 7, 16, 16), T(Cream, 16, 16, 9, 25, 23, 25), T(Gold, 12, 9, 20, 9, 16, 14), T(Gold, 16, 19, 11, 25, 21, 25) } },
            { JournalIcon.Bag, new[] { O(WoodDark, 16, 9, 4, 2), Q(Wood, 8, 12, 24, 12, 27, 27, 5, 27), R(WoodLight, 12, 16, 20, 18, 1) } },
            { JournalIcon.Flag, new[] { L(Grey, 8, 4, 8, 29, 2.5f), Q(Red, 9, 5, 25, 7, 22, 12, 9, 15), T(RedDark, 22, 12, 25, 7, 26, 13) } },
            { JournalIcon.Check, new[] { C(Leaf, 16, 16, 13), L(White, 9, 16, 14, 22, 3.5f), L(White, 14, 22, 24, 10, 3.5f) } },
            { JournalIcon.Lock, new[] { O(Grey, 16, 12, 6, 3), R(Grey, 8, 13, 24, 27, 2), C(Ink, 16, 19, 2.2f), R(Ink, 15, 19, 17, 24) } },
            { JournalIcon.Log, new[] { R(Wood, 4, 10, 26, 22, 2), C(WoodLight, 26, 16, 6), O(WoodDark, 26, 16, 3, 1.5f), L(WoodDark, 7, 14, 20, 14, 1.2f), L(WoodDark, 9, 18, 22, 18, 1.2f) } },
            { JournalIcon.Stone, new[] { Q(Grey, 5, 22, 10, 9, 22, 7, 27, 21), Q(GreyDark, 5, 22, 27, 21, 23, 27, 9, 27), L(White, 12, 12, 18, 10, 1.5f) } },
            { JournalIcon.Apple, new[] { C(Red, 12, 18, 8), C(Red, 20, 18, 8), C(Red, 16, 23, 6), L(WoodDark, 16, 10, 18, 5, 2), Q(Leaf, 18, 8, 24, 4, 26, 7, 20, 10), C(Pink, 12, 15, 2) } },
            { JournalIcon.Sapling, new[] { L(LeafDark, 16, 27, 16, 13, 2.5f), Q(Leaf, 16, 14, 8, 8, 6, 12, 12, 17), Q(Leaf, 16, 12, 24, 5, 27, 9, 20, 15), R(Wood, 9, 25, 23, 29, 2) } },
            { JournalIcon.Meat, new[] { C(Pink, 13, 15, 9), C(Red, 13, 15, 6), L(Cream, 19, 21, 26, 27, 4), C(Cream, 27, 26, 2.5f), C(Cream, 25, 28, 2.5f) } },
            { JournalIcon.CookedMeat, new[] { C(WoodLight, 13, 15, 9), C(Wood, 13, 15, 6), L(Cream, 19, 21, 26, 27, 4), C(Cream, 27, 26, 2.5f), C(Cream, 25, 28, 2.5f),
                L(Amber, 9, 4, 11, 1, 1.5f), L(Amber, 14, 4, 16, 1, 1.5f) } },
            { JournalIcon.Coin, new[] { C(GoldDark, 16, 17, 12), C(Gold, 16, 16, 12), O(GoldDark, 16, 16, 8.5f, 1.5f), R(GoldDark, 14.5f, 11, 17.5f, 21, 1) } },
            { JournalIcon.Planks, new[] { R(WoodLight, 4, 8, 28, 13, 1), R(Wood, 4, 14, 28, 19, 1), R(WoodLight, 4, 20, 28, 25, 1), L(WoodDark, 10, 10, 10, 11, 1), L(WoodDark, 22, 22, 22, 23, 1) } },
            { JournalIcon.Bricks, new[] { R(Red, 4, 8, 15, 14, 1), R(Red, 17, 8, 28, 14, 1), R(RedDark, 10, 16, 22, 22, 1), R(Red, 4, 16, 8, 22, 1), R(Red, 24, 16, 28, 22, 1), R(Red, 4, 24, 15, 29, 1), R(Red, 17, 24, 28, 29, 1) } },
            { JournalIcon.Axe, new[] { L(Wood, 9, 28, 21, 6, 3), Q(Grey, 18, 4, 28, 7, 27, 17, 20, 12), L(White, 24, 7, 26, 14, 1) } },
            { JournalIcon.Pickaxe, new[] { L(Wood, 9, 28, 19, 10, 3), Q(Grey, 6, 9, 16, 4, 28, 8, 17, 9), T(Grey, 6, 9, 9, 7, 4, 15), T(Grey, 28, 8, 25, 6, 30, 14) } },
            { JournalIcon.House, new[] { T(Red, 4, 15, 16, 5, 28, 15), R(Cream, 7, 15, 25, 28), R(Wood, 14, 19, 19, 28), R(Sky, 9, 18, 13, 22) } },
            { JournalIcon.Tower, new[] { R(Grey, 10, 9, 22, 29), R(Grey, 8, 5, 12, 10), R(Grey, 14, 5, 18, 10), R(Grey, 20, 5, 24, 10), R(Ink, 14, 21, 18, 29, 2), R(Ink, 15, 13, 17, 17) } },
            { JournalIcon.Castle, new[] { R(Grey, 4, 14, 28, 29), R(GreyDark, 4, 9, 10, 29), R(GreyDark, 22, 9, 28, 29), R(GreyDark, 13, 5, 19, 29),
                T(Red, 12, 5, 16, 0, 20, 5), R(Ink, 14, 22, 18, 29, 2) } },
            { JournalIcon.Crown, new[] { Q(Gold, 5, 24, 27, 24, 28, 9, 4, 9), T(Ink, 4, 9, 10, 9, 7, 17), T(Ink, 13, 9, 19, 9, 16, 15), T(Ink, 22, 9, 28, 9, 25, 17),
                C(Red, 16, 19, 2.5f), C(Sky, 9, 20, 1.8f), C(Sky, 23, 20, 1.8f) } },
            { JournalIcon.Tent, new[] { T(WoodLight, 3, 27, 16, 6, 29, 27), T(Wood, 12, 27, 16, 17, 20, 27), L(WoodDark, 16, 6, 16, 3, 1.5f) } },
            { JournalIcon.Stall, new[] { Q(Red, 4, 12, 28, 12, 26, 6, 6, 6), R(White, 8, 6, 12, 12), R(White, 16, 6, 20, 12), R(Wood, 6, 18, 26, 22), L(WoodDark, 7, 12, 7, 28, 2), L(WoodDark, 25, 12, 25, 28, 2),
                C(Red, 12, 16, 2), C(Leaf, 17, 16, 2), C(Gold, 21, 16, 2) } },
            { JournalIcon.People, new[] { C(WoodLight, 11, 10, 4.5f), R(Sky, 5, 16, 17, 28, 4), C(Cream, 21, 12, 4.5f), R(Leaf, 15, 18, 27, 29, 4) } },
            { JournalIcon.Hammer, new[] { L(Wood, 10, 28, 19, 12, 3), R(Grey, 12, 4, 28, 11, 1), R(GreyDark, 12, 4, 15, 11, 1) } },
            { JournalIcon.Star, new[] { T(Gold, 16, 3, 12, 14, 20, 14), T(Gold, 3, 13, 29, 13, 16, 21), T(Gold, 16, 18, 7, 29, 12, 14), T(Gold, 16, 18, 25, 29, 20, 14), C(Gold, 16, 16, 5) } },
            { JournalIcon.Calendar, new[] { R(White, 5, 8, 27, 28, 2), R(Red, 5, 8, 27, 13, 2), L(GreyDark, 11, 5, 11, 10, 2), L(GreyDark, 21, 5, 21, 10, 2),
                R(GreyDark, 9, 16, 12, 19), R(GreyDark, 15, 16, 18, 19), R(GreyDark, 21, 16, 24, 19), R(Red, 9, 22, 12, 25), R(GreyDark, 15, 22, 18, 25) } },
            { JournalIcon.Leaf, new[] { Q(Amber, 6, 26, 8, 10, 22, 5, 26, 12), Q(Amber, 6, 26, 26, 12, 24, 24, 12, 27), L(WoodDark, 7, 25, 22, 10, 1.5f) } },
            { JournalIcon.Sun, new[] { C(Gold, 16, 16, 7), L(Gold, 16, 2, 16, 6, 2), L(Gold, 16, 26, 16, 30, 2), L(Gold, 2, 16, 6, 16, 2), L(Gold, 26, 16, 30, 16, 2),
                L(Gold, 6, 6, 9, 9, 2), L(Gold, 23, 23, 26, 26, 2), L(Gold, 6, 26, 9, 23, 2), L(Gold, 23, 9, 26, 6, 2) } },
            { JournalIcon.Snow, new[] { L(Sky, 16, 3, 16, 29, 2.5f), L(Sky, 5, 9, 27, 23, 2.5f), L(Sky, 5, 23, 27, 9, 2.5f), C(White, 16, 16, 3) } },
            { JournalIcon.Swords, new[] { L(Grey, 6, 6, 24, 24, 3), L(Grey, 26, 6, 8, 24, 3), L(Wood, 20, 26, 26, 20, 3), L(Wood, 6, 20, 12, 26, 3), C(Gold, 6, 26, 2), C(Gold, 26, 26, 2) } },
            { JournalIcon.Handshake, new[] { Q(WoodLight, 3, 14, 12, 11, 20, 18, 11, 22), Q(Cream, 29, 14, 20, 11, 12, 18, 21, 22), R(Sky, 1, 10, 5, 20, 1), R(Leaf, 27, 10, 31, 20, 1) } },
            { JournalIcon.Shield, new[] { Q(Sky, 6, 5, 26, 5, 26, 16, 6, 16), T(Sky, 6, 16, 26, 16, 16, 29), L(White, 16, 6, 16, 26, 2), L(White, 7, 13, 25, 13, 2) } },
            { JournalIcon.Skull, new[] { C(Cream, 16, 13, 10), R(Cream, 10, 18, 22, 27, 2), C(Ink, 12, 13, 3), C(Ink, 20, 13, 3), T(Ink, 16, 17, 14, 21, 18, 21), L(Ink, 14, 24, 14, 27, 1), L(Ink, 18, 24, 18, 27, 1) } },
            { JournalIcon.Cart, new[] { R(Wood, 4, 10, 26, 20, 2), L(WoodDark, 26, 14, 31, 12, 2), C(Ink, 9, 23, 4), C(Grey, 9, 23, 2), C(Ink, 21, 23, 4), C(Grey, 21, 23, 2), R(Gold, 7, 6, 13, 10), R(Leaf, 15, 7, 21, 10) } },
            { JournalIcon.Heart, new[] { C(Red, 11, 12, 6.5f), C(Red, 21, 12, 6.5f), T(Red, 4.8f, 14, 27.2f, 14, 16, 28) } },
            { JournalIcon.Warning, new[] { T(Amber, 16, 3, 2, 28, 30, 28), R(Ink, 14.5f, 11, 17.5f, 21, 1), C(Ink, 16, 24.5f, 1.8f) } },
            { JournalIcon.Globe, new[] { C(Sky, 16, 16, 12), Q(Leaf, 9, 9, 15, 7, 14, 15, 8, 14), Q(Leaf, 18, 16, 25, 14, 24, 23, 19, 25), O(SkyDark, 16, 16, 12, 1.5f) } },
            { JournalIcon.Feather, new[] { Q(Cream, 8, 26, 22, 4, 27, 6, 13, 24), L(GreyDark, 6, 28, 24, 6, 1.5f) } },
            { JournalIcon.Compass, new[] { C(Cream, 16, 16, 13), O(WoodDark, 16, 16, 13, 2f),
                T(Red, 16, 5, 12.5f, 16, 19.5f, 16), T(GreyDark, 16, 27, 12.5f, 16, 19.5f, 16), C(GoldDark, 16, 16, 2f) } },
        };

        /** The picture for an icon; every icon has one. */
        public static Shape[] picture(JournalIcon icon) {
            return Pictures.TryGetValue(icon, out Shape[] shapes) ? shapes : Pictures[JournalIcon.Star];
        }

        public static bool hasPicture(JournalIcon icon) {
            return Pictures.ContainsKey(icon);
        }
    }
}
