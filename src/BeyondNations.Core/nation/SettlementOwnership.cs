namespace beyondnations {

    /**
    * Who holds a settlement within its nation (#176).
    *
    * Before, the leader of a nation held every settlement in it. A
    * settlement can now be given to another member: its funds, its
    * stockpile and the stalls built in it are then that member's to manage
    * (WithdrawSettlementFundsCommand, CollectSettlementStockpileCommand,
    * BuildStallCommand), and the leader takes it back to manage it again.
    * A settlement nobody has been given (Settlement.getOwnerId() is null) is
    * held by whoever leads the nation, so a change of leader moves it with
    * the crown, and a settlement given to a member stays theirs through
    * one.
    *
    * A holder who leaves the nation or dies gives the settlement back to the
    * leader; the simulation puts that right as it recounts the residents
    * (reconcile), and holderOf already reads it so in between. A captured
    * settlement is the new nation's leader's (Settlement.changeNation).
    */
    public static class SettlementOwnership {

        /**
        * Who holds the settlement now: its owner if they are still a member
        * of its nation, otherwise the nation's leader. Null without a nation.
        */
        public static EntityId holderOf(Settlement settlement, Nation nation) {
            if (nation == null) {
                return null;
            }
            EntityId owner = settlement.getOwnerId();
            return owner != null && nation.isMember(owner) ? owner : nation.getLeaderId();
        }

        /** Whether the settlement has been given to a member, rather than held by the leader. */
        public static bool isGranted(Settlement settlement, Nation nation) {
            EntityId owner = settlement.getOwnerId();
            return nation != null && owner != null && nation.isMember(owner) && owner != nation.getLeaderId();
        }

        /** Whether this member may manage the settlement: it is their nation's, and they hold it. */
        public static bool mayManage(EntityId memberId, Nation nation, Settlement settlement) {
            return nation != null && settlement.getNationId() == nation.getId() && holderOf(settlement, nation) == memberId;
        }

        /**
        * Why the player may not manage a settlement of their own nation, or
        * null when they may: "Ada Smith holds this settlement." or "Only the
        * leader of Xland, or a member it is given to, manages it."
        */
        public static string refusal(EntityId memberId, Nation nation, Settlement settlement, EntityRepository entityRepository) {
            if (mayManage(memberId, nation, settlement)) {
                return null;
            }
            if (nation == null || settlement.getNationId() != nation.getId()) {
                return "You do not own this settlement.";
            }
            EntityId holder = holderOf(settlement, nation);
            Entity entity = holder == null ? null : entityRepository.getEntity(holder);
            if (nation.getLeaderId() == memberId && entity != null) {
                return entity.getName() + " holds this settlement. Take it back on the settlement screen to manage it.";
            }
            return "Only the leader of " + nation.getName() + ", or a member it is given to, manages this settlement.";
        }

        /**
        * Gives a settlement back to its nation's leader when its holder is no
        * longer a member, and keeps the nametag's name in step with the
        * holder's (a ruler's heir takes the ruler's place under their own
        * name). Returns whether it changed anything.
        */
        public static bool reconcile(Settlement settlement, Nation nation, EntityRepository entityRepository) {
            EntityId owner = settlement.getOwnerId();
            if (owner == null) {
                return false;
            }
            Entity entity = entityRepository.getEntity(owner);
            if (nation == null || !nation.isMember(owner) || entity == null || entity.isMarkedForDeletion()) {
                settlement.setOwner(null, null);
                return true;
            }
            if (entity.getName() != settlement.getOwnerName()) {
                settlement.setOwner(owner, entity.getName());
                return true;
            }
            return false;
        }
    }
}
