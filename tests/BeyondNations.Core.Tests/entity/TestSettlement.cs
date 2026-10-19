using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestSettlement {
        private readonly RandomSource random = new RandomSource(20260816);

        private Settlement makeSettlement(NationId nationId) {
            return new Settlement(new Vector3(10, 0, 20), nationId, Rgba.Red, "Testland", random);
        }

        [Fact]
        public void testInstantiation() {
            // prepare
            NationId nationId = new NationId();

            // run
            Settlement settlement = makeSettlement(nationId);

            // check
            Assert.Equal(EntityType.SETTLEMENT, settlement.getType());
            Assert.Equal("Settlement", settlement.getName());
            Assert.Equal(new Vector3(10, 0, 20), settlement.getPosition());
            Assert.Equal(nationId, settlement.getNationId());
            Assert.Equal(Rgba.Red, settlement.getColor());
            Assert.Equal(0, settlement.getFunds());
            Assert.Equal(0, settlement.getCurrentlyPresentEntitiesCount());
            Assert.Equal(4, settlement.getMarket().getMaxNumStalls());

            // drawn as a place: a plaza disc first, with the nation's colour on the well and banner
            AppearancePart part = settlement.getAppearance().getPrimaryPart();
            Assert.Equal(PrimitiveKind.Cylinder, part.getKind());
            Assert.True(settlement.getAppearance().getParts().Count > 1);
            Assert.Contains(settlement.getAppearance().getParts(), p => p.getColor().Equals(Rgba.Red));
            Assert.Equal(SettlementTier.CAMP, settlement.getTier());
            Assert.Equal(BuildingCatalog.BASE_RESIDENTS, settlement.getResidentCap());
        }

        [Fact]
        public void testNameTag_NamesNationAndCountsNobody() {
            Settlement settlement = makeSettlement(new NationId());

            Assert.Equal("Camp of Testland\n0 here, 0/8 residents", settlement.getNameTagText());
        }

        [Fact]
        public void testAddCurrentlyPresentEntity_UpdatesCountAndNameTag() {
            // prepare
            Settlement settlement = makeSettlement(new NationId());
            EntityId first = new EntityId();
            EntityId second = new EntityId();

            // run
            settlement.addCurrentlyPresentEntity(first);
            settlement.addCurrentlyPresentEntity(second);

            // check
            Assert.Equal(2, settlement.getCurrentlyPresentEntitiesCount());
            Assert.Contains(first, settlement.getCurrentlyPresentEntities());
            Assert.Contains(second, settlement.getCurrentlyPresentEntities());
            Assert.Equal("Camp of Testland\n2 here, 0/8 residents", settlement.getNameTagText());
        }

        [Fact]
        public void testRemoveCurrentlyPresentEntity_UpdatesCountAndNameTag() {
            // prepare
            Settlement settlement = makeSettlement(new NationId());
            EntityId stays = new EntityId();
            EntityId leaves = new EntityId();
            settlement.addCurrentlyPresentEntity(stays);
            settlement.addCurrentlyPresentEntity(leaves);

            // run
            settlement.removeCurrentlyPresentEntity(leaves);

            // check
            Assert.Equal(1, settlement.getCurrentlyPresentEntitiesCount());
            Assert.Contains(stays, settlement.getCurrentlyPresentEntities());
            Assert.DoesNotContain(leaves, settlement.getCurrentlyPresentEntities());
            Assert.Equal("Camp of Testland\n1 here, 0/8 residents", settlement.getNameTagText());
        }

        [Fact]
        public void testFunds_AddAndRemoveCoins() {
            // prepare
            Settlement settlement = makeSettlement(new NationId());

            // run
            settlement.addFunds(30);
            settlement.removeFunds(12);

            // check
            Assert.Equal(18, settlement.getFunds());
            Assert.Equal(18, settlement.getInventory().getNumItems(ItemType.COIN));
        }
    }
}
