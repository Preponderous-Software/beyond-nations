using System;
using System.Reflection;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Ids compare by value with == and != as well as Equals: code across Core
    * writes stall.getOwnerId() == pawn.getId(), which must hold for two
    * objects naming the same entity or nation, not only for one object.
    */
    public class TestEntityId {
        private static T copyOf<T>(Guid guid) {
            return (T) Activator.CreateInstance(typeof(T), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { guid }, null);
        }

        [Fact]
        public void testTwoEntityIds_ForTheSameEntity_AreEqual() {
            EntityId id = new EntityId();
            EntityId copy = copyOf<EntityId>(id.getId());
            Assert.NotSame(id, copy);
            Assert.True(id == copy);
            Assert.False(id != copy);
            Assert.True(id.Equals(copy));
        }

        [Fact]
        public void testDifferentEntityIds_AndNull_AreNotEqual() {
            EntityId id = new EntityId();
            EntityId none = null;
            Assert.False(id == new EntityId());
            Assert.True(id != new EntityId());
            Assert.False(id == none);
            Assert.False(none == id);
            Assert.True(id != none);
            Assert.True(none == null);
            Assert.False(id.Equals(null));
        }

        [Fact]
        public void testTwoNationIds_ForTheSameNation_AreEqual() {
            NationId id = new NationId();
            NationId copy = copyOf<NationId>((Guid) typeof(NationId).GetMethod("getGuid", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(id, null));
            NationId none = null;
            Assert.NotSame(id, copy);
            Assert.True(id == copy);
            Assert.False(id != copy);
            Assert.False(id == new NationId());
            Assert.False(id == none);
            Assert.True(none == null);
        }
    }
}
