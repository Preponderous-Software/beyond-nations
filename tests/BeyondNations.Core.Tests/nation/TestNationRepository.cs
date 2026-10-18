
using Xunit;

using beyondnations;
using EntityId = beyondnations.EntityId;

namespace beyondnationstests {

    public class TestNationRepository {
        private readonly RandomSource random = new RandomSource(20260816);



        [Fact]
        public void testInitialization() {
            // run
            NationRepository repository = new NationRepository(random);

            // verify
            Assert.NotNull(repository);
        }

        [Fact]
        public void testAddNation() {
            // prepare
            NationRepository repository = new NationRepository(random);
            EntityId leaderId = new EntityId();
            Nation nation = new Nation("Test Nation", leaderId, random);

            // run
            repository.addNation(nation);

            // verify
            Assert.Equal(nation, repository.getNation(nation.getId()));     
        }

        [Fact]
        public void testRemoveNation() {
            // prepare
            NationRepository repository = new NationRepository(random);
            EntityId leaderId = new EntityId();
            Nation nation = new Nation("Test Nation", leaderId, random);
            repository.addNation(nation);

            // run
            repository.removeNation(nation);

            // verify
            Assert.Null(repository.getNation(nation.getId()));
        }

        [Fact]
        public void testGetNationByNationId() {
            // prepare
            NationRepository repository = new NationRepository(random);
            EntityId leaderId = new EntityId();
            Nation nation = new Nation("Test Nation", leaderId, random);
            repository.addNation(nation);

            // run
            Nation retrievedNation = repository.getNation(nation.getId());

            // verify
            Assert.Equal(nation.getId(), retrievedNation.getId());
        }
    }
}