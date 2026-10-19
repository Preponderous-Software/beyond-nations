using System;
using System.Collections.Generic;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * EventRepository publishes each added event to its subscribers,
    * synchronously, typed listeners first, and a throwing listener neither
    * loses the event nor starves the listeners after it.
    */
    public class TestEventSubscription {

        [Fact]
        public void testAllEventsListener_ReceivesEveryEvent() {
            EventRepository repository = new EventRepository();
            List<Event> received = new List<Event>();
            repository.subscribe(e => received.Add(e));

            Event chunk = new ChunkGenerateEvent(1, 2);
            Event otherChunk = new ChunkGenerateEvent(3, 4);
            repository.addEvent(chunk);
            repository.addEvent(otherChunk);

            Assert.Equal(new List<Event> { chunk, otherChunk }, received);
        }

        [Fact]
        public void testTypedListener_ReceivesOnlyItsType() {
            RandomSource random = new RandomSource(7);
            EventRepository repository = new EventRepository();
            List<Event> received = new List<Event>();
            repository.subscribe(EventType.NationCreation, e => received.Add(e));

            Nation nation = new Nation("Ironvale", new EntityId(), random);
            repository.addEvent(new ChunkGenerateEvent(0, 0));
            repository.addEvent(new NationCreationEvent(nation));

            Assert.Single(received);
            Assert.Equal(EventType.NationCreation, received[0].getType());
        }

        [Fact]
        public void testListenersRunAfterTheEventIsStored_TypedFirst() {
            EventRepository repository = new EventRepository();
            List<string> calls = new List<string>();
            repository.subscribe(e => calls.Add("all:" + repository.getNumberOfEvents(EventType.ChunkGenerate)));
            repository.subscribe(EventType.ChunkGenerate, e => calls.Add("typed:" + repository.getNumberOfEvents(EventType.ChunkGenerate)));

            repository.addEvent(new ChunkGenerateEvent(0, 0));

            Assert.Equal(new List<string> { "typed:1", "all:1" }, calls);
        }

        [Fact]
        public void testUnsubscribe_StopsDelivery() {
            EventRepository repository = new EventRepository();
            int all = 0;
            int typed = 0;
            Action<Event> allListener = e => all++;
            Action<Event> typedListener = e => typed++;
            repository.subscribe(allListener);
            repository.subscribe(EventType.ChunkGenerate, typedListener);
            Assert.Equal(2, repository.getNumberOfListeners());

            Assert.True(repository.unsubscribe(allListener));
            Assert.True(repository.unsubscribe(EventType.ChunkGenerate, typedListener));
            Assert.False(repository.unsubscribe(allListener));
            Assert.False(repository.unsubscribe(EventType.NationCreation, typedListener));
            repository.addEvent(new ChunkGenerateEvent(0, 0));

            Assert.Equal(0, all);
            Assert.Equal(0, typed);
            Assert.Equal(0, repository.getNumberOfListeners());
        }

        [Fact]
        public void testThrowingListener_DoesNotStopOthersOrLoseTheEvent() {
            EventRepository repository = new EventRepository();
            int after = 0;
            repository.subscribe(e => throw new InvalidOperationException("broken subscriber"));
            repository.subscribe(e => after++);

            repository.addEvent(new ChunkGenerateEvent(0, 0));

            Assert.Equal(1, after);
            Assert.Equal(1, repository.getTotalNumberOfEvents());
        }

        [Fact]
        public void testListener_MaySubscribeWhileBeingCalled() {
            EventRepository repository = new EventRepository();
            int late = 0;
            repository.subscribe(e => repository.subscribe(_ => late++));

            repository.addEvent(new ChunkGenerateEvent(0, 0));
            Assert.Equal(0, late); // added during publish, so not called for that event

            repository.addEvent(new ChunkGenerateEvent(1, 1));
            Assert.Equal(1, late);
        }

        [Fact]
        public void testNullListener_Throws() {
            EventRepository repository = new EventRepository();
            Assert.Throws<ArgumentNullException>(() => repository.subscribe(null));
            Assert.Throws<ArgumentNullException>(() => repository.subscribe(EventType.PawnDeath, null));
        }

        [Fact]
        public void testNewEventTypes_AreProduced() {
            RandomSource random = new RandomSource(7);
            EventRepository repository = new EventRepository();
            EventProducer producer = new EventProducer(repository);
            Nation nation = new Nation("Ironvale", new EntityId(), random);
            Settlement settlement = new Settlement(System.Numerics.Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            EntityId newLeader = new EntityId();

            producer.produceSettlementFoundedEvent(settlement, nation, nation.getLeaderId());
            producer.produceStallBuiltEvent(settlement, nation, nation.getLeaderId());
            producer.produceLeadershipChangeEvent(nation, nation.getLeaderId(), newLeader);

            Assert.Same(settlement, ((SettlementFoundedEvent) repository.getEvents(EventType.SettlementFounded)[0]).getSettlement());
            Assert.Same(nation, ((StallBuiltEvent) repository.getEvents(EventType.StallBuilt)[0]).getNation());
            LeadershipChangeEvent change = (LeadershipChangeEvent) repository.getEvents(EventType.LeadershipChange)[0];
            Assert.Equal(nation.getLeaderId(), change.getPreviousLeaderId());
            Assert.Equal(newLeader, change.getNewLeaderId());
        }
    }
}
