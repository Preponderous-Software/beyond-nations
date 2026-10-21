using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestNationDiplomacyCommands {
        private readonly DiplomacyFixture world = new DiplomacyFixture();

        private string status() {
            return world.player.getStatus().getStatus();
        }

        [Fact]
        public void testGift_MovesCoinsToTheirTreasury_AndWarmsRelations() {
            Nation own = world.playerNation(1);
            Nation other = world.pawnNation("Aland", 1);
            Settlement treasury = world.settlement(other, 0, 0);
            int before = world.player.getInventory().getNumItems(ItemType.COIN);
            world.player.getInventory().addItem(ItemType.COIN, 30);

            new GiftNationCommand(world.nations, world.diplomacy).execute(world.player, other);

            Assert.Equal(before + 5, world.player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(Diplomacy.PLAYER_GIFT_COINS, treasury.getFunds());
            Assert.Equal(10, world.relations.getScore(own, other));
            Assert.Equal("Aland accepts your gift of 25 coins. Relations: Neutral (+10).", status());
        }

        [Fact]
        public void testGift_WithoutTheCoins_ChangesNothing() {
            Nation own = world.playerNation(1);
            Nation other = world.pawnNation("Aland", 1);
            Inventory inventory = world.player.getInventory();
            inventory.removeItem(ItemType.COIN, inventory.getNumItems(ItemType.COIN));
            inventory.addItem(ItemType.COIN, 3);

            new GiftNationCommand(world.nations, world.diplomacy).execute(world.player, other);

            Assert.Equal(3, world.player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(0, world.relations.getScore(own, other));
            Assert.Equal("A gift to Aland costs 25 coins; you carry 3.", status());
        }

        [Fact]
        public void testOnlyTheLeader_SpeaksForANation() {
            Nation theirs = world.pawnNation("Aland", 1);
            Nation other = world.pawnNation("Bshire", 1);
            theirs.addMember(world.player.getId());
            world.player.setNationId(theirs.getId());

            new DenounceNationCommand(world.nations, world.diplomacy).execute(world.player, other);

            Assert.Equal(0, world.relations.getScore(theirs, other));
            Assert.Equal("Only the leader of Aland can speak for it.", status());
        }

        [Fact]
        public void testProposeAlliance_RefusedUntilRelationsAreHighEnough() {
            Nation own = world.playerNation(1);
            Nation other = world.pawnNation("Aland", 1);
            world.relations.adjust(own, other, 12);
            ProposeAllianceCommand propose = new ProposeAllianceCommand(world.nations);

            propose.execute(world.player, other);
            Assert.False(world.relations.isAllied(own, other));
            Assert.Equal("Aland declines: relations are Neutral (+12); an alliance needs +30. Gifts help.", status());

            world.relations.adjust(own, other, 18);
            propose.execute(world.player, other);
            Assert.True(world.relations.isAllied(own, other));
            Assert.Equal(NationRelationState.ALLIED, world.relations.getState(own, other));
        }

        [Fact]
        public void testDenounce_BreaksAnAlliance_AndSoursRelations() {
            Nation own = world.playerNation(1);
            Nation other = world.pawnNation("Aland", 1);
            world.relations.adjust(own, other, 40);
            world.relations.formAlliance(own, other);

            new DenounceNationCommand(world.nations, world.diplomacy).execute(world.player, other);

            Assert.False(world.relations.isAllied(own, other));
            Assert.Equal(15, world.relations.getScore(own, other));
            Assert.Equal("You denounced Aland. Relations: Neutral (+15).", status());
        }
    }
}
