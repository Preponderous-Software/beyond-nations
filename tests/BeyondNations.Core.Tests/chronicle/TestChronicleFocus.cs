using System.Collections.Generic;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * "Your news" (ChronicleFocus): the chronicle lines about the player, the
    * player's nation, its neighbours and the world, and not the far-off AI
    * rivalries that buried them in the 0.6.2 playtest (docs/playtests/0.6.2,
    * s04: 495 entries by day 2).
    */
    public class TestChronicleFocus {

        private static int sequence;

        private static ChronicleEntry entry(EventType type, List<NationId> nations, List<EntityId> entities) {
            return new ChronicleEntry(sequence++, type, 0, 1, "dawn", "Something happened.", nations, entities);
        }

        private static ChronicleEntry between(Nation a, Nation b) {
            return entry(EventType.NationRelationChange, new List<NationId> { a.getId(), b.getId() }, new List<EntityId>());
        }

        /** The player's nation at the origin, a bordering one, and two far ones that only know each other. */
        private class World {
            public readonly DiplomacyFixture fixture = new DiplomacyFixture();
            public readonly Nation own;
            public readonly Nation border;
            public readonly Nation farA;
            public readonly Nation farB;

            public World() {
                own = fixture.playerNation(2);
                fixture.settlement(own, 0, 0);
                border = fixture.pawnNation("Westham", 2);
                fixture.settlement(border, 300, 0);
                farA = fixture.pawnNation("Aland", 2);
                fixture.settlement(farA, 6000, 0);
                farB = fixture.pawnNation("Borrow", 2);
                fixture.settlement(farB, 6000, 300);
            }

            public ChronicleFocus focus() {
                return ChronicleFocus.of(fixture.simulation);
            }
        }

        [Fact]
        public void testOwnNation_IsYourNews() {
            World world = new World();
            Assert.True(world.focus().includes(between(world.own, world.farA)));
            Assert.True(world.focus().includes(entry(EventType.Statecraft, new List<NationId> { world.own.getId() }, null)));
        }

        [Fact]
        public void testPlayer_IsYourNews() {
            World world = new World();
            Assert.True(world.focus().includes(entry(EventType.PlayerDeath, null, new List<EntityId> { world.fixture.player.getId() })));
        }

        [Fact]
        public void testBorderingNation_IsANeighbour() {
            World world = new World();
            ChronicleFocus focus = world.focus();
            Assert.True(focus.isNeighbour(world.border.getId()));
            // Its quarrels with a nation the player never met still count.
            Assert.True(focus.includes(between(world.border, world.farA)));
        }

        [Fact]
        public void testUnrelatedAiNations_AreNotYourNews() {
            World world = new World();
            ChronicleFocus focus = world.focus();
            Assert.False(focus.isNeighbour(world.farA.getId()));
            Assert.False(focus.includes(between(world.farA, world.farB)));
            ChronicleEntry raid = entry(EventType.NationRaid, new List<NationId> { world.farA.getId(), world.farB.getId() },
                new List<EntityId> { world.farA.getLeaderId() });
            Assert.False(focus.includes(raid));
        }

        [Fact]
        public void testAFarNationWithStanding_IsANeighbour() {
            World world = new World();
            world.fixture.relations.adjust(world.own, world.farA, NationRelations.RIVAL_AT);
            ChronicleFocus focus = world.focus();
            Assert.True(focus.isNeighbour(world.farA.getId()));
            Assert.True(focus.includes(between(world.farA, world.farB)));
            Assert.False(focus.isNeighbour(world.farB.getId()));
        }

        [Fact]
        public void testADenouncedFarNation_IsANeighbour() {
            World world = new World();
            world.fixture.relations.markDenounced(world.farB, world.own);
            Assert.True(world.focus().isNeighbour(world.farB.getId()));
        }

        [Fact]
        public void testFoundingsAndDisbandings_AreWorldNews() {
            World world = new World();
            ChronicleFocus focus = world.focus();
            ChronicleEntry founded = entry(EventType.NationCreation, new List<NationId> { world.farA.getId() }, new List<EntityId> { world.farA.getLeaderId() });
            ChronicleEntry fell = entry(EventType.NationDisband, new List<NationId> { world.farB.getId() }, null);
            Assert.True(ChronicleFocus.isWorldNews(founded));
            Assert.True(focus.includes(founded));
            Assert.True(focus.includes(fell));
            Assert.False(ChronicleFocus.isWorldNews(between(world.farA, world.farB)));
        }

        [Fact]
        public void testAWorldWideLine_IsWorldNews() {
            World world = new World();
            Assert.True(world.focus().includes(entry(EventType.WorldEvent, new List<NationId>(), new List<EntityId>())));
        }

        /**
        * An entry with no nations or entities, as one built without that
        * metadata reads (ChronicleEntry treats null as empty), is shown rather
        * than lost.
        */
        [Fact]
        public void testAnEntryWithoutMetadata_IsShown() {
            World world = new World();
            ChronicleEntry bare = new ChronicleEntry(7, EventType.NationRelationChange, 0, 1, "dawn", "Old news.", null, null);
            Assert.True(world.focus().includes(bare));
        }

        [Fact]
        public void testWithoutANation_ThePlayerAndWorldNewsOnly() {
            DiplomacyFixture fixture = new DiplomacyFixture();
            Nation a = fixture.pawnNation("Aland", 2);
            fixture.settlement(a, 0, 0);
            Nation b = fixture.pawnNation("Borrow", 2);
            fixture.settlement(b, 200, 0);
            fixture.relations.adjust(a, b, NationRelations.HOSTILE_AT);

            ChronicleFocus focus = ChronicleFocus.of(fixture.simulation);
            Assert.Null(fixture.player.getNationId());
            Assert.False(focus.isNeighbour(a.getId()));
            Assert.False(focus.includes(between(a, b)));
            Assert.True(focus.includes(entry(EventType.NationJoin, new List<NationId> { a.getId() }, new List<EntityId> { fixture.player.getId() })));
            Assert.True(focus.includes(entry(EventType.NationCreation, new List<NationId> { b.getId() }, null)));
        }

        [Fact]
        public void testRecent_TakesTheLastInFocus_OldestFirst() {
            World world = new World();
            ChronicleFocus focus = world.focus();
            ChronicleEntry first = between(world.own, world.farA);
            ChronicleEntry noise1 = between(world.farA, world.farB);
            ChronicleEntry second = between(world.border, world.farB);
            ChronicleEntry noise2 = between(world.farB, world.farA);
            List<ChronicleEntry> entries = new List<ChronicleEntry> { first, noise1, second, noise2 };

            Assert.Equal(new List<ChronicleEntry> { first, second }, focus.select(entries));
            Assert.Equal(new List<ChronicleEntry> { first, second }, focus.recent(entries, 5));
            Assert.Equal(new List<ChronicleEntry> { second }, focus.recent(entries, 1));
            Assert.Empty(focus.recent(entries, 0));
        }

        /** Real chronicle lines, recorded from events, filter the same way. */
        [Fact]
        public void testRecordedLines_FilterLikeBuiltOnes() {
            World world = new World();
            Simulation simulation = world.fixture.simulation;
            world.fixture.relations.adjust(world.farA, world.farB, NationRelations.RIVAL_AT);
            world.fixture.relations.adjust(world.border, world.farA, NationRelations.RIVAL_AT);
            IReadOnlyList<ChronicleEntry> entries = simulation.getChronicle().getEntries();
            List<ChronicleEntry> yours = ChronicleFocus.of(simulation).select(entries);

            Assert.Contains(entries, e => e.involvesNation(world.farA.getId()) && e.involvesNation(world.farB.getId()));
            Assert.DoesNotContain(yours, e => e.involvesNation(world.farA.getId()) && e.involvesNation(world.farB.getId()));
            Assert.Contains(yours, e => e.involvesNation(world.border.getId()) && e.involvesNation(world.farA.getId()));
        }
    }
}
