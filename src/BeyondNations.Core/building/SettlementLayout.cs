using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * What a settlement looks like, worked out from what is in it.
    *
    * A settlement is drawn as a place rather than a single shape: a sandy plaza
    * with a well and the nation's banner at its centre, the market's stalls in
    * a ring around the plaza, and its buildings in a wider ring beyond the
    * stalls. A town is fenced by a palisade. Everything is a compound of the
    * four primitives, so there is no asset to load.
    *
    * Stalls read at a glance: an owned stall's awning is the nation's colour
    * and its counter carries a crate of whatever it has for sale (red for
    * apples); a stall still for sale has a pale awning and a yellow sign.
    *
    * The layout is a pure function of the settlement's state -- slot positions
    * come from slot numbers, never from randomness -- so the same settlement
    * always looks the same, and building something only ever adds to the
    * picture.
    *
    * Primitives are axis-aligned (a RenderItem carries no rotation), so
    * buildings do not turn to face the plaza; a house's door is put on
    * whichever face is nearest it instead.
    */
    public static class SettlementLayout {

        /** Top of the ground tiles: a tile is a unit cube centred on y = 0. */
        public const float GroundTopY = 0.5f;

        public const float PlazaRadius = 7f;
        public const float StallRingRadius = 11f;
        public const int StallSlots = 8;
        public const float BuildingRingRadius = 21f;
        public const float PalisadeRadius = 27f;
        public const int PalisadePosts = 48;

        /** Present pawns wander between these distances from the centre. */
        public const float MillMinRadius = 3f;
        public const float MillMaxRadius = 16f;

        public const float LabelHeight = 3.6f;
        public const float LabelScale = 2f;

        private static readonly Rgba Sand = new Rgba(0.78f, 0.70f, 0.52f);
        private static readonly Rgba Stone = new Rgba(0.56f, 0.56f, 0.58f);
        private static readonly Rgba DarkStone = new Rgba(0.38f, 0.38f, 0.42f);
        private static readonly Rgba Slate = new Rgba(0.27f, 0.29f, 0.34f);
        private static readonly Rgba Water = new Rgba(0.20f, 0.45f, 0.80f);
        private static readonly Rgba Wood = new Rgba(0.55f, 0.37f, 0.20f);
        private static readonly Rgba DarkWood = new Rgba(0.36f, 0.23f, 0.12f);
        private static readonly Rgba Plaster = new Rgba(0.88f, 0.82f, 0.68f);
        private static readonly Rgba Door = new Rgba(0.25f, 0.15f, 0.08f);
        private static readonly Rgba Canvas = new Rgba(0.93f, 0.91f, 0.84f);
        private static readonly Rgba SignYellow = new Rgba(0.98f, 0.82f, 0.15f);
        private static readonly Rgba AppleRed = new Rgba(0.82f, 0.12f, 0.10f);
        private static readonly Rgba Goods = new Rgba(0.70f, 0.55f, 0.33f);
        private static readonly Rgba Soil = new Rgba(0.42f, 0.28f, 0.15f);
        private static readonly Rgba Crop = new Rgba(0.40f, 0.72f, 0.22f);

        /** Where stall slot i stands, relative to the settlement's centre (y = 0). */
        public static Vector3 getStallSlotOffset(int slot) {
            return ringPoint(StallRingRadius, (slot + 0.5f) / StallSlots);
        }

        /** Where building slot i stands, relative to the settlement's centre (y = 0). */
        public static Vector3 getBuildingSlotOffset(int slot) {
            return ringPoint(BuildingRingRadius, (slot + 0.5f) / BuildingCatalog.MAX_BUILDINGS);
        }

        /**
        * A number that changes whenever something this layout draws changes:
        * the stalls (how many, which are owned, what they hold) and the
        * buildings. Comparing it is how a settlement knows it needs redrawing.
        */
        public static long computeSignature(Settlement settlement) {
            List<Stall> stalls = settlement.getMarket().getStalls();
            long signature = stalls.Count;
            for (int i = 0; i < stalls.Count; i++) {
                signature = signature * 8 + stallState(stalls[i]);
            }
            signature = signature * 16 + settlement.getNumBuildings();
            signature = signature * 8 + getStockpileLevel(settlement.getInventory(), ItemType.WOOD);
            signature = signature * 8 + getStockpileLevel(settlement.getInventory(), ItemType.STONE);
            return signature;
        }

        /**
        * Replaces every part of the appearance with the settlement as it now
        * stands, and lifts the nametag above the banner.
        */
        public static void build(Settlement settlement, Appearance appearance) {
            appearance.clearParts();
            Rgba nation = settlement.getColor();
            // Parts are offsets from the settlement's position, which is
            // wherever its founder stood; drawing from the ground up means
            // starting from the ground's height relative to that.
            float g = GroundTopY - settlement.getPosition().Y;

            addCentre(appearance, nation, g);
            addStockpile(appearance, settlement.getInventory(), g);

            List<Stall> stalls = settlement.getMarket().getStalls();
            for (int i = 0; i < stalls.Count && i < StallSlots; i++) {
                addStall(appearance, getStallSlotOffset(i), stalls[i], nation, g);
            }

            foreach (Building building in settlement.getBuildings()) {
                Vector3 at = getBuildingSlotOffset(building.getSlot());
                switch (building.getType()) {
                    case BuildingType.HOUSE: addHouse(appearance, at, nation, g); break;
                    case BuildingType.STOREHOUSE: addStorehouse(appearance, at, nation, g); break;
                    case BuildingType.FARM: addFarm(appearance, at, g); break;
                }
            }

            if (settlement.getTier() == SettlementTier.TOWN) {
                addPalisade(appearance, g);
            }

            appearance.setLabelOffset(new Vector3(0f, g + LabelHeight, 0f));
            appearance.setLabelScale(LabelScale);
        }

        // pieces --------------------------------------------------------------------------------------------

        private static void addCentre(Appearance a, Rgba nation, float g) {
            // plaza: a flat disc of packed sand
            a.addPart(PrimitiveKind.Cylinder, new Vector3(0, g + 0.05f, 0), new Vector3(PlazaRadius * 2f, 0.05f, PlazaRadius * 2f), Sand);
            // well
            a.addPart(PrimitiveKind.Cylinder, new Vector3(0, g + 0.6f, 0), new Vector3(2.6f, 0.6f, 2.6f), Stone);
            a.addPart(PrimitiveKind.Cylinder, new Vector3(0, g + 1.22f, 0), new Vector3(2.0f, 0.02f, 2.0f), Water);
            a.addPart(PrimitiveKind.Cube, new Vector3(-1.1f, g + 2.0f, 0), new Vector3(0.2f, 2.0f, 0.2f), DarkWood);
            a.addPart(PrimitiveKind.Cube, new Vector3(1.1f, g + 2.0f, 0), new Vector3(0.2f, 2.0f, 0.2f), DarkWood);
            a.addPart(PrimitiveKind.Cube, new Vector3(0, g + 3.1f, 0), new Vector3(3.0f, 0.3f, 1.4f), nation);
            // banner: a tall pole flying the nation's colour
            a.addPart(PrimitiveKind.Cylinder, new Vector3(4f, g + 3.5f, 4f), new Vector3(0.25f, 3.5f, 0.25f), DarkWood);
            a.addPart(PrimitiveKind.Cube, new Vector3(5.1f, g + 6.2f, 4f), new Vector3(2.0f, 1.3f, 0.1f), nation);
        }

        private static void addStall(Appearance a, Vector3 at, Stall stall, Rgba nation, float g) {
            bool owned = stall.getOwnerId() != null;
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 0.5f, 0), new Vector3(2.6f, 1.0f, 1.4f), Wood);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(-1.2f, g + 1.25f, -0.6f), new Vector3(0.15f, 2.5f, 0.15f), DarkWood);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(1.2f, g + 1.25f, -0.6f), new Vector3(0.15f, 2.5f, 0.15f), DarkWood);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(-1.2f, g + 1.25f, 0.6f), new Vector3(0.15f, 2.5f, 0.15f), DarkWood);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(1.2f, g + 1.25f, 0.6f), new Vector3(0.15f, 2.5f, 0.15f), DarkWood);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 2.6f, 0), new Vector3(3.0f, 0.25f, 1.9f), owned ? nation : Canvas);

            int state = stallState(stall);
            if (!owned) {
                a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 1.7f, 0), new Vector3(1.0f, 0.6f, 0.1f), SignYellow);
            }
            else if (state != 0) {
                Rgba goods = (state & 2) != 0 ? AppleRed : Goods;
                a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 1.3f, 0), new Vector3(0.9f, 0.6f, 0.7f), goods);
            }
        }

        private static void addHouse(Appearance a, Vector3 at, Rgba nation, float g) {
            Rgba roof = shade(nation, 0.75f);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 1.5f, 0), new Vector3(4f, 3f, 4f), Plaster);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 3.3f, 0), new Vector3(4.8f, 0.6f, 4.8f), roof);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 3.9f, 0), new Vector3(3.4f, 0.6f, 3.4f), roof);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 4.45f, 0), new Vector3(1.8f, 0.5f, 1.8f), roof);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(1.3f, g + 4.4f, 1.3f), new Vector3(0.5f, 1.4f, 0.5f), Stone);
            a.addPart(PrimitiveKind.Cube, at + doorOffset(at, 2f, 1.8f) + new Vector3(0, g + 0.9f, 0), doorScale(at, 1.0f, 1.8f), Door);
        }

        private static void addStorehouse(Appearance a, Vector3 at, Rgba nation, float g) {
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 1.75f, 0), new Vector3(5f, 3.5f, 4.4f), Stone);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 3.75f, 0), new Vector3(5.6f, 0.5f, 5.0f), Slate);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 3.2f, 0), new Vector3(5.1f, 0.3f, 4.5f), nation);
            a.addPart(PrimitiveKind.Cube, at + doorOffset(at, 2.2f, 2.4f) + new Vector3(0, g + 1.2f, 0), doorScale(at, 2.0f, 2.4f), Door);
            // a stack of cut stone beside it
            a.addPart(PrimitiveKind.Cube, at + new Vector3(3.3f, g + 0.4f, 1.2f), new Vector3(1.2f, 0.8f, 1.2f), DarkStone);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(3.3f, g + 1.1f, 1.2f), new Vector3(0.9f, 0.6f, 0.9f), DarkStone);
        }

        private static void addFarm(Appearance a, Vector3 at, float g) {
            a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 0.1f, 0), new Vector3(6.5f, 0.2f, 5.5f), Soil);
            for (int row = -1; row <= 1; row++) {
                a.addPart(PrimitiveKind.Cube, at + new Vector3(0, g + 0.5f, row * 1.7f), new Vector3(5.6f, 0.6f, 0.7f), Crop);
            }
            // fence posts at the corners
            a.addPart(PrimitiveKind.Cube, at + new Vector3(-3.25f, g + 0.6f, -2.75f), new Vector3(0.25f, 1.2f, 0.25f), DarkWood);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(3.25f, g + 0.6f, -2.75f), new Vector3(0.25f, 1.2f, 0.25f), DarkWood);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(-3.25f, g + 0.6f, 2.75f), new Vector3(0.25f, 1.2f, 0.25f), DarkWood);
            a.addPart(PrimitiveKind.Cube, at + new Vector3(3.25f, g + 0.6f, 2.75f), new Vector3(0.25f, 1.2f, 0.25f), DarkWood);
        }

        private static void addPalisade(Appearance a, float g) {
            // A ring of sharpened stakes with a gap left for a gate on each side
            // the axes cross, so the town can still be walked into.
            for (int i = 0; i < PalisadePosts; i++) {
                if (i % (PalisadePosts / 4) < 2) {
                    continue;
                }
                Vector3 at = ringPoint(PalisadeRadius, (float) i / PalisadePosts);
                a.addPart(PrimitiveKind.Cylinder, at + new Vector3(0, g + 1.2f, 0), new Vector3(0.45f, 1.2f, 0.45f), DarkWood);
            }
        }

        /**
        * The settlement's stockpile, piled on the plaza: a log pile and a heap
        * of stone that rise a layer for every StockpileUnitsPerLevel delivered,
        * so a leader can see from the plaza whether there is enough to build.
        */
        private static void addStockpile(Appearance a, Inventory stockpile, float g) {
            int woodLevels = getStockpileLevel(stockpile, ItemType.WOOD);
            for (int i = 0; i < woodLevels; i++) {
                a.addPart(PrimitiveKind.Cube, new Vector3(4.8f, g + 0.3f + i * 0.6f, -3.6f), new Vector3(2.4f - i * 0.3f, 0.6f, 1.2f), Wood);
            }
            int stoneLevels = getStockpileLevel(stockpile, ItemType.STONE);
            for (int i = 0; i < stoneLevels; i++) {
                a.addPart(PrimitiveKind.Cube, new Vector3(4.8f, g + 0.3f + i * 0.6f, -1.4f), new Vector3(1.8f - i * 0.3f, 0.6f, 1.6f - i * 0.3f), DarkStone);
            }
        }

        public const int StockpileUnitsPerLevel = 15;
        public const int StockpileMaxLevels = 4;

        /** How many layers tall the pile of this item is drawn: 0 when there is none. */
        public static int getStockpileLevel(Inventory stockpile, ItemType itemType) {
            int amount = stockpile.getNumItems(itemType);
            if (amount <= 0) {
                return 0;
            }
            return Math.Min(StockpileMaxLevels, 1 + amount / StockpileUnitsPerLevel);
        }

        // helpers -------------------------------------------------------------------------------------------

        /**
        * 0 for an empty stall, 1 for one holding goods but no apples, 2 or 3
        * for one holding apples; 4 is added when it has an owner.
        */
        private static int stallState(Stall stall) {
            Inventory inventory = stall.getInventory();
            int state = 0;
            if (inventory.getNumItems(ItemType.WOOD) > 0 || inventory.getNumItems(ItemType.STONE) > 0 || inventory.getNumItems(ItemType.SAPLING) > 0) {
                state |= 1;
            }
            if (FoodItems.hasFood(inventory)) {
                state |= 2;
            }
            if (stall.getOwnerId() != null) {
                state |= 4;
            }
            return state;
        }

        private static Vector3 ringPoint(float radius, float turns) {
            float angle = turns * 2f * MathF.PI;
            return new Vector3(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);
        }

        /** The middle of whichever face of a building points most toward the plaza. */
        private static Vector3 doorOffset(Vector3 at, float halfDepth, float height) {
            if (MathF.Abs(at.X) > MathF.Abs(at.Z)) {
                return new Vector3(-MathF.Sign(at.X) * (halfDepth + 0.05f), 0, 0);
            }
            return new Vector3(0, 0, -MathF.Sign(at.Z) * (halfDepth + 0.05f));
        }

        private static Vector3 doorScale(Vector3 at, float width, float height) {
            if (MathF.Abs(at.X) > MathF.Abs(at.Z)) {
                return new Vector3(0.1f, height, width);
            }
            return new Vector3(width, height, 0.1f);
        }

        private static Rgba shade(Rgba color, float factor) {
            return new Rgba(color.r * factor, color.g * factor, color.b * factor, color.a);
        }
    }
}
