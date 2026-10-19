namespace beyondnations {

    /**
    * What a nation's leader can ask of one of its member pawns. Only the
    * player's own nation is in scope: these are the levers that make leading
    * a nation more than a title.
    */
    public static class LeaderPawnCommands {

        /** The player leads the nation the pawn belongs to (and the pawn is not the player). */
        public static bool playerLeads(Player player, Pawn pawn, NationRepository nationRepository) {
            if (player.getNationId() == null || pawn.getNationId() != player.getNationId()) {
                return false;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            return nation != null && nation.getLeaderId() == player.getId();
        }

        /** Whether the pawn's home settlement has a stall it could be handed. */
        public static bool canAppointMerchant(Player player, Pawn pawn, NationRepository nationRepository, EntityRepository entityRepository) {
            if (!playerLeads(player, pawn, nationRepository)) {
                return false;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getRole(pawn.getId()) != NationRole.SERF) {
                return false;
            }
            Settlement home = getHome(pawn, entityRepository);
            return home != null && home.getMarket().getNumStallsForSale() > 0;
        }

        public static Settlement getHome(Pawn pawn, EntityRepository entityRepository) {
            if (pawn.getHomeSettlementId() == null) {
                return null;
            }
            return entityRepository.getEntity(pawn.getHomeSettlementId()) as Settlement;
        }
    }

    /**
    * A leader orders a member pawn to gather for the nation. The pawn has to
    * know the leader (Acquaintance) to take orders; a stranger shrugs it off.
    * Delivery happens on its own as the pawn goes about its life -- see
    * NationOrderDelivery.
    */
    public class OrderPawnToGatherCommand {
        public const RelationshipTier TIER_REQUIRED = RelationshipTier.ACQUAINTANCE;

        private readonly NationRepository nationRepository;

        public OrderPawnToGatherCommand(NationRepository nationRepository) {
            this.nationRepository = nationRepository;
        }

        public void execute(Player player, Pawn pawn) {
            execute(player, pawn, ItemType.WOOD, NationOrder.DEFAULT_QUANTITY);
        }

        public void execute(Player player, Pawn pawn, ItemType itemType, int quantity) {
            if (!LeaderPawnCommands.playerLeads(player, pawn, nationRepository)) {
                player.getStatus().update("Only the leader of " + pawn.getName() + "'s nation can give it orders.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            string who = PawnRelations.describe(pawn, player);
            if (nation.getNumberOfSettlements() == 0) {
                player.getStatus().update(who + ": \"Gather for what? We have no settlement to bring it to.\"");
                return;
            }
            if (PawnRelations.getTier(pawn, player) < TIER_REQUIRED) {
                player.getStatus().update(who + ": \"I hardly know you. Why should I take your orders?\" (needs " + RelationshipTiers.getName(TIER_REQUIRED) + ")");
                return;
            }
            NationOrder current = pawn.getOrder();
            if (current != null && !current.isComplete() && current.getNationId() == nation.getId()) {
                player.getStatus().update(who + ": \"I'm already on it -- " + current.describe() + ".\"");
                return;
            }
            NationOrder order = new NationOrder(nation.getId(), itemType, quantity);
            pawn.setOrder(order);
            player.getStatus().update(who + ": \"I'll " + order.describeTask() + " for " + nation.getName() + ".\"");
        }
    }

    /**
    * A leader hands a member pawn a free stall in its home settlement,
    * making it a merchant. A stall costs a pawn a hundred coins, so this is
    * the biggest reward a leader can give, and the pawn remembers it.
    */
    public class AppointMerchantCommand {
        public const int RELATIONSHIP_GAIN = 6;

        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;
        private readonly EventProducer eventProducer;

        public AppointMerchantCommand(NationRepository nationRepository, EntityRepository entityRepository, EventProducer eventProducer) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
            this.eventProducer = eventProducer;
        }

        public void execute(Player player, Pawn pawn) {
            if (!LeaderPawnCommands.playerLeads(player, pawn, nationRepository)) {
                player.getStatus().update("Only the leader of " + pawn.getName() + "'s nation can appoint it.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getRole(pawn.getId()) != NationRole.SERF) {
                player.getStatus().update(pawn.getName() + " is already a " + nation.getRole(pawn.getId()).ToString().ToLower() + ".");
                return;
            }
            Settlement home = LeaderPawnCommands.getHome(pawn, entityRepository);
            if (home == null) {
                player.getStatus().update(pawn.getName() + " has no home settlement to keep a stall in.");
                return;
            }
            Stall stall = home.getMarket().getStallForSale();
            if (stall == null) {
                player.getStatus().update("There is no free stall in " + pawn.getName() + "'s settlement. Build one first.");
                return;
            }

            stall.setOwnerId(pawn.getId());
            nation.setRole(pawn.getId(), NationRole.MERCHANT);
            bool tierUp = PawnRelations.raise(pawn, player, RELATIONSHIP_GAIN, eventProducer);
            player.getStatus().update("You appointed " + pawn.getName() + " a merchant. " + PawnRelations.describe(pawn, player)
                + ": \"A stall of my own? I won't let you down!\"" + PawnRelations.tierUpSuffix(tierUp, pawn, player));
        }
    }
}
