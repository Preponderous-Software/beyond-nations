using System.IO;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The claimant world event (worldevents/events/ClaimantEvent) goes through
    * the Realm's rules (realm/): wars and truces bend the goodwill and grudges
    * it brings, a nation keeps its laws through a coup but loses its deposed
    * ruler's pending proclamations, the player's own rule passes only to an
    * heir, and a nation's heir is still its eldest member after a usurper.
    */
    public class TestClaimantRealm {
        private const float Step = 1f / 50f;

        private static Simulation newWorld(int seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        /** The player's nation beside a hostile and a friendly pawn-led one, far enough apart that nothing is besieged. */
        private static DiplomacyShowcase.Staged staged(Simulation simulation) {
            DiplomacyShowcase.Staged s = DiplomacyShowcase.stage(simulation);
            Vector3 at = s.hostileSettlement.getPosition();
            Vector3 away = Vector3.Normalize(at - s.playerSettlement.getPosition());
            s.hostileSettlement.setPosition(at + away * 600f);
            simulation.getNationRepository().getRelations().adjust(s.playerNation, s.hostile, -DiplomacyShowcase.HostileScore);
            simulation.getDiplomacy().runRound();
            simulation.getPlayer().getInventory().setNumItems(ItemType.COIN, 50);
            return s;
        }

        private static WorldEventInstance offer(Simulation simulation) {
            Assert.True(simulation.getWorldEvents().startPlayerEventNow(simulation, ClaimantEvent.Id), "could not offer the claimant");
            WorldEventInstance pending = simulation.getWorldEvents().getPending();
            Assert.Equal(ClaimantEvent.Id, pending.getEventId());
            return pending;
        }

        private static string choose(Simulation simulation, int choice) {
            string outcome = simulation.getWorldEvents().choose(simulation, choice);
            Assert.NotNull(outcome);
            Assert.Null(simulation.getWorldEvents().getPending());
            return outcome;
        }

        private static int score(Simulation simulation, Nation a, Nation b) {
            return simulation.getNationRepository().getRelations().getScore(a, b);
        }

        // --- war and truce ----------------------------------------------------------------------------------

        [Fact]
        public void testBackingAClaimant_InANationAtWar_PutsThemInPower_ButTheWarKeepsRelationsDown() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventInstance pending = offer(simulation);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            Realm realm = simulation.getRealm();
            realm.declareWar(s.playerNation, other);
            int before = score(simulation, s.playerNation, other);
            string backing = simulation.getWorldEvents().getPendingChoices(simulation)[0].getConsequence();
            Assert.Contains("war", backing);

            choose(simulation, 0);

            Assert.Equal(pending.getEntityId(), other.getLeaderId());
            Assert.Equal(before, score(simulation, s.playerNation, other));
            Assert.True(realm.isAtWar(s.playerNation, other));
            Assert.Equal(30, simulation.getPlayer().getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testWarningALeader_InANationAtWar_DrivesTheClaimantOut_WithoutGoodwill() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventInstance pending = offer(simulation);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            EntityId leader = other.getLeaderId();
            simulation.getRealm().declareWar(s.playerNation, other);
            int before = score(simulation, s.playerNation, other);
            int leaves = RealmTestHelp.count(simulation.getEventRepository(), EventType.NationLeave);

            choose(simulation, 1);

            Pawn claimant = (Pawn) simulation.getEntityRepository().getEntity(pending.getEntityId());
            Assert.False(other.isMember(claimant.getId()));
            Assert.Null(claimant.getNationId());
            Assert.Equal(Rgba.White, claimant.getAppearance().getPrimaryPart().getColor());
            Assert.Equal(leaves + 1, RealmTestHelp.count(simulation.getEventRepository(), EventType.NationLeave));
            Assert.Equal(leader, other.getLeaderId());
            Assert.Equal(before, score(simulation, s.playerNation, other));
        }

        [Fact]
        public void testWarningALeader_InPeacetime_StillEarnsGoodwill() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventInstance pending = offer(simulation);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            int before = score(simulation, s.playerNation, other);
            choose(simulation, 1);
            Assert.Equal(before + ClaimantEvent.WARNED_RELATIONS, score(simulation, s.playerNation, other));
        }

        [Fact]
        public void testAClaimantWhoWinsAlone_UnderATruce_BearsNoGrudge() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventInstance pending = offer(simulation);
            pending.setMagnitude(1);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            simulation.getRealm().grantTruce(s.playerNation, other);
            int before = score(simulation, s.playerNation, other);
            Assert.Contains("truce", simulation.getWorldEvents().getPendingChoices(simulation)[2].getConsequence());

            choose(simulation, 2);

            Assert.Equal(pending.getEntityId(), other.getLeaderId());
            Assert.Equal(before, score(simulation, s.playerNation, other));
        }

        [Fact]
        public void testAClaimantWhoWinsAlone_WithoutATruce_RemembersIt() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventInstance pending = offer(simulation);
            pending.setMagnitude(1);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            int before = score(simulation, s.playerNation, other);

            choose(simulation, 2);

            Assert.Equal(pending.getEntityId(), other.getLeaderId());
            Assert.Equal(before + ClaimantEvent.SPURNED_RELATIONS, score(simulation, s.playerNation, other));
        }

        // --- laws -------------------------------------------------------------------------------------------

        [Fact]
        public void testAUsurper_KeepsTheNationsLaws_ButNotTheDeposedRulersPendingProclamations() {
            Simulation simulation = newWorld(42);
            staged(simulation);
            WorldEventInstance pending = offer(simulation);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            Realm realm = simulation.getRealm();
            realm.proclaim(other, LawType.CONSCRIPTION, 1);
            realm.getLaws(other).setLevel(LawType.TAXES, 2);
            Assert.Equal(1, realm.getLaws(other).getPending(LawType.CONSCRIPTION));

            choose(simulation, 0);

            Assert.Equal(pending.getEntityId(), other.getLeaderId());
            Assert.Equal(2, realm.getLevel(other, LawType.TAXES));
            Assert.Equal(-1, realm.getLaws(other).getPending(LawType.CONSCRIPTION));
            Assert.Equal(-1, realm.getTicksUntilEnacted(other, LawType.CONSCRIPTION));
            Assert.Equal(0, realm.getLevel(other, LawType.CONSCRIPTION));
        }

        [Fact]
        public void testAClaimThatComesToNothing_LeavesPendingProclamationsAlone() {
            Simulation simulation = newWorld(42);
            staged(simulation);
            WorldEventInstance pending = offer(simulation);
            pending.setMagnitude(0);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            EntityId leader = other.getLeaderId();
            Realm realm = simulation.getRealm();
            realm.proclaim(other, LawType.RATIONING, 1);

            choose(simulation, 2);

            Assert.Equal(leader, other.getLeaderId());
            Assert.Equal(1, realm.getLaws(other).getPending(LawType.RATIONING));
        }

        // --- heirs and succession ---------------------------------------------------------------------------

        [Fact]
        public void testAClaim_AgainstANationThePlayerHasComeToRule_ComesToNothing() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventInstance pending = offer(simulation);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            Player player = simulation.getPlayer();
            // between the claimant's coming and the choice, the player has come to rule the nation
            other.addMember(player.getId());
            other.setLeaderId(player.getId());
            other.setRole(player.getId(), NationRole.LEADER);
            int before = score(simulation, s.playerNation, other);
            int changes = RealmTestHelp.count(simulation.getEventRepository(), EventType.LeadershipChange);

            string outcome = choose(simulation, 0);

            Assert.Contains("passes only to an heir", outcome);
            Assert.Equal(player.getId(), other.getLeaderId());
            Assert.True(other.isMember(pending.getEntityId()));
            Assert.Equal(50, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(before, score(simulation, s.playerNation, other));
            Assert.Equal(changes, RealmTestHelp.count(simulation.getEventRepository(), EventType.LeadershipChange));
        }

        [Fact]
        public void testWhyClaimFails_GuardsThePlayersRule_AndOnlyLetsAMemberWhoDoesNotLeadClaim() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            Realm realm = simulation.getRealm();
            Pawn leader = (Pawn) simulation.getEntityRepository().getEntity(s.hostile.getLeaderId());
            Pawn member = null;
            foreach (EntityId id in s.hostile.getMemberIds()) {
                if (!id.Equals(s.hostile.getLeaderId()) && simulation.getEntityRepository().getEntity(id) is Pawn pawn) {
                    member = pawn;
                    break;
                }
            }
            Assert.NotNull(member);
            Pawn outsider = null;
            foreach (EntityId id in s.friendly.getMemberIds()) {
                if (simulation.getEntityRepository().getEntity(id) is Pawn pawn) {
                    outsider = pawn;
                    break;
                }
            }
            Assert.NotNull(outsider);

            Assert.Null(realm.whyClaimFails(s.hostile, member));
            Assert.NotNull(realm.whyClaimFails(s.hostile, leader));
            Assert.NotNull(realm.whyClaimFails(s.hostile, outsider));
            Assert.NotNull(realm.whyClaimFails(s.hostile, null));

            // a member of the player's own nation can never take it from the player
            Pawn ownMember = null;
            foreach (EntityId id in s.playerNation.getMemberIds()) {
                if (simulation.getEntityRepository().getEntity(id) is Pawn pawn) {
                    ownMember = pawn;
                    break;
                }
            }
            Assert.NotNull(ownMember);
            Assert.Contains("heir", realm.whyClaimFails(s.playerNation, ownMember));
            Assert.False(realm.usurp(s.playerNation, ownMember));
            Assert.Equal(simulation.getPlayer().getId(), s.playerNation.getLeaderId());
        }

        [Fact]
        public void testTheClaimantEvent_NeverStirsInTheNationThePlayerRules() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            for (int i = 0; i < 20; i++) {
                WorldEventInstance pending = offer(simulation);
                Assert.NotEqual(s.playerNation.getId(), pending.getOtherNationId());
                simulation.getWorldEvents().choose(simulation, 2);
            }
        }

        [Fact]
        public void testAfterAUsurperDies_TheNationsEldest_TheDeposedLeader_SucceedsThem() {
            Simulation simulation = newWorld(42);
            staged(simulation);
            WorldEventInstance pending = offer(simulation);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            EntityId deposed = other.getLeaderId();
            Assert.Equal(deposed, other.getOldestMemberId());

            choose(simulation, 0);
            Assert.Equal(NationRole.SERF, other.getRole(deposed));
            Assert.True(other.isMember(deposed));

            Pawn usurper = (Pawn) simulation.getEntityRepository().getEntity(pending.getEntityId());
            usurper.setEnergy(0);
            simulation.fixedUpdate(Step);

            Assert.False(other.isMember(usurper.getId()));
            Assert.Equal(deposed, other.getLeaderId());
            Assert.Equal(NationRole.LEADER, other.getRole(deposed));
        }

        // --- saves ------------------------------------------------------------------------------------------

        [Fact]
        public void testAPendingClaimant_SavedAtWar_StillHonoursTheWarOnceLoaded() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventInstance pending = offer(simulation);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            simulation.getRealm().declareWar(s.playerNation, other);

            Simulation loaded = SaveGame.decode(SaveGame.encode(simulation, "test"));
            Nation loadedOwn = loaded.getNationRepository().getNation(s.playerNation.getId());
            Nation loadedOther = loaded.getNationRepository().getNation(other.getId());
            int before = score(loaded, loadedOwn, loadedOther);

            choose(loaded, 0);

            Assert.Equal(pending.getEntityId(), loadedOther.getLeaderId());
            Assert.Equal(before, score(loaded, loadedOwn, loadedOther));
            Assert.True(loaded.getRealm().isAtWar(loadedOwn, loadedOther));
        }

        /**
        * Every committed save format loads, and a claim made in the loaded
        * world goes through the realm: a pawn-led nation of the fixture is
        * usurped with the nation's laws kept and its proclamation lapsing.
        */
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void testEverySaveFormat_LoadsAndTakesAClaimThroughTheRealm(int version) {
            Simulation loaded = SaveGame.decode(File.ReadAllBytes(SaveFixtures.pathFor(version)));
            Nation target = null;
            Pawn claimant = null;
            foreach (Nation nation in loaded.getNationRepository().getNations()) {
                foreach (EntityId id in nation.getMemberIds()) {
                    if (loaded.getEntityRepository().getEntity(id) is Pawn pawn && loaded.getRealm().whyClaimFails(nation, pawn) == null) {
                        target = nation;
                        claimant = pawn;
                        break;
                    }
                }
                if (target != null) {
                    break;
                }
            }
            if (target == null) {
                // the fixture's pawn-led nations are too young for a claim: one more member makes one
                foreach (Nation nation in loaded.getNationRepository().getNations()) {
                    if (!nation.getLeaderId().Equals(loaded.getPlayer().getId())) {
                        target = nation;
                        break;
                    }
                }
                Assert.NotNull(target);
                claimant = new Pawn(new Vector3(0f, 2f, 0f), loaded.getPawnNameGenerator().generate(), loaded.getRandom());
                loaded.getEntityRepository().addEntity(claimant);
                target.addMember(claimant.getId());
                target.setRole(claimant.getId(), NationRole.SERF);
                claimant.setNationId(target.getId());
            }
            Realm realm = loaded.getRealm();
            Assert.Null(realm.whyClaimFails(target, claimant));

            realm.proclaim(target, LawType.TAXES, 1);
            Assert.True(realm.usurp(target, claimant));
            Assert.Equal(claimant.getId(), target.getLeaderId());
            Assert.Equal(-1, realm.getLaws(target).getPending(LawType.TAXES));

            // and the world plays on and saves again
            for (int i = 0; i < 200; i++) {
                loaded.fixedUpdate(Step);
            }
            Assert.Null(WorldComparer.difference(loaded, SaveGame.decode(SaveGame.encode(loaded, "test"))));
        }
    }
}
