
namespace beyondnations {

    public class NationLeaveCommand {
        private NationRepository nationRepository;
        private EventProducer eventProducer;
        private EntityRepository entityRepository;

        public NationLeaveCommand(NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            if (player.getNationId() == null) {
                player.getStatus().update("You are not a member of a nation.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getLeaderId() == player.getId()) {
                if (nation.getNumberOfMembers() == 1) {
                    if (player.isCurrentlyInSettlement()) {
                        player.getStatus().update("You cannot disband your nation while in a settlement.");
                        return;
                    }
                    deleteNation(nation, player);
                    return;
                }
                else {
                    transferLeadership(nation, player);
                    return;
                }
            }
            nation.removeMember(player.getId());
            player.setNationId(null);
            player.setColor(Rgba.White);
            eventProducer.produceNationLeaveEvent(nation, player.getId());
            player.getStatus().update("You left nation " + nation.getName() + ". Members: " + nation.getNumberOfMembers() + ".");
            releaseStallsAndHome(nation, player);
        }

        private void deleteNation(Nation nation, Player player) {
            nationRepository.removeNation(nation);
            player.setNationId(null);
            player.setColor(Rgba.White);
            eventProducer.produceNationDisbandEvent(nation);
            player.getStatus().update("You disbanded nation " + nation.getName() + ".");

            // remove settlements
            foreach (EntityId settlementId in nation.getSettlements()) {
                Settlement settlement = (Settlement) entityRepository.getEntity(settlementId);
                settlement.markForDeletion();
            }

            // clear settlements
            nation.getSettlements().Clear();

            player.setHomeSettlementId(null);
        }

        private void transferLeadership(Nation nation, Player player) {
            while (nation.getLeaderId() == player.getId()) {
                nation.setLeaderId(nation.getRandomMemberId());
                nation.setRole(nation.getLeaderId(), NationRole.LEADER);
            }
            eventProducer.produceLeadershipChangeEvent(nation, player.getId(), nation.getLeaderId());
            nation.removeMember(player.getId());
            player.setNationId(null);
            player.setColor(Rgba.White);
            eventProducer.produceNationLeaveEvent(nation, player.getId());
            player.getStatus().update("You left nation " + nation.getName() + ". Members: " + nation.getNumberOfMembers() + ".");
            releaseStallsAndHome(nation, player);
        }

        /**
        * Hands back every stall the player owns in the nation's settlements,
        * moving its stock into the player's inventory, and clears the
        * player's home settlement. Shared by both ways of leaving a nation
        * that leave the nation standing.
        */
        private void releaseStallsAndHome(Nation nation, Player player) {
            foreach (Settlement settlement in entityRepository.getEntitiesOfType(EntityType.SETTLEMENT)) {
                if (settlement.getNationId() != nation.getId()) {
                    continue;
                }
                Market market = settlement.getMarket();
                Stall stall = market.getStall(player.getId());
                if (stall != null) {
                    player.getInventory().transferContentsOfInventory(stall.getInventory());
                    stall.setOwnerId(null);
                }
            }

            player.setHomeSettlementId(null);
        }
    }
}