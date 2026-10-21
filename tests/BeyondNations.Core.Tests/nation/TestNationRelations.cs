using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestNationRelations {
        private readonly RandomSource random = new RandomSource(7);

        private Nation nation(string name) {
            return new Nation(name, new EntityId(), random);
        }

        [Fact]
        public void testNewPair_IsNeutralAtZero_AndSymmetric() {
            NationRelations relations = new NationRelations();
            Nation a = nation("A");
            Nation b = nation("B");
            Assert.Equal(0, relations.getScore(a, b));
            Assert.Equal(NationRelationState.NEUTRAL, relations.getState(a, b));

            relations.adjust(a, b, 12);
            Assert.Equal(12, relations.getScore(b, a));
        }

        [Theory]
        [InlineData(-100, NationRelationState.HOSTILE)]
        [InlineData(-50, NationRelationState.HOSTILE)]
        [InlineData(-49, NationRelationState.RIVAL)]
        [InlineData(-20, NationRelationState.RIVAL)]
        [InlineData(-19, NationRelationState.NEUTRAL)]
        [InlineData(19, NationRelationState.NEUTRAL)]
        [InlineData(20, NationRelationState.FRIENDLY)]
        [InlineData(100, NationRelationState.FRIENDLY)]
        public void testStateFor_Thresholds(int score, NationRelationState expected) {
            Assert.Equal(expected, NationRelations.stateFor(score, false));
            Assert.Equal(NationRelationState.ALLIED, NationRelations.stateFor(score, true));
        }

        [Fact]
        public void testAdjust_ClampsToRange() {
            NationRelations relations = new NationRelations();
            Nation a = nation("A");
            Nation b = nation("B");
            relations.adjust(a, b, 500);
            Assert.Equal(NationRelations.MAX_SCORE, relations.getScore(a, b));
            relations.adjust(a, b, -1000);
            Assert.Equal(NationRelations.MIN_SCORE, relations.getScore(a, b));
        }

        [Fact]
        public void testAlliance_EndsWhenScoreFallsBelowZero() {
            NationRelations relations = new NationRelations();
            Nation a = nation("A");
            Nation b = nation("B");
            relations.adjust(a, b, 30);
            Assert.True(relations.formAlliance(a, b));
            Assert.False(relations.formAlliance(a, b));
            Assert.Equal(NationRelationState.ALLIED, relations.getState(a, b));

            relations.adjust(a, b, -30);
            Assert.True(relations.isAllied(a, b));
            relations.adjust(a, b, -1);
            Assert.False(relations.isAllied(a, b));
            Assert.Equal(NationRelationState.NEUTRAL, relations.getState(a, b));
        }

        [Fact]
        public void testStateChanges_ProduceEvents() {
            EventRepository events = new EventRepository();
            NationRelations relations = new NationRelations();
            relations.setEventProducer(new EventProducer(events));
            Nation a = nation("Aland");
            Nation b = nation("Bshire");

            relations.adjust(a, b, -5);
            Assert.Equal(0, events.getTotalNumberOfEvents());
            relations.adjust(a, b, -20);
            relations.adjust(a, b, -30);
            relations.adjust(a, b, 90);
            relations.formAlliance(a, b);

            Assert.Equal(4, events.getNumberOfEvents(EventType.NationRelationChange));
            Assert.Equal("Aland and Bshire have become rivals.", events.getEvents(EventType.NationRelationChange)[0].getDescription());
            Assert.Equal("Aland and Bshire are now hostile.", events.getEvents(EventType.NationRelationChange)[1].getDescription());
            Assert.Equal("Aland and Bshire have formed an alliance.", events.getEvents(EventType.NationRelationChange)[3].getDescription());
        }

        [Fact]
        public void testRemovingANation_ForgetsItsRelations() {
            NationRepository repository = new NationRepository(random);
            Nation a = nation("A");
            Nation b = nation("B");
            repository.addNation(a);
            repository.addNation(b);
            repository.getRelations().adjust(a, b, -40);
            repository.getRelations().formAlliance(a, b);

            repository.removeNation(b);

            Assert.Equal(0, repository.getRelations().getScore(a, b));
            Assert.False(repository.getRelations().isAllied(a, b));
        }

        [Fact]
        public void testDescribe_ShowsStateAndSignedScore() {
            NationRelations relations = new NationRelations();
            Nation a = nation("A");
            Nation b = nation("B");
            relations.adjust(a, b, -27);
            Assert.Equal("Rival (-27)", relations.describe(a, b));
            relations.adjust(a, b, 60);
            Assert.Equal("Friendly (+33)", relations.describe(a, b));
        }
    }
}
