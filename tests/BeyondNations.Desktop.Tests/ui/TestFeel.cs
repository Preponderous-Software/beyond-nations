using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.feel;

namespace beyondnationstests {

    /** A sound service that remembers what it was asked to play. */
    internal class RecordingSoundService : ISoundService {
        public readonly List<SoundCue> played = new List<SoundCue>();
        public AmbientKind ambient = AmbientKind.None;
        public float ambientVolume;
        public string getName() { return "recording"; }
        public void play(SoundCue cue, float volume) { played.Add(cue); }
        public void setAmbient(AmbientKind kind, float volume) { ambient = kind; ambientVolume = volume; }
        public string getState() { return "running"; }
    }

    /**
    * Sound, juice and the first-five-minutes tips (ui/feel): what is noticed,
    * what is played, what is drawn, what is remembered, and that none of it
    * changes how the world steps.
    */
    public class TestFeel {
        private const float FixedDeltaTime = 1f / 50f;

        private static Simulation makeSimulation(int seed = 42) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            Simulation simulation = new Simulation(config);
            // the first step generates the land around the player and lays out settlements
            simulation.fixedUpdate(FixedDeltaTime);
            return simulation;
        }

        private static void stage(Simulation simulation) {
            SettlementShowcase.stage(simulation);
            simulation.fixedUpdate(FixedDeltaTime);
        }

        private static FeelDirector makeDirector(Simulation simulation, RecordingSoundService sound, IFeelSettingsStore store = null, bool newWorld = true) {
            FeelDirector director = new FeelDirector(sound, store ?? new MemoryFeelSettingsStore(), false);
            director.beginWorld(simulation, newWorld);
            director.frame(simulation, ScreenType.WORLD, 0.016);
            return director;
        }

        // --- settings ---

        [Fact]
        public void testSettings_RoundTrip() {
            FeelSettings settings = new FeelSettings();
            settings.setVolume(0.35f);
            settings.setMuted(true);
            settings.markHintDone("move");
            settings.markHintDone("chop");
            FeelSettings read = FeelSettings.parse(settings.serialize());
            Assert.Equal(0.35f, read.getVolume(), 3);
            Assert.True(read.isMuted());
            Assert.True(read.areHintsEnabled());
            Assert.True(read.isHintDone("move"));
            Assert.True(read.isHintDone("chop"));
            Assert.False(read.isHintDone("stone"));
            Assert.Equal(0f, read.getEffectiveVolume());
        }

        [Fact]
        public void testSettings_ReadLeniently() {
            FeelSettings read = FeelSettings.parse("volume=lots\nmuted=1\nsomething=else\n\nhints=0\nvolume=9\n");
            Assert.Equal(1f, read.getVolume());
            Assert.True(read.isMuted());
            Assert.False(read.areHintsEnabled());
            Assert.Equal(FeelSettings.DefaultVolume, FeelSettings.parse(null).getVolume());
            Assert.Equal(FeelSettings.DefaultVolume, FeelSettings.parse("garbage").getVolume());
        }

        [Fact]
        public void testSettings_AreWrittenWhenChanged() {
            MemoryFeelSettingsStore store = new MemoryFeelSettingsStore();
            Simulation simulation = makeSimulation();
            FeelDirector director = makeDirector(simulation, new RecordingSoundService(), store);
            director.getSettings().setVolume(0.2f);
            director.frame(simulation, ScreenType.WORLD, 0.016);
            Assert.Equal(0.2f, FeelSettings.parse(store.read()).getVolume(), 3);
        }

        // --- what is noticed ---

