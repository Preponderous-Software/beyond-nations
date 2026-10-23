using System.Linq;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A new player's nation is not swamped by hostility it did nothing to
    * earn (playtest 2026-10-21: raided 37 times in ten minutes by neighbours
    * it had never met). While its era shelters it, border friction alone
    * stops short of hostility and raids on it are rationed; whatever does
    * change between it and another nation is told with the reason; and a
    * nation the player leads only raids and besieges those it is at war
    * with. Nations the player has nothing to do with are untouched.
    */
    public class TestEarlyHostility {

        /** The player's nation, sheltered as EraTracker.applyRules would leave it in the Camp era. */
        private static Nation shelteredPlayerNation(DiplomacyFixture world, int members) {
            Nation own = world.playerNation(members);
            own.setSheltered(true);
            return own;
        }

        private static int raidsOn(DiplomacyFixture world, Nation victim) {
            EventRepository events = world.simulation.getEventRepository();
            if (!hasRaids(events)) {
                return 0;
            }
            return events.getEvents(EventType.NationRaid).Count(e => ((NationRaidEvent) e).getVictim() == victim);
        }

        private static bool hasRaids(EventRepository events) {
            try {
                return events.getNumberOfEvents(EventType.NationRaid) > 0;
            } catch (System.Collections.Generic.KeyNotFoundException) {
                return false;
            }
        }

        // --- border friction ------------------------------------------------------------------------------

        [Fact]
        public void testBorderFriction_AloneStopsShortOfHostility_WhileThePlayerIsSheltered() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = shelteredPlayerNation(world, 1);
            Nation neighbour = world.pawnNation("Aland", 3);
            world.settlement(own, 0, 0);
            world.settlement(neighbour, 140, 0);   // camps 140 apart: borders touch

            world.rounds(100);

            Assert.Equal(Diplomacy.SHELTERED_FRICTION_FLOOR, world.relations.getScore(own, neighbour));
            Assert.Equal(NationRelationState.RIVAL, world.relations.getState(own, neighbour));
            // the floor sits above the score at which a pawn-led nation denounces
            Assert.True(Diplomacy.SHELTERED_FRICTION_FLOOR > Diplomacy.AI_DENOUNCE_AT);
            Assert.False(world.relations.hasDenounced(neighbour, own));
            Assert.Equal(0, raidsOn(world, own));
        }

        [Fact]
        public void testBorderFriction_OnceTheShelterEnds_GoesOnToHostility() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = world.playerNation(1);
            Nation neighbour = world.pawnNation("Aland", 3);
            world.settlement(own, 0, 0);
            world.settlement(neighbour, 140, 0);

            world.rounds(100);

            Assert.Equal(NationRelationState.HOSTILE, world.relations.getState(own, neighbour));
        }

        [Fact]
        public void testBorderFriction_BetweenOtherNations_IsUnchanged() {
            DiplomacyFixture world = new DiplomacyFixture();
            shelteredPlayerNation(world, 1);
            // of equal strength, so neither denounces the other on the way down
            Nation a = world.pawnNation("Aland", 1);
            Nation b = world.pawnNation("Bshire", 1);
            world.settlement(a, 0, 0);
            world.settlement(b, 140, 0);

            world.rounds(30);

            Assert.Equal(30 * Diplomacy.TOUCHING_FRICTION, world.relations.getScore(a, b));
        }

        [Fact]
        public void testHostilityThePlayerEarns_IsNotFloored() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = shelteredPlayerNation(world, 1);
            Nation neighbour = world.pawnNation("Aland", 3);
            world.settlement(own, 0, 0);
            world.settlement(neighbour, 140, 0);
            world.rounds(100);

            world.diplomacy.change(own, neighbour, Diplomacy.DENOUNCE);
            world.rounds(10);

            // what the player did stands, and so does the stronger neighbour's
            // denunciation in answer; friction no longer deepens it
            Assert.True(world.relations.hasDenounced(neighbour, own));
            Assert.Equal(Diplomacy.SHELTERED_FRICTION_FLOOR + Diplomacy.DENOUNCE + Diplomacy.AI_DENOUNCE, world.relations.getScore(own, neighbour));
            Assert.Equal(NationRelationState.HOSTILE, world.relations.getState(own, neighbour));
        }

        // --- raids on the player --------------------------------------------------------------------------

        [Fact]
        public void testRaidsOnAShelteredPlayer_AreRationed() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = shelteredPlayerNation(world, 1);
            Nation a = world.pawnNation("Aland", 3);
            Nation b = world.pawnNation("Bshire", 3);
            Settlement camp = world.settlement(own, 0, 0);
            world.settlement(a, 300, 0);
            world.settlement(b, -300, 0);
            camp.addFunds(1000);
            world.relations.adjust(own, a, -80);
            world.relations.adjust(own, b, -80);

            world.rounds(2 * Diplomacy.SHELTERED_RAID_EVERY_ROUNDS);

            // two hostile neighbours would have raided every fourth round each: 24 raids
            Assert.Equal(2, raidsOn(world, own));
        }

        [Fact]
        public void testRaidsOnThePlayer_OnceTheShelterEnds_ComeAsBefore() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = world.playerNation(1);
            Nation a = world.pawnNation("Aland", 3);
            Settlement camp = world.settlement(own, 0, 0);
            world.settlement(a, 300, 0);
            camp.addFunds(1000);
            world.relations.adjust(own, a, -80);

            world.rounds(2 * Diplomacy.SHELTERED_RAID_EVERY_ROUNDS);

            Assert.Equal(2 * Diplomacy.SHELTERED_RAID_EVERY_ROUNDS / Diplomacy.RAID_EVERY_ROUNDS, raidsOn(world, own));
        }

        [Fact]
        public void testARaidOnThePlayer_SaysWhoAndWhy() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = world.playerNation(1);
            Nation a = world.pawnNation("Aland", 3);
            Settlement camp = world.settlement(own, 0, 0);
            world.settlement(a, 300, 0);
            camp.addFunds(12);
            world.relations.adjust(own, a, -80);

            world.rounds(Diplomacy.RAID_EVERY_ROUNDS);

            string status = world.player.getStatus().getStatus();
            Assert.Contains("Aland raided your camp: 5 coins taken.", status);
            Assert.Contains("Hostile (-", status);
        }

        // --- telling the player ---------------------------------------------------------------------------

        [Fact]
        public void testBorderFriction_IsToldWhenItFirstGrates() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = shelteredPlayerNation(world, 1);
            Nation neighbour = world.pawnNation("Aland", 3);
            world.settlement(own, 0, 0);
            world.settlement(neighbour, 300, 0);

            world.rounds(1);

            Assert.Contains("Aland resents your border", world.player.getStatus().getStatus());
        }

        [Fact]
        public void testARelationChange_WithThePlayer_IsToldWithItsReason() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = shelteredPlayerNation(world, 1);
            Nation neighbour = world.pawnNation("Aland", 3);
            world.settlement(own, 0, 0);
            world.settlement(neighbour, 300, 0);

            world.rounds(-NationRelations.RIVAL_AT / -Diplomacy.NEIGHBOUR_FRICTION);

            Assert.Equal("Aland resents your border: relations are now Rival (-20).", world.player.getStatus().getStatus());
            Event change = world.simulation.getEventRepository().getEvents(EventType.NationRelationChange).Last();
            Assert.Contains("have become rivals over their border", change.getDescription());
            Assert.Contains("over their border", world.simulation.getChronicle().getEntries().Last().getText());
        }

        [Fact]
        public void testADriftSeeSawingAcrossABoundary_IsToldOnce() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = shelteredPlayerNation(world, 1);
            Nation neighbour = world.pawnNation("Aland", 3);
            world.settlement(own, 0, 0);
            world.settlement(neighbour, 300, 0);
            world.rounds(-NationRelations.RIVAL_AT / -Diplomacy.NEIGHBOUR_FRICTION);
            Assert.Contains("now Rival", world.player.getStatus().getStatus());

            // trade warms it back over the line, and the border sours it again
            world.relations.adjust(own, neighbour, 1);
            world.player.getStatus().update("Gathered 5 wood.");
            world.rounds(1);

            Assert.Equal(NationRelationState.RIVAL, world.relations.getState(own, neighbour));
            Assert.Equal("Gathered 5 wood.", world.player.getStatus().getStatus());
        }

        // --- raids by the player's nation -----------------------------------------------------------------

        [Fact]
        public void testANationThePlayerLeads_DoesNotRaidOrBesiegeOnItsOwn() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = world.playerNation(6);
            Nation weak = world.pawnNation("Aland", 1);
            world.settlement(own, 0, 0);
            Settlement target = world.settlement(weak, 300, 0);
            target.addFunds(100);
            world.relations.adjust(own, weak, -80);

            world.rounds(2 * Diplomacy.RAID_EVERY_ROUNDS);

            Assert.Equal(0, raidsOn(world, weak));
            Assert.Null(world.diplomacy.getBesieger(target));
        }

        [Fact]
        public void testANationThePlayerLeads_RaidsANationItIsAtWarWith_AndSaysSo() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation own = world.playerNation(3);
            Nation weak = world.pawnNation("Aland", 2);
            world.settlement(own, 0, 0);
            Settlement target = world.settlement(weak, 300, 0);
            target.addFunds(100);
            world.simulation.getRealm().declareWar(own, weak);

            world.rounds(Diplomacy.RAID_EVERY_ROUNDS);

            Assert.Equal(1, raidsOn(world, weak));
            Assert.Contains("Your people raided Aland (at war with you): 5 coins brought home.", world.player.getStatus().getStatus());
        }

        [Fact]
        public void testANationThePlayerJoined_StillRaids_AndTheReasonIsTold() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation joined = world.pawnNation("Aland", 4);
            joined.addMember(world.player.getId());
            world.player.setNationId(joined.getId());
            Nation weak = world.pawnNation("Bshire", 1);
            world.settlement(joined, 0, 0);
            Settlement target = world.settlement(weak, 300, 0);
            target.addFunds(100);
            world.relations.adjust(joined, weak, -80);

            world.rounds(Diplomacy.RAID_EVERY_ROUNDS);

            Assert.Equal(1, raidsOn(world, weak));
            Assert.Contains("Your nation's raiders struck Bshire (Hostile, -", world.player.getStatus().getStatus());
        }

        // --- the era box ----------------------------------------------------------------------------------

        [Fact]
        public void testTheShelter_SaysRaidsCanStillCome() {
            Assert.Contains("raid", EraRules.ShelterDescription);
            Assert.Contains("siege", EraRules.ShelterDescription);
        }
    }
}
