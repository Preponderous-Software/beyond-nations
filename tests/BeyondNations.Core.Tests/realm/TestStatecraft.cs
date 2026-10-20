using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * War, peace, alliance and tribute as the player offers them, and how a
    * pawn-led nation answers -- by relations and by strength, with the
    * reason on the status line (realm/Realm, command/nation/RealmCommands).
    */
    public class TestStatecraft {
        private readonly DiplomacyFixture world = new DiplomacyFixture();
        private Realm realm { get { return world.simulation.getRealm(); } }

        private string status() {
            return world.player.getStatus().getStatus();
        }

        private void declareWar(Nation target) {
            new DeclareWarCommand(world.nations, realm).execute(world.player, target);
        }

        private void offerPeace(Nation target) {
            new OfferPeaceCommand(world.nations, realm).execute(world.player, target);
        }

        private void tribute(Nation target) {
            world.player.getInventory().addItem(ItemType.COIN, Diplomacy.PLAYER_GIFT_COINS);
            new GiftNationCommand(world.nations, world.diplomacy, realm).execute(world.player, target);
        }

        [Fact]
        public void testDeclaringWar_MakesThemHostile_BreaksAlliances_AndAngersTheirAllies() {
            Nation own = world.playerNation(1);
            Nation enemy = world.pawnNation("Aland", 1);
            Nation enemysAlly = world.pawnNation("Bshire", 1);
            world.relations.adjust(own, enemy, 40);
            world.relations.formAlliance(own, enemy);
            world.relations.adjust(enemy, enemysAlly, 40);
            world.relations.formAlliance(enemy, enemysAlly);

            declareWar(enemy);

            Assert.True(realm.isAtWar(own, enemy));
            Assert.False(world.relations.isAllied(own, enemy));
            Assert.Equal(Realm.WAR_SCORE, world.relations.getScore(own, enemy));
            Assert.Equal(NationRelationState.HOSTILE, world.relations.getState(own, enemy));
            Assert.Equal(Realm.WAR_ON_ALLY_PENALTY, world.relations.getScore(own, enemysAlly));
            // the two pawn-led allies count as one: Aland fields 2 against the player's 1
            Assert.Equal("Aland laughs at your declaration of war: they are 2 to your 1. They are far off; war bites only where your lands meet.", status());
            Assert.StartsWith(own.getName() + " declared war on Aland.", world.simulation.getChronicle().getRecent(1)[0].getText());
        }

        [Fact]
        public void testTheEnemyAnswers_ByTheStrengthEachSideFields() {
            Nation own = world.playerNation(4);
            Nation weak = world.pawnNation("Aland", 1);
            Nation even = world.pawnNation("Bshire", 3);
            world.settlement(own, 0, 0);
            world.settlement(weak, 300, 0);
            world.settlement(even, -300, 0);

            declareWar(weak);
            Assert.Equal("Aland falls back behind its walls: war, and they are only 1 to your 4.", status());
            declareWar(even);
            Assert.Equal("Bshire takes up arms: war, 3 to your 4.", status());
            declareWar(even);
            Assert.Equal("You are already at war with Bshire.", status());
        }

        [Fact]
        public void testAtWar_RelationsCannotWarm() {
            Nation own = world.playerNation(1);
            Nation enemy = world.pawnNation("Aland", 1);
            world.settlement(own, 0, 0);
            world.settlement(enemy, 3000, 0);   // too far apart to grate
            declareWar(enemy);

            world.rounds(Diplomacy.DRIFT_EVERY_ROUNDS * 2);

            // without the war, the score would have eased two points toward zero
            Assert.Equal(Realm.WAR_SCORE, world.relations.getScore(own, enemy));
        }

        [Fact]
        public void testPeace_IsRefused_ByAnEnemyTwiceAsStrong_UntilTributeBuysATruce() {
            Nation own = world.playerNation(1);
            Nation enemy = world.pawnNation("Aland", 3);
            declareWar(enemy);

            offerPeace(enemy);
            Assert.True(realm.isAtWar(own, enemy));
            Assert.Equal("Aland refuses peace: they are 3 to your 1 and think they are winning. Tribute may buy a truce.", status());

            tribute(enemy);
            Assert.True(realm.hasTruce(own, enemy));
            Assert.Equal("Aland accepts your tribute of 25 coins and will hold off its raiders for 60 seconds. Relations: Hostile (-50).", status());

            offerPeace(enemy);
            Assert.False(realm.isAtWar(own, enemy));
            Assert.Equal(Realm.PEACE_SCORE, world.relations.getScore(own, enemy));
            Assert.Equal(NationRelationState.NEUTRAL, world.relations.getState(own, enemy));
            Assert.Equal("Aland accepts peace (your tribute has softened them). A truce holds for 60 seconds; relations are Neutral (-19).", status());
        }

        [Fact]
        public void testPeace_IsAccepted_ByTheWeakerSide() {
            Nation own = world.playerNation(3);
            Nation enemy = world.pawnNation("Aland", 1);
            declareWar(enemy);

            offerPeace(enemy);

            Assert.False(realm.isAtWar(own, enemy));
            Assert.True(realm.hasTruce(own, enemy));
            Assert.Equal("Aland accepts peace (they are the weaker, 1 to your 3). A truce holds for 60 seconds; relations are Neutral (-19).", status());
        }

        [Fact]
        public void testPeace_IsRefused_WhenTooMuchBloodLiesBetween() {
            Nation own = world.playerNation(3);
            Nation enemy = world.pawnNation("Aland", 1);
            world.relations.adjust(own, enemy, -95);

            offerPeace(enemy);

            Assert.Equal("Aland refuses peace: too much blood lies between you (Hostile (-95)). Tribute may soften them.", status());
            Assert.False(realm.hasTruce(own, enemy));
        }

        [Fact]
        public void testPeace_WithANationNotAtOdds_IsNotOffered() {
            Nation own = world.playerNation(1);
            Nation other = world.pawnNation("Aland", 1);
            Assert.False(OfferPeaceCommand.isApplicable(world.nations, realm, own, other));

            offerPeace(other);

            Assert.Equal("You are not at odds with Aland: relations are Neutral (0).", status());
        }

        [Fact]
        public void testATruce_HoldsOffRaids_ThatWouldOtherwiseCome() {
            int[] taken = new int[2];
            for (int truce = 0; truce < 2; truce++) {
                DiplomacyFixture fixture = new DiplomacyFixture();
                Nation own = fixture.playerNation(1);
                Nation raider = fixture.pawnNation("Aland", 3);
                Settlement target = fixture.settlement(own, 0, 0);
                fixture.settlement(raider, 300, 0);
                target.getInventory().addItem(ItemType.COIN, 40);
                fixture.relations.adjust(own, raider, -70);
                if (truce == 1) {
                    fixture.simulation.getRealm().grantTruce(own, raider);
                }
                int before = target.getFunds();
                fixture.rounds(Diplomacy.RAID_EVERY_ROUNDS);
                taken[truce] = before - target.getFunds();
            }
            Assert.Equal(Diplomacy.RAID_COINS, taken[0]);
            Assert.Equal(0, taken[1]);
        }

        [Fact]
        public void testAGiftToAnEqual_IsAGiftNotTribute() {
            Nation own = world.playerNation(1);
            Nation other = world.pawnNation("Aland", 1);
            world.relations.adjust(own, other, -30);

            tribute(other);

            Assert.False(realm.hasTruce(own, other));
            Assert.Equal("Aland accepts your gift of 25 coins. Relations: Rival (-20).", status());
        }

        [Fact]
        public void testAlliance_IsRefusedInWar_AndByAMuchStrongerNation_BelowPlus60() {
            Nation own = world.playerNation(1);
            Nation strong = world.pawnNation("Aland", 3);
            ProposeAllianceCommand propose = new ProposeAllianceCommand(world.nations, realm);

            declareWar(strong);
            propose.execute(world.player, strong);
            Assert.Equal("Aland will not ally with a nation it is at war with. Offer peace first.", status());

            realm.makePeace(own, strong);
            world.relations.adjust(own, strong, 35 - world.relations.getScore(own, strong));
            propose.execute(world.player, strong);
            Assert.False(world.relations.isAllied(own, strong));
            Assert.Equal("Aland declines: they are 3 to your 1 and want relations of +60 before taking on so small a partner (now Friendly (+35)).", status());

            world.relations.adjust(own, strong, 25);
            propose.execute(world.player, strong);
            Assert.True(world.relations.isAllied(own, strong));
        }
    }
}