        [Fact]
        public void testObserver_FirstFrameOnlyRecords() {
            Simulation simulation = makeSimulation();
            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, 30);
            FeelObserver observer = new FeelObserver();
            observer.attach(simulation);
            List<FeelEvent> events = new List<FeelEvent>();
            observer.observe(events);
            Assert.Empty(events);
        }

        [Fact]
        public void testObserver_WoodNextToATreeIsAChop() {
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            AppleTree tree = simulation.getEnvironment().getNearestTree(player.getPosition());
            Assert.NotNull(tree);
            player.setPosition(tree.getPosition() + new Vector3(2f, 0f, 0f));

            FeelObserver observer = new FeelObserver();
            observer.attach(simulation);
            List<FeelEvent> events = new List<FeelEvent>();
            observer.observe(events);

            new InteractCommand(simulation.getEnvironment(), simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getEntityRepository(), simulation.getRandom()).execute(player);
            observer.observe(events);

            Assert.Contains(events, e => e.kind == FeelEventKind.Chop && Vector3.Distance(e.position, tree.getPosition()) < 0.01f);
            Assert.Contains(events, e => e.kind == FeelEventKind.Gain && e.item == ItemType.WOOD && e.amount > 0);
            Assert.DoesNotContain(events, e => e.kind == FeelEventKind.Pickup);
        }

        [Fact]
        public void testObserver_CoinsAndCraftsAndHunger() {
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            player.setPosition(new Vector3(5000f, player.getPosition().Y, 5000f)); // nothing in reach
            FeelObserver observer = new FeelObserver();
            observer.attach(simulation);
            List<FeelEvent> events = new List<FeelEvent>();
            observer.observe(events);

            player.getInventory().addItem(ItemType.COIN, 10);
            observer.observe(events);
            Assert.Contains(events, e => e.kind == FeelEventKind.Coin);

            events.Clear();
            player.getInventory().addItem(ItemType.STONE, 5);
            observer.observe(events);
            Assert.Contains(events, e => e.kind == FeelEventKind.Pickup);
            events.Clear();
            player.getInventory().removeItem(ItemType.STONE, 2);
            player.getInventory().addItem(ItemType.AXE, 1);
            observer.observe(events);
            Assert.Contains(events, e => e.kind == FeelEventKind.Craft);

            events.Clear();
            player.setEnergy(WorldHudHungry() - 1f);
            observer.observe(events);
            Assert.Contains(events, e => e.kind == FeelEventKind.HungerWarning);
            events.Clear();
            observer.observe(events);
            Assert.DoesNotContain(events, e => e.kind == FeelEventKind.HungerWarning);
        }

        private static float WorldHudHungry() {
            return beyondnations.desktop.ui.screens.WorldHud.HungryEnergy;
        }

        [Fact]
        public void testObserver_ObjectiveAndNewBuilding() {
            Simulation simulation = makeSimulation();
            stage(simulation);
            Settlement settlement = (Settlement) simulation.getEnvironment().getNearestEntityOfType(
                simulation.getPlayer().getPosition(), EntityType.SETTLEMENT);
            Assert.NotNull(settlement);

            FeelObserver observer = new FeelObserver();
            observer.attach(simulation);
            List<FeelEvent> events = new List<FeelEvent>();
            observer.observe(events);

            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, 200);
            Assert.True(simulation.getObjectiveTracker().evaluate(simulation));
            int before = settlement.getBuildings().Count;
            Assert.True(settlement.addBuilding(BuildingType.FARM) || settlement.addBuilding(BuildingType.HOUSE)
                || settlement.getBuildings().Count > before);
            observer.observe(events);

            Assert.Contains(events, e => e.kind == FeelEventKind.ObjectiveComplete);
            if (settlement.getBuildings().Count > before) {
                Assert.Contains(events, e => e.kind == FeelEventKind.BuildComplete && e.rises);
            }
        }

        // --- what is played and drawn ---

        [Fact]
        public void testDirector_PlaysCuesAndAmbientAndRespectsMute() {
            Simulation simulation = makeSimulation();
            RecordingSoundService sound = new RecordingSoundService();
            FeelDirector director = makeDirector(simulation, sound);
            Assert.Equal(simulation.getTimeOfDay().isNight() ? AmbientKind.Night : AmbientKind.Day, sound.ambient);
            Assert.True(sound.ambientVolume > 0f);

            simulation.getPlayer().getInventory().addItem(ItemType.COIN, 5);
            director.frame(simulation, ScreenType.WORLD, 0.016);
            Assert.Contains(SoundCue.Coin, sound.played);
            Assert.NotEmpty(director.getJuice().getFloatingTexts());

            director.getSettings().setMuted(true);
            sound.played.Clear();
            simulation.getPlayer().getInventory().addItem(ItemType.COIN, 5);
            director.frame(simulation, ScreenType.WORLD, 0.016);
            Assert.Empty(sound.played);
            Assert.Equal(0f, sound.ambientVolume);

            director.frame(simulation, ScreenType.MAIN_MENU, 0.016);
            Assert.Equal(AmbientKind.None, sound.ambient);
        }

        [Fact]
        public void testDirector_ScreenChangeClicks() {
            Simulation simulation = makeSimulation();
            RecordingSoundService sound = new RecordingSoundService();
            FeelDirector director = makeDirector(simulation, sound);
            director.frame(simulation, ScreenType.PAUSE, 0.016);
            Assert.Contains(SoundCue.UiClick, sound.played);
        }

        [Fact]
        public void testGainText() {
            Assert.Equal("+5 stone", FeelDirector.describeGain(ItemType.STONE, 5));
            Assert.Equal("+1 axe", FeelDirector.describeGain(ItemType.AXE, 1));
            Assert.Equal("-20 coins", FeelDirector.describeGain(ItemType.COIN, -20));
        }

        [Fact]
        public void testAttenuation() {
            Assert.Equal(1f, FeelDirector.attenuate(Vector3.Zero, Vector3.Zero));
            Assert.Equal(0f, FeelDirector.attenuate(new Vector3(FeelDirector.HearingDistance + 1f, 0f, 0f), Vector3.Zero));
            float mid = FeelDirector.attenuate(new Vector3(FeelDirector.HearingDistance / 2f, 0f, 0f), Vector3.Zero);
            Assert.InRange(mid, 0.2f, 0.9f);
        }

        [Fact]
        public void testProjection_CentreOfViewIsCentreOfScreen() {
            Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0f, 0f, -10f), Vector3.Zero, Vector3.UnitY);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(1f, 2f, 0.1f, 100f);
            Assert.True(FeelDirector.project(view * projection, Vector3.Zero, 800f, 400f, out Vector2 screen));
            Assert.Equal(400f, screen.X, 2);
            Assert.Equal(200f, screen.Y, 2);
            Assert.False(FeelDirector.project(view * projection, new Vector3(0f, 0f, -20f), 800f, 400f, out _));
        }

        [Fact]
        public void testJuice_ParticlesAreAddedToTheSnapshotAndFade() {
            Simulation simulation = makeSimulation();
            WorldSnapshot snapshot = new WorldSnapshot();
            snapshot.capture(simulation.getEntityRepository(), simulation.getEnvironment());
            int captured = snapshot.getEntityItems().Count;

            JuiceEffects juice = new JuiceEffects();
            juice.burst(new Vector3(0f, 1f, 0f), JuiceEffects.WoodChip, 10, 5f, 0.3f);
            juice.decorate(snapshot);
            Assert.Equal(captured + 10, snapshot.getEntityItems().Count);

            for (int i = 0; i < 100; i++) {
                juice.update(0.05f);
            }
            Assert.Empty(juice.getParticles());
            snapshot.capture(simulation.getEntityRepository(), simulation.getEnvironment());
            Assert.Equal(captured, snapshot.getEntityItems().Count);
        }

        [Fact]
        public void testJuice_ABuildingRisesOutOfTheGround() {
            Simulation simulation = makeSimulation();
            stage(simulation);
            Settlement settlement = (Settlement) simulation.getEnvironment().getNearestEntityOfType(
                simulation.getPlayer().getPosition(), EntityType.SETTLEMENT);
            Assert.NotEmpty(settlement.getBuildings());
            Building building = settlement.getBuildings()[0];
            Vector3 slot = SettlementLayout.getBuildingSlotOffset(building.getSlot());
            Vector3 centre = new Vector3(settlement.getPosition().X + slot.X, SettlementLayout.GroundTopY, settlement.getPosition().Z + slot.Z);

            WorldSnapshot snapshot = new WorldSnapshot();
            snapshot.capture(simulation.getEntityRepository(), simulation.getEnvironment());
            int captured = snapshot.getEntityItems().Count;
            float tallest = tallestNear(snapshot, centre, captured);

            JuiceEffects juice = new JuiceEffects();
            juice.rise(centre);
            juice.update(JuiceEffects.RiseSeconds * 0.1f);
            juice.decorate(snapshot);
            // the building's own parts (the scaffold added after them stands taller)
            Assert.True(tallestNear(snapshot, centre, captured) < tallest * 0.6f);
            Assert.True(snapshot.getEntityItems().Count > captured);
            Assert.Equal(0.0f, JuiceEffects.riseProgress(0f));
            Assert.Equal(1.0f, JuiceEffects.riseProgress(JuiceEffects.RiseSeconds));
        }

        private static float tallestNear(WorldSnapshot snapshot, Vector3 centre, int count) {
            float top = 0f;
            for (int i = 0; i < count; i++) {
                RenderItem item = snapshot.getEntityItems()[i];
                float dx = item.position.X - centre.X;
                float dz = item.position.Z - centre.Z;
                if (dx * dx + dz * dz < 9f) {
                    top = Math.Max(top, item.position.Y + item.scale.Y / 2f);
                }
            }
            return top;
        }

        [Fact]
        public void testJuice_FlashAndShakeDecay() {
            JuiceEffects juice = new JuiceEffects();
            Assert.Equal(0f, juice.getFlashStrength());
            juice.flash();
            juice.shake(0.2f);
            juice.update(0.01f);
            Assert.True(juice.getFlashStrength() > 0.9f);
            Assert.NotEqual(Vector3.Zero, juice.getShakeOffset());
            for (int i = 0; i < 20; i++) {
                juice.update(0.1f);
            }
            Assert.Equal(0f, juice.getFlashStrength());
            Assert.Equal(Vector3.Zero, juice.getShakeOffset());
        }

        // --- the tips ---

        [Fact]
        public void testHints_RunInOrderAndAdvanceWhenDone() {
            Simulation simulation = makeSimulation();
            FeelSettings settings = new FeelSettings();
            OnboardingHints hints = new OnboardingHints(settings);
            hints.begin(true);
            hints.update(simulation, 0.5f);
            Assert.Null(hints.getVisible());
            hints.update(simulation, OnboardingHints.FirstDelaySeconds);
            Assert.Equal(OnboardingHints.Step.Move, hints.getVisible());

            Player player = simulation.getPlayer();
            player.setPosition(player.getPosition() + new Vector3(OnboardingHints.MoveDistance + 1f, 0f, 0f));
            hints.update(simulation, 0.016f);
            Assert.Null(hints.getVisible());
            Assert.True(settings.isHintDone("move"));

            hints.update(simulation, OnboardingHints.BetweenSeconds + 0.1f);
            Assert.Equal(OnboardingHints.Step.Chop, hints.getVisible());
            player.getInventory().addItem(ItemType.WOOD, 3);
            hints.update(simulation, 0.016f);
            hints.update(simulation, OnboardingHints.BetweenSeconds + 0.1f);
            Assert.Equal(OnboardingHints.Step.Stone, hints.getVisible());
            Assert.Equal(3, hints.getNumber());

            hints.dismiss();
            hints.update(simulation, OnboardingHints.BetweenSeconds + 0.1f);
            Assert.Equal(OnboardingHints.Step.Found, hints.getVisible());
            Assert.True(settings.isHintDone("stone"));
        }

        [Fact]
        public void testHints_DoneOnesAreNeverShownAgain() {
            Simulation simulation = makeSimulation();
            FeelSettings settings = new FeelSettings();
            settings.markHintDone("move");
            settings.markHintDone("chop");
            OnboardingHints hints = new OnboardingHints(settings);
            hints.begin(true);
            hints.update(simulation, 5f);
            Assert.Equal(OnboardingHints.Step.Stone, hints.getVisible());

            foreach (OnboardingHints.Step step in OnboardingHints.Steps) {
                settings.markHintDone(OnboardingHints.id(step));
            }
            OnboardingHints again = new OnboardingHints(settings);
            again.begin(true);
            again.update(simulation, 5f);
            Assert.Null(again.getVisible());
            Assert.False(again.isActive());
        }

        [Fact]
        public void testHints_SkipAllAndLoadedWorlds() {
            Simulation simulation = makeSimulation();
            FeelSettings settings = new FeelSettings();
            OnboardingHints hints = new OnboardingHints(settings);
            hints.begin(false);
            hints.update(simulation, 5f);
            Assert.Null(hints.getVisible());

            hints.begin(true);
            hints.update(simulation, 5f);
            Assert.NotNull(hints.getVisible());
            hints.skipAll();
            Assert.Null(hints.getVisible());
            Assert.False(settings.areHintsEnabled());
            hints.begin(true);
            hints.update(simulation, 5f);
            Assert.Null(hints.getVisible());

            settings.resetHints();
            Assert.True(settings.areHintsEnabled());
        }

        [Fact]
        public void testHints_TouchWordingNamesTheOnScreenControls() {
            foreach (OnboardingHints.Step step in OnboardingHints.Steps) {
                Assert.False(string.IsNullOrWhiteSpace(OnboardingHints.text(step, true)));
                Assert.False(string.IsNullOrWhiteSpace(OnboardingHints.text(step, false)));
                Assert.DoesNotContain("mouse", OnboardingHints.text(step, true));
                Assert.DoesNotContain("press", OnboardingHints.text(step, true));
            }
            Assert.Contains("tap Use", OnboardingHints.text(OnboardingHints.Step.Chop, true));
            Assert.Contains("press E", OnboardingHints.text(OnboardingHints.Step.Chop, false));
        }

        // --- presentation only ---

        /**
        * The same seed played twice, once with the whole feel layer watching
        * and drawing every frame, must end in the same world: positions,
        * inventories, the chronicle and the random stream.
        */
        [Fact]
        public void testFeelLayerDoesNotChangeTheSimulation() {
            Simulation plain = makeSimulation(7);
            Simulation watched = makeSimulation(7);
            RecordingSoundService sound = new RecordingSoundService();
            FeelDirector director = makeDirector(watched, sound);
            WorldSnapshot snapshot = new WorldSnapshot();
            watched.getPlayer().setMovementInput(0f, 1f);
            plain.getPlayer().setMovementInput(0f, 1f);
            for (int i = 0; i < 3000; i++) {
                plain.fixedUpdate(FixedDeltaTime);
                watched.fixedUpdate(FixedDeltaTime);
                director.frame(watched, ScreenType.WORLD, FixedDeltaTime);
                snapshot.capture(watched.getEntityRepository(), watched.getEnvironment());
                director.decorate(snapshot);
            }
            Assert.Equal(describe(plain), describe(watched));
            Assert.Equal(plain.getRandom().range(0, 1000000), watched.getRandom().range(0, 1000000));
        }

        private static string describe(Simulation simulation) {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            text.Append(simulation.getTickCounter().getTotalTicks()).Append('|');
            EntityRepository entities = simulation.getEntityRepository();
            for (int i = 0; i < entities.getEntityCount(); i++) {
                Entity entity = entities.getEntityAt(i);
                Vector3 p = entity.getPosition();
                text.Append(entity.getType()).Append(':').Append(p.X.ToString("F3")).Append(',').Append(p.Y.ToString("F3")).Append(',').Append(p.Z.ToString("F3")).Append(';');
            }
            foreach (ItemType item in (ItemType[]) Enum.GetValues(typeof(ItemType))) {
                text.Append(item).Append('=').Append(simulation.getPlayer().getInventory().getNumItems(item)).Append(';');
            }
            text.Append(simulation.getChronicle().getEntries().Count);
            return text.ToString();
        }
    }
}
