using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * What a settlement looks like (SettlementLayout): deterministic, grows as
    * the settlement grows, and tells owned stalls from stalls for sale.
    */
    public class TestSettlementLayout {
        private readonly RandomSource random = new RandomSource(20261018);
        private static readonly Rgba NationColour = new Rgba(0.1f, 0.2f, 0.9f);

        private Settlement makeSettlement(Vector3 position) {
            return new Settlement(position, new NationId(), NationColour, "Testland", random);
        }

        private static void grow(Settlement settlement) {
            settlement.addBuilding(BuildingType.HOUSE);
            settlement.addBuilding(BuildingType.FARM);
            settlement.getMarket().createStall();
            settlement.getMarket().createStall();
            settlement.getMarket().getStalls()[0].setOwnerId(new EntityId());
            settlement.refreshAppearance();
        }

        private static List<string> describe(Appearance appearance) {
            List<string> parts = new List<string>();
            foreach (AppearancePart part in appearance.getParts()) {
                parts.Add(part.getKind() + " " + part.getOffset() + " " + part.getScale() + " " + part.getColor());
            }
            return parts;
        }

        [Fact]
        public void testLayout_IsDeterministic() {
            Settlement first = makeSettlement(new Vector3(0, 1, 0));
            Settlement second = makeSettlement(new Vector3(500, 1, -300));
            grow(first);
            grow(second);

            Assert.Equal(describe(first.getAppearance()), describe(second.getAppearance()));
        }

        [Fact]
        public void testLayout_SitsOnTheGroundWhereverTheFounderStood() {
            Settlement low = makeSettlement(new Vector3(0, 0.5f, 0));
            Settlement high = makeSettlement(new Vector3(0, 1.5f, 0));

            // the plaza's world height is the same either way
            float lowY = low.getPosition().Y + low.getAppearance().getPrimaryPart().getOffset().Y;
            float highY = high.getPosition().Y + high.getAppearance().getPrimaryPart().getOffset().Y;
            Assert.Equal(lowY, highY, 3);
        }

        [Fact]
        public void testLayout_GrowsWithStallsAndBuildings() {
            Settlement settlement = makeSettlement(Vector3.Zero);
            int bare = settlement.getAppearance().getParts().Count;

            settlement.getMarket().createStall();
            settlement.refreshAppearance();
            int withStall = settlement.getAppearance().getParts().Count;

            settlement.addBuilding(BuildingType.HOUSE);
            int withHouse = settlement.getAppearance().getParts().Count;

            Assert.True(withStall > bare);
            Assert.True(withHouse > withStall);
        }

        [Fact]
        public void testLayout_OwnedStallHasNationAwning_ForSaleDoesNot() {
            Settlement settlement = makeSettlement(Vector3.Zero);
            settlement.getMarket().createStall();
            settlement.refreshAppearance();
            int nationPartsForSale = countColour(settlement.getAppearance(), NationColour);

            settlement.getMarket().getStalls()[0].setOwnerId(new EntityId());
            settlement.refreshAppearance();
            int nationPartsOwned = countColour(settlement.getAppearance(), NationColour);

            Assert.Equal(nationPartsForSale + 1, nationPartsOwned);
        }

        [Fact]
        public void testRefreshAppearance_RedrawsOnlyWhenSomethingChanged() {
            Settlement settlement = makeSettlement(Vector3.Zero);
            settlement.getMarket().createStall();
            settlement.refreshAppearance();
            AppearancePart before = settlement.getAppearance().getPrimaryPart();

            settlement.refreshAppearance();
            Assert.Same(before, settlement.getAppearance().getPrimaryPart());

            settlement.getMarket().getStalls()[0].getInventory().addItem(ItemType.APPLE, 1);
            settlement.refreshAppearance();
            Assert.NotSame(before, settlement.getAppearance().getPrimaryPart());
        }

        [Fact]
        public void testSignature_ChangesWithStockpile() {
            Settlement settlement = makeSettlement(Vector3.Zero);
            long empty = SettlementLayout.computeSignature(settlement);

            settlement.getInventory().addItem(ItemType.STONE, 20);

            Assert.NotEqual(empty, SettlementLayout.computeSignature(settlement));
        }

        [Fact]
        public void testTown_IsFencedByAPalisade() {
            Settlement settlement = makeSettlement(Vector3.Zero);
            for (int i = 0; i < SettlementTiers.TOWN_BUILDINGS - 1; i++) {
                settlement.addBuilding(i % 2 == 0 ? BuildingType.HOUSE : BuildingType.FARM);
            }
            int village = settlement.getAppearance().getParts().Count;

            settlement.addBuilding(BuildingType.HOUSE);
            int town = settlement.getAppearance().getParts().Count;

            Assert.Equal(SettlementTier.TOWN, settlement.getTier());
            Assert.True(town - village > 20, "a town should gain a ring of palisade posts");
        }

        [Fact]
        public void testSlots_DoNotOverlap() {
            for (int i = 0; i < BuildingCatalog.MAX_BUILDINGS; i++) {
                for (int j = i + 1; j < BuildingCatalog.MAX_BUILDINGS; j++) {
                    Assert.True(Vector3.Distance(SettlementLayout.getBuildingSlotOffset(i), SettlementLayout.getBuildingSlotOffset(j)) > 7f);
                }
            }
            for (int i = 0; i < SettlementLayout.StallSlots; i++) {
                for (int j = i + 1; j < SettlementLayout.StallSlots; j++) {
                    Assert.True(Vector3.Distance(SettlementLayout.getStallSlotOffset(i), SettlementLayout.getStallSlotOffset(j)) > 3f);
                }
            }
        }

        [Fact]
        public void testSnapshot_LiftsAndEnlargesTheSettlementNametag() {
            EntityRepository entityRepository = new EntityRepository(random);
            Settlement settlement = makeSettlement(new Vector3(10, 0.5f, 20));
            entityRepository.addEntity(settlement);
            WorldSnapshot snapshot = new WorldSnapshot();

            snapshot.capture(entityRepository, null);

            LabelItem label = snapshot.getLabels()[0];
            Assert.True(label.position.Y > settlement.getPosition().Y + 2f);
            Assert.Equal(SettlementLayout.LabelScale, label.scale);
        }

        private static int countColour(Appearance appearance, Rgba colour) {
            int count = 0;
            foreach (AppearancePart part in appearance.getParts()) {
                if (part.getColor().Equals(colour)) {
                    count++;
                }
            }
            return count;
        }
    }
}
