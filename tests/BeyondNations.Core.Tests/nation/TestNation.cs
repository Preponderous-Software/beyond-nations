
using Xunit;

using beyondnations;
using EntityId = beyondnations.EntityId;

namespace beyondnationstests {

    public class TestNation {
        private readonly RandomSource random = new RandomSource(20260816);



        [Fact]
        public void testInitialization() {
            // prepare
            string name = "Test Nation";
            EntityId leaderId = new EntityId();

            // run
            Nation nation = new Nation(name, leaderId, random);

            // verify
            Assert.NotNull(nation.getId());
            Assert.Equal(name, nation.getName());
            Assert.Equal(leaderId, nation.getLeaderId());
            Assert.Equal(1, nation.getNumberOfMembers());
            Assert.True(nation.isMember(leaderId));
            Rgba color = nation.getColor();
            Assert.InRange(color.r, 0f, 1f);
            Assert.InRange(color.g, 0f, 1f);
            Assert.InRange(color.b, 0f, 1f);
        }

        [Fact]
        public void testAddMember() {
            // prepare
            string name = "Test Nation";
            EntityId leaderId = new EntityId();
            Nation nation = new Nation(name, leaderId, random);

            // run
            EntityId memberId = new EntityId();
            nation.addMember(memberId);

            // verify
            Assert.Equal(2, nation.getNumberOfMembers());
            Assert.True(nation.isMember(memberId));
        }

        [Fact]
        public void testRemoveMember() {
            // prepare
            string name = "Test Nation";
            EntityId leaderId = new EntityId();
            Nation nation = new Nation(name, leaderId, random);

            // run
            EntityId memberId = new EntityId();
            nation.addMember(memberId);
            nation.removeMember(memberId);

            // verify
            Assert.Equal(1, nation.getNumberOfMembers());
            Assert.False(nation.isMember(memberId));
        }
    }
}