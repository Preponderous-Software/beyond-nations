using System.Collections.Generic;

namespace beyondnations {

    public class Nation {
        private RandomSource random;
        private NationId id;
        private string name;
        private EntityId leaderId;
        private List<EntityId> members = new List<EntityId>();
        private Dictionary<EntityId, NationRole> roles = new Dictionary<EntityId, NationRole>();
        private Rgba color;
        private List<EntityId> settlements = new List<EntityId>();

        public Nation(string name, EntityId leaderId, RandomSource random) {
            this.random = random;
            id = new NationId();
            this.name = name;
            this.leaderId = leaderId;
            members.Add(leaderId);
            roles[leaderId] = NationRole.LEADER;
            color = new Rgba(random.value(), random.value(), random.value());
        }

        public NationId getId() {
            return id;
        }

        public string getName() {
            return name;
        }

        public EntityId getLeaderId() {
            return leaderId;
        }

        public void setLeaderId(EntityId leaderId) {
            this.leaderId = leaderId;
        }

        public void addMember(EntityId memberId) {
            members.Add(memberId);
            roles[memberId] = NationRole.SERF;
        }

        public void removeMember(EntityId memberId) {
            members.Remove(memberId);
            roles.Remove(memberId);
        }

        public bool isMember(EntityId memberId) {
            return members.Contains(memberId);
        }

        public int getNumberOfMembers() {
            return members.Count;
        }

        public Rgba getColor() {
            return color;
        }

        public NationRole getRole(EntityId memberId) {
            return roles[memberId];
        }

        public void setRole(EntityId memberId, NationRole role) {
            roles[memberId] = role;
        }

        public EntityId getRandomMemberId() {
            int randomIndex = random.range(0, members.Count);
            return members[randomIndex];
        }

        public EntityId getOldestMemberId() {
            return members[0];
        }

        public List<EntityId> getSettlements() {
            return settlements;
        }

        public void addSettlement(EntityId settlementId) {
            settlements.Add(settlementId);
        }

        public void removeSettlement(EntityId settlementId) {
            settlements.Remove(settlementId);
        }

        public int getNumberOfSettlements() {
            return settlements.Count;
        }

        public EntityId getSettlement(int index) {
            return settlements[index];
        }

        public EntityId getRandomSettlementId() {
            int randomIndex = random.range(0, settlements.Count);
            return settlements[randomIndex];
        }
    }
}