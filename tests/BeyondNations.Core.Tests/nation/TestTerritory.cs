using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestTerritory {

        [Fact]
        public void testClaimRadius_GrowsWithTier() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation nation = world.pawnNation("Aland", 1);
            Settlement camp = world.settlement(nation, 0, 0);
            Assert.Equal(Territory.CAMP_CLAIM_RADIUS, camp.getClaimRadius());

            camp.addBuilding(BuildingType.HOUSE);
            camp.addBuilding(BuildingType.FARM);
            Territory.updateClaims(world.entities);
            Assert.Equal(Territory.VILLAGE_CLAIM_RADIUS, camp.getClaimRadius());

            camp.addBuilding(BuildingType.HOUSE);
            camp.addBuilding(BuildingType.FARM);
            camp.addBuilding(BuildingType.STOREHOUSE);
            Territory.updateClaims(world.entities);
            Assert.Equal(Territory.TOWN_CLAIM_RADIUS, camp.getClaimRadius());
        }

        [Fact]
        public void testClaims_NeverOverlap_AndShareTheGapByTier() {
            DiplomacyFixture world = new DiplomacyFixture();
            Settlement town = world.settlement(world.pawnNation("Aland", 1), 0, 0, 5);
            Settlement camp = world.settlement(world.pawnNation("Bshire", 1), 150, 0);

            Assert.True(town.getClaimRadius() + camp.getClaimRadius() <= 150.001f);
            // 130 : 70 of the 150 between them
            Assert.Equal(150f * 130f / 200f, town.getClaimRadius(), 3);
            Assert.Equal(150f * 70f / 200f, camp.getClaimRadius(), 3);
        }

        [Fact]
        public void testFindClaimAt_NamesTheOwnerOrNullInTheWilds() {
            DiplomacyFixture world = new DiplomacyFixture();
            Settlement camp = world.settlement(world.pawnNation("Aland", 1), 0, 0);

            Assert.Same(camp, Territory.findClaimAt(world.entities, new Vector3(60, 5, 0)));
            Assert.Null(Territory.findClaimAt(world.entities, new Vector3(80, 1, 0)));
        }

        [Fact]
        public void testFounding_InsideForeignClaim_IsRefused() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation other = world.pawnNation("Aland", 1);
            world.settlement(other, 0, 0);
            Nation own = world.playerNation(1);

            Assert.Equal("This land is claimed by Aland.",
                Territory.getFoundingObjection(world.entities, world.nations, new Vector3(30, 1, 0), own.getId()));
            Assert.Null(Territory.getFoundingObjection(world.entities, world.nations, new Vector3(30, 1, 0), other.getId()));
            Assert.Null(Territory.getFoundingObjection(world.entities, world.nations, new Vector3(300, 1, 0), own.getId()));
        }

        [Fact]
        public void testFoundSettlementCommand_InsideForeignClaim_TellsThePlayer() {
            DiplomacyFixture world = new DiplomacyFixture();
            world.settlement(world.pawnNation("Aland", 1), 0, 0);
            world.playerNation(1);
            world.player.setPosition(new Vector3(30, 1, 0));
            world.player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            new FoundSettlementCommand(world.nations, world.simulation.getEventProducer(), world.entities, world.simulation.getGameConfig(), world.simulation.getRandom())
                .execute(world.player);

            Assert.Equal("This land is claimed by Aland. Found your settlement outside its borders.", world.player.getStatus().getStatus());
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, world.player.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testFoundingNearANeighbour_CostsRelations() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation near = world.pawnNation("Aland", 1);
            world.settlement(near, 0, 0);
            Nation far = world.pawnNation("Bshire", 1);
            world.settlement(far, 2000, 0);
            Nation own = world.playerNation(1);
            Settlement founded = world.settlement(own, 300, 0);

            Territory.recordFounding(founded, world.entities, world.nations);

            Assert.Equal(Diplomacy.FOUNDING_NEAR_BORDER, world.relations.getScore(own, near));
            Assert.Equal(0, world.relations.getScore(own, far));
        }

        [Fact]
        public void testDescribeFor_SaysWhoseLandThePlayerIsIn() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation other = world.pawnNation("Aland", 1);
            world.settlement(other, 0, 0);
            Nation own = world.playerNation(1);
            world.settlement(own, 400, 0);
            world.relations.adjust(own, other, -30);

            world.player.setPosition(new Vector3(10, 1, 0));
            Assert.Equal("Aland (Rival)", Territory.describeFor(world.player, world.entities, world.nations));
            world.player.setPosition(new Vector3(400, 1, 10));
            Assert.Equal("your own land", Territory.describeFor(world.player, world.entities, world.nations));
            world.player.setPosition(new Vector3(200, 1, 0));
            Assert.Equal("wilds", Territory.describeFor(world.player, world.entities, world.nations));
        }

        [Fact]
        public void testBorderPosts_AreDrawnAtTheClaimInTheNationsColour() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation nation = world.pawnNation("Aland", 1);
            Settlement camp = world.settlement(nation, 0, 0);
            camp.refreshAppearance();

            int atBorder = 0;
            foreach (AppearancePart part in camp.getAppearance().getParts()) {
                Vector3 o = part.getOffset();
                float distance = new Vector2(o.X, o.Z).Length();
                if (System.MathF.Abs(distance - camp.getClaimRadius()) < 0.01f && part.getColor().Equals(nation.getColor())) {
                    atBorder++;
                }
            }
            Assert.Equal(SettlementLayout.getBorderPostCount(Territory.CAMP_CLAIM_RADIUS), atBorder);
        }
    }
}
