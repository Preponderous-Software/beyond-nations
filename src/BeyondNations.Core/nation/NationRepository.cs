using System.Collections.Generic;

namespace beyondnations {

    public class NationRepository {
        private RandomSource random;
        private Dictionary<NationId, Nation> nations;
        private List<NationId> nationIds;

        public NationRepository(RandomSource random) {
            this.random = random;
            nations = new Dictionary<NationId, Nation>();
            nationIds = new List<NationId>();
        }

        public Nation getNation(NationId id) {
            try {
                return nations[id];
            } catch (KeyNotFoundException) {
                return null;
            }
        }

        public Nation getNation(EntityId entityId) {
            foreach (Nation nation in nations.Values) {
                if (nation.getLeaderId().Equals(entityId)) {
                    return nation;
                }
            }

            return null;
        }

        public void addNation(Nation nation) {
            nations.Add(nation.getId(), nation);
            nationIds.Add(nation.getId());
        }

        public void removeNation(Nation nation) {
            nations.Remove(nation.getId());
            nationIds.Remove(nation.getId());
        }

        public int getNumberOfNations() {
            return nations.Count;
        }

        public Nation getRandomNation() {
            int randomIndex = random.range(0, nationIds.Count);
            NationId randomNationId = nationIds[randomIndex];
            return nations[randomNationId];
        }

        public List<Nation> getNations() {
            List<Nation> nationList = new List<Nation>();
            foreach (Nation nation in nations.Values) {
                nationList.Add(nation);
            }
            return nationList;
        }
    }
}