using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A ruler's realm survives a save (docs/SAVES.md): laws in force and
    * coming in, wars and truces, the line of rulers and a pending
    * succession all come back, and the world goes on exactly as the saved
    * one does. A save from before the realm loads with none of it.
    */
    public class TestRealmSaves {
        private const float Step = 1f / 50f;

        private static void run(Simulation simulation, int ticks) {
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(Step);
            }
        }

        private static void assertSame(Simulation original, Simulation copy, string when, HashSet<Guid> savedIds) {
            string difference = savedIds == null ? WorldComparer.difference(original, copy) : WorldComparer.difference(original, copy, savedIds);
            Assert.True(difference == null, when + ": " + difference);
            Assert.Equal(WorldComparer.fingerprint(original), WorldComparer.fingerprint(copy));
        }

        /** The diplomacy stage, ruled: laws in force and pending, a war, a truce, and one succession behind it. */
        private static Simulation ruledWorld(out DiplomacyShowcase.Staged staged) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            staged = DiplomacyShowcase.stage(simulation);
            Realm realm = simulation.getRealm();
            Player player = simulation.getPlayer();
            realm.proclaim(staged.playerNation, LawType.TAXES, 1);
            realm.proclaim(staged.playerNation, LawType.CONSCRIPTION, 1);
            // a law round run straight away: the staged siege would take the village in the time a law takes
            realm.update(Realm.LAW_DELAY_TICKS);
            run(simulation, 10);
            realm.proclaim(staged.playerNation, LawType.OPEN_BORDERS, 1);
            new DeclareWarCommand(simulation.getNationRepository(), realm).execute(player, staged.hostile);
            realm.grantTruce(staged.playerNation, staged.friendly);
            // starving, and with nothing to eat (an objective's reward may have been food)
            foreach (ItemType food in FoodItems.getFoodInDescendingEnergyOrder()) {
                player.getInventory().removeItem(food, player.getInventory().getNumItems(food));
            }
            player.setEnergy(0.0001f);
            run(simulation, 1);
            Assert.Single(realm.getLineage());
            return simulation;
        }

        [Fact]
        public void testARuledWorld_SavedMidReign_LoadsAndRunsOnIdentically() {
            DiplomacyShowcase.Staged staged;
            Simulation original = ruledWorld(out staged);
            Assert.Equal(1, original.getRealm().getLevel(staged.playerNation, LawType.TAXES));
            Assert.Equal(1, original.getRealm().getLaws(staged.playerNation).getPending(LawType.OPEN_BORDERS));

            byte[] file = SaveGame.encode(original, "test");
            Simulation copy = SaveGame.decode(file);
            assertSame(original, copy, "straight after loading", null);

            Realm loaded = copy.getRealm();
            Nation loadedNation = copy.getNationRepository().getNation(copy.getPlayer().getNationId());
            Nation loadedHostile = null;
            Nation loadedFriendly = null;
            foreach (Nation nation in copy.getNationRepository().getNations()) {
                if (nation.getName() == staged.hostile.getName()) loadedHostile = nation;
                if (nation.getName() == staged.friendly.getName()) loadedFriendly = nation;
            }
            Assert.Equal(1, loaded.getLevel(loadedNation, LawType.CONSCRIPTION));
            Assert.True(loaded.isAtWar(loadedNation, loadedHostile));
            Assert.True(loaded.hasTruce(loadedNation, loadedFriendly));
            // the envoy sent with the declaration of war is still away (#424)
            Nation originalNation = original.getNationRepository().getNation(original.getPlayer().getNationId());
            Assert.True(original.getRealm().getEnvoyTicksLeft(originalNation, staged.hostile) > 0);
            Assert.Equal(original.getRealm().getEnvoyTicksLeft(originalNation, staged.hostile), loaded.getEnvoyTicksLeft(loadedNation, loadedHostile));
            Assert.Equal(original.getRealm().getLineage()[0].getName(), loaded.getLineage()[0].getName());
            Assert.Equal(original.getPlayer().getName(), copy.getPlayer().getName());
            // the succession screen was not yet shown when the world was saved: it is still to show
            Assert.NotNull(loaded.consumeNotice());

            HashSet<Guid> savedIds = WorldComparer.idsOf(original);
            original.getRealm().consumeNotice();
            run(original, 3000);
            run(copy, 3000);
            assertSame(original, copy, "after 3000 more ticks", savedIds);
        }

        [Fact]
        public void testAWorldWhoseLineHasEnded_LoadsStillEnded() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation original = new Simulation(config);
            new NationCreateCommand(original.getNationRepository(), original.getEventProducer(), original.getRandom(), original.getNationNameGenerator())
                .execute(original.getPlayer());
            original.getPlayer().setEnergy(0f);
            run(original, 1);
            Assert.True(original.getRealm().isRunOver());

            Simulation copy = SaveGame.decode(SaveGame.encode(original, "test"));

            Assert.True(copy.getRealm().isRunOver());
            Assert.True(copy.getRealm().consumeNotice().isLineEnded());
        }

        [Fact]
        public void testAVersion1Save_LoadsWithAnEmptyRealm() {
            Simulation loaded = SaveGame.decode(File.ReadAllBytes(SaveFixtures.pathFor(1)));
            Realm realm = loaded.getRealm();

            Assert.Empty(realm.getLineage());
            Assert.False(realm.isRunOver());
            Assert.Null(realm.getNotice());
            // the law rounds a world played to tick 2400 would have counted
            Assert.Equal(SaveFixtures.FixtureTicks / Realm.LAW_ROUND_TICKS, realm.getLawRound());
            foreach (Nation nation in loaded.getNationRepository().getNations()) {
                Assert.Null(realm.getLaws(nation));
            }

            // and plays on as a world of today's format does, laws and all
            run(loaded, Realm.LAW_ROUND_TICKS * 2);
            Assert.Equal(SaveFixtures.FixtureTicks / Realm.LAW_ROUND_TICKS + 2, realm.getLawRound());
        }
    }
}
