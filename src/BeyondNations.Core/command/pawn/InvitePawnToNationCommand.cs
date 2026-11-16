namespace beyondnations {

    /**
    * Inviting a pawn into the player's nation. A nationless pawn accepts once
    * it knows the player (Acquaintance). A pawn already in another nation has
    * to be Loyal before it will defect, and its old nation loses a member --
    * its stall there is given up and it starts over without a home. A pawn
    * that leads its nation never defects, so no nation is left leaderless or
    * disbanded by an invitation and the leadership rules in Simulation keep
    * holding.
    */
    public class InvitePawnToNationCommand {
        public const RelationshipTier NATIONLESS_TIER_REQUIRED = RelationshipTier.ACQUAINTANCE;
        public const RelationshipTier RIVAL_TIER_REQUIRED = RelationshipTier.LOYAL;

        private readonly NationRepository nationRepository;
        private readonly EventProducer eventProducer;
        private readonly EntityRepository entityRepository;

        public InvitePawnToNationCommand(NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.entityRepository = entityRepository;
        }

        /** Whether an invitation is a meaningful question at all: the player has a nation and the pawn is not in it. */
        public static bool isApplicable(Player player, Pawn pawn) {
            return player.getNationId() != null && pawn.getNationId() != player.getNationId();
        }

        /**
        * Why an invitation would be turned down now, or null when the pawn
        * would accept: the reason on the greyed Invite button (0.8.0
        * playtest s02, where invitations had no button at all). It follows
        * execute's rules and never names a key.
        */
        public static string whyNot(Player player, Pawn pawn, NationRepository nationRepository) {
            Nation playerNation = player.getNationId() == null ? null : nationRepository.getNation(player.getNationId());
            if (playerNation == null) {
                return "Create or join a nation first, then invite people into it.";
            }
            if (pawn.getNationId() == playerNation.getId()) {
                return pawn.getName() + " is already one of " + playerNation.getName() + ".";
            }
            RelationshipTier tier = PawnRelations.getTier(pawn, player);
            Nation oldNation = pawn.getNationId() == null ? null : nationRepository.getNation(pawn.getNationId());
            if (oldNation == null) {
                if (tier < NATIONLESS_TIER_REQUIRED) {
                    return pawn.getName() + " doesn't know you yet. Talk to them or give a gift until they are an "
                        + RelationshipTiers.getName(NATIONLESS_TIER_REQUIRED) + ".";
                }
                return null;
            }
            if (oldNation.getLeaderId() == pawn.getId()) {
                return pawn.getName() + " leads " + oldNation.getName() + "; will never join.";
            }
            if (tier < RIVAL_TIER_REQUIRED) {
                return pawn.getName() + " belongs to " + oldNation.getName() + " and would leave it only for a "
                    + RelationshipTiers.getName(RIVAL_TIER_REQUIRED) + " friend.";
            }
            if (PawnRelations.getRelationship(pawn, player) < PawnTraits.relationshipToDefect(pawn)) {
                return pawn.getName() + " is Loyal by nature and won't leave " + oldNation.getName() + " yet.";
            }
            return null;
        }

        public void execute(Player player, Pawn pawn) {
            if (player.getNationId() == null) {
                player.getStatus().update("You need to be in a nation to invite anyone into it.");
                return;
            }
            Nation playerNation = nationRepository.getNation(player.getNationId());
            if (pawn.getNationId() == playerNation.getId()) {
                player.getStatus().update(pawn.getName() + " is already a member of " + playerNation.getName() + ".");
                return;
            }

            RelationshipTier tier = PawnRelations.getTier(pawn, player);
            string who = PawnRelations.describe(pawn, player);
            Nation oldNation = pawn.getNationId() == null ? null : nationRepository.getNation(pawn.getNationId());

            if (oldNation == null) {
                if (tier < NATIONLESS_TIER_REQUIRED) {
                    player.getStatus().update(who + ": \"I don't even know you.\" (needs " + RelationshipTiers.getName(NATIONLESS_TIER_REQUIRED) + ")");
                    return;
                }
            }
            else {
                if (oldNation.getLeaderId() == pawn.getId()) {
                    player.getStatus().update(who + ": \"I lead " + oldNation.getName() + ". I won't abandon my people.\"");
                    return;
                }
                if (tier < RIVAL_TIER_REQUIRED) {
                    player.getStatus().update(who + ": \"I'm loyal to " + oldNation.getName() + ".\" (needs " + RelationshipTiers.getName(RIVAL_TIER_REQUIRED) + ")");
                    return;
                }
                // a pawn with the Loyal trait needs far more than Loyal standing (PawnTraits)
                if (PawnRelations.getRelationship(pawn, player) < PawnTraits.relationshipToDefect(pawn)) {
                    player.getStatus().update(who + ": \"I think the world of you, but " + oldNation.getName() + " is my home. I won't turn my back on it.\" (Loyal by nature)");
                    return;
                }
                leaveOldNation(pawn, oldNation);
            }

            playerNation.addMember(pawn.getId());
            pawn.setNationId(playerNation.getId());
            pawn.setColor(playerNation.getColor());
            eventProducer.produceNationJoinEvent(playerNation, pawn.getId());

            string defection = oldNation == null ? "" : " left " + oldNation.getName() + " and";
            string resentment = "";
            if (oldNation != null) {
                // poaching a member is an affront to the nation it was taken from
                nationRepository.getRelations().adjust(playerNation, oldNation, Diplomacy.POACHING);
                resentment = " " + oldNation.getName() + " resents it: relations " + nationRepository.getRelations().describe(playerNation, oldNation) + ".";
            }
            player.getStatus().update(pawn.getName() + defection + " joined " + playerNation.getName() + "! Members: " + playerNation.getNumberOfMembers() + "." + resentment);
        }

        private void leaveOldNation(Pawn pawn, Nation oldNation) {
            foreach (EntityId settlementId in oldNation.getSettlements()) {
                Settlement settlement = (Settlement) entityRepository.getEntity(settlementId);
                if (settlement == null) {
                    continue;
                }
                foreach (Stall stall in settlement.getMarket().getStalls()) {
                    if (stall.getOwnerId() == pawn.getId()) {
                        stall.setOwnerId(null);
                    }
                }
            }
            oldNation.removeMember(pawn.getId());
            eventProducer.produceNationLeaveEvent(oldNation, pawn.getId());
            pawn.setHomeSettlementId(null);
            pawn.setOrder(null);
        }
    }
}
