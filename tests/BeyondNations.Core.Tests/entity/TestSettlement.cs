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

            AppearancePart part = settlement.getAppearance().getPrimaryPart();
            Assert.Equal(PrimitiveKind.Cylinder, part.getKind());
            Assert.Equal(new Vector3(10, 5, 10), part.getScale());
            Assert.Equal(Rgba.Red, part.getColor());
        }

        [Fact]
        public void testNameTag_NamesNationAndCountsNobody() {
            Settlement settlement = makeSettlement(new NationId());

            Assert.Equal("Settlement of Testland\n\n(0)", settlement.getNameTagText());
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
            Assert.Equal("Settlement of Testland\n\n(2)", settlement.getNameTagText());
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
            Assert.Equal("Settlement of Testland\n\n(1)", settlement.getNameTagText());
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
