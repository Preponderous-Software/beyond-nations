
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestStall {
        private readonly RandomSource random = new RandomSource(20260816);



        [Fact]
        public void testInstantiation() {
            Stall stall = new Stall();
            Assert.NotNull(stall);
            Assert.Null(stall.getOwnerId());
            Assert.NotNull(stall.getInventory());
        }

        [Fact]
        public void testSetOwnerId() {
            // prepare
            Stall stall = new Stall();
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);

            // execute
            stall.setOwnerId(pawn.getId());

            // verify
            Assert.Equal(pawn.getId(), stall.getOwnerId());
        
            // cleanup
        }
    }
}