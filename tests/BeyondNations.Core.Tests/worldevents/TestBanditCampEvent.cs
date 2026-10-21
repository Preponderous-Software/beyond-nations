using System.Collections.Generic;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Every branch of the bandit camp (BanditCampEvent and its camp in
    * WorldEvents): who it falls on, what each choice costs and does, how a
    * pawn-led nation chooses, and -- the gap this covers -- that a camp the
    * player waited on can still be driven off or paid while it stands,
    * including one brought back from a save.
    */
    public class TestBanditCampEvent {
        private const float Step = 1f / 50f;

        private static Simulation newWorld() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            return new Simulation(config);
        }

        private static void run(Simulation simulation, int ticks) {
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(Step);
            }
        }

        /** The player leads a nation with a village, far from a hostile nation that is calmed down so it does not take the village. */
        private static DiplomacyShowcase.Staged staged(Simulation simulation) {
            DiplomacyShowcase.Staged s = DiplomacyShowcase.stage(simulation);
            System.Numerics.Vector3 at = s.hostileSettlement.getPosition();
            System.Numerics.Vector3 away = System.Numerics.Vector3.Normalize(at - s.playerSettlement.getPosition());
            s.hostileSettlement.setPosition(at + away * 600f);
            simulation.getNationRepository().getRelations().adjust(s.playerNation, s.hostile, -DiplomacyShowcase.HostileScore);
            simulation.getDiplomacy().runRound();
            return s;
        }

        private static WorldEventContext contextOf(Simulation simulation) {
            return new WorldEventContext(simulation, simulation.getWorldEvents());
        }

        private static void setFunds(Settlement settlement, int funds) {
            settlement.removeFunds(settlement.getFunds());
            settlement.addFunds(funds);
        }

        private static BanditCampEvent banditEvent(Simulation simulation) {
            return (BanditCampEvent) simulation.getWorldEvents().getRegistry().get(BanditCampEvent.Id);
        }

        /** The player's village gets bandits and the player bars the gates against them. */
        private static WorldEffect waitedOutCamp(Simulation simulation, DiplomacyShowcase.Staged s) {
            s.playerSettlement.addFunds(100);
            s.playerSettlement.getInventory().addItem(ItemType.WOOD, 40);
            Assert.True(simulation.getWorldEvents().startPlayerEventNow(simulation, BanditCampEvent.Id));
            Assert.False(BanditCampEvent.isStandingCamp(simulation.getWorldEvents().getPending()));
            Assert.NotNull(simulation.getWorldEvents().choose(simulation, 2));
            WorldEffect camp = simulation.getWorldEvents().findEffect(WorldEffectKind.BANDITS, null, s.playerSettlement.getId());
            Assert.NotNull(camp);
            return camp;
        }

        private static List<string> worldEventLines(Simulation simulation) {
            List<string> lines = new List<string>();
            foreach (ChronicleEntry entry in simulation.getChronicle().getEntries()) {
                if (entry.getEventType() == EventType.WorldEvent) {
                    lines.Add(entry.getText());
                }
            }
            return lines;
        }

        // --- who it falls on ------------------------------------------------

        [Fact]
        public void testWeight_GrowsWithTheRichestSettlement_AndIsZeroWithNothingToSteal() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            BanditCampEvent bandits = banditEvent(simulation);
            WorldEventContext context = contextOf(simulation);
            s.playerSettlement.getInventory().setNumItems(ItemType.WOOD, 0);
            s.playerSettlement.getInventory().setNumItems(ItemType.STONE, 0);

            setFunds(s.playerSettlement, 9);
            Assert.Equal(0, bandits.getWeight(context, s.playerNation));
            Assert.False(bandits.prepare(context, s.playerNation, new WorldEventInstance(BanditCampEvent.Id, 0, 0, true)));

            setFunds(s.playerSettlement, 40);
            Assert.Equal(3 + 4, bandits.getWeight(context, s.playerNation));
            setFunds(s.playerSettlement, 500);
            Assert.Equal(3 + 9, bandits.getWeight(context, s.playerNation));

            WorldEventInstance instance = new WorldEventInstance(BanditCampEvent.Id, 0, 0, true);
            Assert.True(bandits.prepare(context, s.playerNation, instance));
            Assert.Equal(s.playerSettlement.getId(), instance.getSettlementId());
            Assert.Equal(0, bandits.getWeight(context, null));
        }

        [Fact]
        public void testATown_IsBehindItsPalisade_AndACampedSettlementIsNotCampedTwice() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            BanditCampEvent bandits = banditEvent(simulation);
            WorldEventContext context = contextOf(simulation);
            setFunds(s.playerSettlement, 200);
            Assert.True(bandits.getWeight(context, s.playerNation) > 0);

            // bandits already outside: no second camp there
            WorldEffect camp = new WorldEffect(WorldEffectKind.BANDITS, s.playerNation.getId(), s.playerSettlement.getId(), 0, 100000, 100000);
            simulation.getWorldEvents().addEffect(camp);
            Assert.Equal(0, bandits.getWeight(context, s.playerNation));
            simulation.getWorldEvents().removeEffect(camp);

            // a town has a palisade
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType))) {
                while (s.playerSettlement.getTier() != SettlementTier.TOWN && s.playerSettlement.addBuilding(type)) {
                }
            }
            Assert.Equal(SettlementTier.TOWN, s.playerSettlement.getTier());
            Assert.Equal(0, bandits.getWeight(context, s.playerNation));
        }

        // --- the choices ----------------------------------------------------

        [Fact]
        public void testChoices_MilitiaNeedsMembers_BribeNeedsCoins_WaitingIsAlwaysThere() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            BanditCampEvent bandits = banditEvent(simulation);
            WorldEventContext context = contextOf(simulation);
            simulation.getPlayer().getInventory().setNumItems(ItemType.COIN, 0);
            setFunds(s.playerSettlement, 20);
            WorldEventInstance instance = new WorldEventInstance(BanditCampEvent.Id, 0, 0, true);
            instance.setNationId(s.playerNation.getId());
            instance.setSettlementId(s.playerSettlement.getId());

            List<WorldEventChoice> choices = bandits.getChoices(context, instance);
            Assert.Equal(3, choices.Count);
            Assert.True(choices[0].isAvailable());
            Assert.False(choices[1].isAvailable());
            Assert.Equal("Needs " + BanditCampEvent.BRIBE_COINS + " coins", choices[1].getUnavailableReason());
            Assert.True(choices[2].isAvailable());
            Assert.Contains("driven off or paid later", choices[2].getConsequence());
            Assert.Equal(2, bandits.getDefaultChoice(context, instance));

            setFunds(s.playerSettlement, BanditCampEvent.BRIBE_COINS);
            Assert.True(bandits.getChoices(context, instance)[1].isAvailable());

            // no nation behind it: nobody to turn out
            instance.setNationId(null);
            Assert.Equal("Needs " + BanditCampEvent.MILITIA_MEMBERS + " members", bandits.getChoices(context, instance)[0].getUnavailableReason());
        }

        [Fact]
        public void testAPawnLedNation_FightsWithMembersToSpare_ElsePaysIfItCan_ElseWaits() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            BanditCampEvent bandits = banditEvent(simulation);
            WorldEventContext context = contextOf(simulation);
            WorldEventInstance instance = new WorldEventInstance(BanditCampEvent.Id, 0, 0, false);
            instance.setNationId(s.friendly.getId());
            instance.setSettlementId(s.friendlySettlement.getId());
            Assert.Equal(DiplomacyShowcase.FriendlyMembers, s.friendly.getNumberOfMembers());

            setFunds(s.friendlySettlement, 0);
            Assert.Equal(2, bandits.chooseForNation(context, instance));
            setFunds(s.friendlySettlement, BanditCampEvent.BRIBE_COINS);
            Assert.Equal(1, bandits.chooseForNation(context, instance));

            // a fourth member: three can turn out and one stays home
            Pawn extra = new Pawn(s.friendlySettlement.getPosition(), "Extra", simulation.getRandom());
            simulation.getEntityRepository().addEntity(extra);
            contextOf(simulation).join(extra, s.friendly);
            Assert.Equal(0, bandits.chooseForNation(context, instance));
        }

        [Fact]
        public void testANationsOwnWait_LeavesACampThatRobsAndThenMovesOn() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            BanditCampEvent bandits = banditEvent(simulation);
            WorldEventContext context = contextOf(simulation);
            setFunds(s.friendlySettlement, 50);
            WorldEventInstance instance = new WorldEventInstance(BanditCampEvent.Id, 0, 0, false);
            instance.setNationId(s.friendly.getId());
            instance.setSettlementId(s.friendlySettlement.getId());
            string text = bandits.resolve(context, instance, 2);
            Assert.StartsWith("bandits made camp outside the ", text);
            Assert.True(simulation.getWorldEvents().hasBandits(s.friendlySettlement.getId()));

            // resolving the same wait again never puts up a second camp
            bandits.resolve(context, instance, 2);
            Assert.Single(simulation.getWorldEvents().getEffects());

            run(simulation, WorldEvents.BANDIT_THEFT_SECONDS * 50 + 1);
            Assert.True(s.friendlySettlement.getFunds() <= 50 - WorldEvents.BANDIT_THEFT_COINS);

            // the player does not lead it: it is not the player's to deal with
            Assert.False(simulation.getWorldEvents().offerStandingBanditCamp(simulation, s.friendlySettlement.getId()));
            Assert.Empty(simulation.getWorldEvents().getStandingBanditCamps(simulation));
        }

        [Fact]
        public void testResolve_ChangesNothing_WhenTheSettlementIsGone() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            BanditCampEvent bandits = banditEvent(simulation);
            WorldEventInstance instance = new WorldEventInstance(BanditCampEvent.Id, 0, 0, true);
            instance.setNationId(s.playerNation.getId());
            Assert.Null(bandits.resolve(contextOf(simulation), instance, 2));
            Assert.Empty(simulation.getWorldEvents().getEffects());
        }

        [Fact]
        public void testACamp_EndsQuietly_WhenItsSettlementIsGone() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            waitedOutCamp(simulation, s);
            int lines = worldEventLines(simulation).Count;
            s.playerSettlement.markForDeletion();
            run(simulation, 1);
            Assert.Empty(simulation.getWorldEvents().getEffects());
            Assert.Equal(lines, worldEventLines(simulation).Count);
        }

        // --- a camp the player waited on is still the player's to deal with ---

        [Fact]
        public void testAWaitedOutCamp_IsOfferedOnTheCommandBar_AndCanStillBeDrivenOff() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEvents events = simulation.getWorldEvents();
            Assert.Empty(events.getStandingBanditCamps(simulation));
            Assert.False(events.offerStandingBanditCamp(simulation, s.playerSettlement.getId()));

            waitedOutCamp(simulation, s);
            run(simulation, 50);
            List<Settlement> camps = events.getStandingBanditCamps(simulation);
            Assert.Single(camps);
            Assert.Same(s.playerSettlement, camps[0]);

            int numEvents = events.getNumEvents();
            int nextPlayerEvent = events.getNextPlayerEventTick();
            Assert.True(events.offerStandingBanditCamp(simulation, s.playerSettlement.getId()));
            WorldEventInstance pending = events.getPending();
            Assert.True(BanditCampEvent.isStandingCamp(pending));
            // off the event clock: not a new event
            Assert.Equal(numEvents, events.getNumEvents());
            Assert.Equal(nextPlayerEvent, events.getNextPlayerEventTick());
            // and not twice over
            Assert.False(events.offerStandingBanditCamp(simulation, s.playerSettlement.getId()));

            string description = events.describePending(simulation);
            Assert.StartsWith("Bandits are still camped in the woods outside the " + s.playerSettlement.getDisplayName(), description);
            Assert.EndsWith("they move on in about 15 minutes.", description);
            List<WorldEventChoice> choices = events.getPendingChoices(simulation);
            Assert.True(choices[0].isAvailable());
            Assert.True(choices[1].isAvailable());
            Assert.Equal("Keep the gates barred", choices[2].getLabel());

            float energy = simulation.getPlayer().getEnergy();
            string outcome = events.choose(simulation, 0);
            Assert.StartsWith("You raised a militia of " + s.playerNation.getName() + " and drove off the bandits", outcome);
            Assert.False(events.hasBandits(s.playerSettlement.getId()));
            Assert.Null(events.getPending());
            Assert.True(simulation.getPlayer().getEnergy() <= System.Math.Max(1f, energy - BanditCampEvent.MILITIA_ENERGY));
            Assert.Contains(outcome, worldEventLines(simulation));

            // gone: no more robbing, and nothing left on the bar
            run(simulation, WorldEvents.BANDIT_THEFT_SECONDS * 50 + 1);
            Assert.DoesNotContain("Bandits robbed", simulation.getPlayer().getStatus().getStatus());
            Assert.Empty(events.getEffects());
            Assert.Empty(events.getStandingBanditCamps(simulation));
        }

        [Fact]
        public void testAWaitedOutCamp_CanStillBePaidOff() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEvents events = simulation.getWorldEvents();
            waitedOutCamp(simulation, s);
            simulation.getPlayer().getInventory().setNumItems(ItemType.COIN, 40);
            Assert.True(events.offerStandingBanditCamp(simulation, s.playerSettlement.getId()));
            string outcome = events.choose(simulation, 1);
            Assert.Equal("You paid the bandits camped outside the " + s.playerSettlement.getDisplayName() + " "
                + BanditCampEvent.BRIBE_COINS + " coins to go away.", outcome);
            Assert.Equal(10, simulation.getPlayer().getInventory().getNumItems(ItemType.COIN));
            Assert.Empty(events.getEffects());
        }

        [Fact]
        public void testKeepingTheGatesBarred_LeavesTheCampOnItsClock_AndItCanBeTakenUpAgain() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEvents events = simulation.getWorldEvents();
            WorldEffect camp = waitedOutCamp(simulation, s);
            int endTick = camp.getEndTick();

            Assert.True(events.offerStandingBanditCamp(simulation, s.playerSettlement.getId()));
            Assert.StartsWith("You kept the gates of the ", events.choose(simulation, 2));
            Assert.Same(camp, events.findEffect(WorldEffectKind.BANDITS, null, s.playerSettlement.getId()));
            Assert.Equal(endTick, camp.getEndTick());

            // unanswered (a headless run), the default is to keep waiting
            // (the timeout brought forward, so the staged neighbours have no time to take the village)
            Assert.True(events.offerStandingBanditCamp(simulation, s.playerSettlement.getId()));
            Assert.Equal(simulation.getTickCounter().getTotalTicks() + WorldEvents.CHOICE_TIMEOUT_SECONDS * 50, events.getPending().getDeadlineTick());
            events.getPending().setDeadlineTick(simulation.getTickCounter().getTotalTicks() + 1);
            run(simulation, 2);
            Assert.Null(events.getPending());
            Assert.True(events.hasBandits(s.playerSettlement.getId()));
            Assert.Single(events.getEffects());
            Assert.Equal(endTick, camp.getEndTick());

            // and still dealt with after that
            Assert.True(events.offerStandingBanditCamp(simulation, s.playerSettlement.getId()));
            Assert.NotNull(events.choose(simulation, 0));
            Assert.False(events.hasBandits(s.playerSettlement.getId()));
        }

        [Fact]
        public void testACampThatMovesOnWhileTheChoiceIsOpen_LeavesNothingToPayOrFight() {
            Simulation simulation = newWorld();
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEvents events = simulation.getWorldEvents();
            WorldEffect camp = waitedOutCamp(simulation, s);
            Assert.True(events.offerStandingBanditCamp(simulation, s.playerSettlement.getId()));
            camp.setEndTick(simulation.getTickCounter().getTotalTicks() + 2);
            run(simulation, 3);
            Assert.False(events.hasBandits(s.playerSettlement.getId()));

            Assert.Equal("The bandits outside the " + s.playerSettlement.getDisplayName() + " have already broken camp and moved on.",
                events.describePending(simulation));
            List<WorldEventChoice> choices = events.getPendingChoices(simulation);
            Assert.Equal(BanditCampEvent.AlreadyGoneReason, choices[0].getUnavailableReason());
            Assert.Equal(BanditCampEvent.AlreadyGoneReason, choices[1].getUnavailableReason());
            Assert.Null(events.choose(simulation, 0));

            int coins = simulation.getPlayer().getInventory().getNumItems(ItemType.COIN);
            Assert.Null(events.choose(simulation, 2));
            Assert.Null(events.getPending());
            // keeping the gates barred against nobody does not bring them back
            Assert.Empty(events.getEffects());
            Assert.Equal(coins, simulation.getPlayer().getInventory().getNumItems(ItemType.COIN));
        }

        // --- saves ----------------------------------------------------------

        [Fact]
        public void testACampWaitedOnInASave_LoadsAndCanBeDrivenOff() {
            // the camp a save written before this fix holds: the same effect, in the same format
            Simulation original = newWorld();
            DiplomacyShowcase.Staged s = staged(original);
            waitedOutCamp(original, s);
            run(original, 100);

            Simulation loaded = SaveGame.decode(SaveGame.encode(original, "test"));
            WorldEvents events = loaded.getWorldEvents();
            List<Settlement> camps = events.getStandingBanditCamps(loaded);
            Assert.Single(camps);
            Assert.Equal(s.playerSettlement.getId(), camps[0].getId());
            Assert.True(events.offerStandingBanditCamp(loaded, camps[0].getId()));

            // the open choice about the standing camp saves as one too
            Simulation again = SaveGame.decode(SaveGame.encode(loaded, "test"));
            Assert.True(BanditCampEvent.isStandingCamp(again.getWorldEvents().getPending()));
            Assert.Equal(events.describePending(loaded), again.getWorldEvents().describePending(again));
            Assert.NotNull(again.getWorldEvents().choose(again, 0));
            Assert.False(again.getWorldEvents().hasBandits(s.playerSettlement.getId()));
            Assert.Empty(again.getWorldEvents().getEffects());
        }
    }
}
