using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    public class FoundSettlementCommand {
        private RandomSource random;
        private NationRepository nationRepository;
        private EventProducer eventProducer;
        private EntityRepository entityRepository;
        private GameConfig gameConfig;

        public FoundSettlementCommand(NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository, GameConfig gameConfig, RandomSource random) {
            this.random = random;
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.entityRepository = entityRepository;
            this.gameConfig = gameConfig;
        }

        public void execute(Player player) {
            // if not in a nation
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            // if not the leader of a nation
            if (nation.getLeaderId() != player.getId()) {
                player.getStatus().update("You are not the leader of a nation.");
                return;
            }
            // the era limits how many settlements a nation the player leads may hold (EraRules)
            if (!nation.canFoundSettlement()) {
                player.getStatus().update("Your nation may hold only " + nation.getSettlementLimit()
                    + (nation.getSettlementLimit() == 1 ? " settlement" : " settlements") + " in this era. "
                    + EraRules.describeSettlementCap(nation.getSettlementLimit()));
                return;
            }
            // the first costs wood; each one after it more wood, and stone (FoundingCost, #145)
            int woodCost = FoundingCost.wood(nation);
            int stoneCost = FoundingCost.stone(nation);
            int woodShort = woodCost - player.getInventory().getNumItems(ItemType.WOOD);
            int stoneShort = stoneCost - player.getInventory().getNumItems(ItemType.STONE);
            if (woodShort > 0 || stoneShort > 0) {
                string ordinal = nation.getNumberOfSettlements() == 0 ? "" : " another settlement";
                if (stoneShort <= 0) {
                    player.getStatus().update("Not enough wood" + (ordinal == "" ? "" : " for" + ordinal) + ". Need " + woodShort + " more.");
                }
                else {
                    player.getStatus().update("Founding" + ordinal + " takes " + FoundingCost.describe(nation) + ". Need "
                        + (woodShort > 0 ? woodShort + " more wood and " : "") + stoneShort + " more stone.");
                }
                return;
            }

            // never inside another nation's land, nor too near a settlement; the refusal says which way open ground lies
            string groundRefusal = explainGround(player, nation.getId());
            if (groundRefusal != null) {
                player.getStatus().update(groundRefusal);
                return;
            }

            player.getInventory().removeItem(ItemType.WOOD, woodCost);
            if (stoneCost > 0) {
                player.getInventory().removeItem(ItemType.STONE, stoneCost);
            }

            // create settlement
            Settlement settlement = new Settlement(player.getPosition(), player.getNationId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            bool first = nation.getNumberOfSettlements() == 0;
            nation.addSettlement(settlement.getId());
            // the leader's home stays where it was: a second settlement is an outpost, not a move
            if (first || !nation.getSettlements().Contains(player.getHomeSettlementId())) {
                player.setHomeSettlementId(settlement.getId());
            }
            List<Nation> offended = Territory.recordFounding(settlement, entityRepository, nationRepository);
            if (offended.Count == 0) {
                player.getStatus().update(first ? "Settlement founded." : "Settlement founded. Your nation now holds " + nation.getNumberOfSettlements() + " settlements.");
            }
            else {
                List<string> names = new List<string>();
                foreach (Nation neighbour in offended) {
                    names.Add(neighbour.getName());
                }
                player.getStatus().update("Settlement founded. " + string.Join(" and ", names) + " resent a new settlement so near their land.");
            }
            eventProducer.produceSettlementFoundedEvent(settlement, nation, player.getId());
        }

        /**
        * Why founder may not found a settlement where the player stands, with
        * the way to the nearest open ground, or null when the ground is open.
        * The open ground it names is pointed out to the player, so the world
        * holds it for them (#433). It runs the open-ground search, so it is
        * for a refusal or a tap on the greyed button (#521), not for every
        * frame: SettlementSites.describeWhyNotHere is the cheap reason.
        */
        public string explainGround(Player player, NationId founder) {
            int minDistance = gameConfig.getMinDistanceBetweenSettlements();
            Vector3 position = player.getPosition();
            string objection = Territory.getFoundingObjection(entityRepository, nationRepository, position, founder);
            if (objection != null) {
                Vector3? site = pointOut(player, SettlementSites.findNearestOpenSite(entityRepository, position, minDistance, founder));
                return objection + " Found your settlement outside its borders. " + SettlementSites.describeOpenSite(position, site);
            }
            Settlement tooClose = SettlementSites.findTooClose(entityRepository, position, minDistance);
            if (tooClose != null) {
                Vector3? site = pointOut(player, SettlementSites.findNearestOpenSite(entityRepository, position, minDistance, founder));
                return SettlementSites.describeTooClose(position, tooClose, minDistance, site);
            }
            return null;
        }

        /**
        * Tells the player which open ground the refusal names, so the world
        * holds it for them while they walk there (ReservedGround, #433).
        */
        private static Vector3? pointOut(Player player, Vector3? site) {
            if (site != null) {
                player.pointOutOpenGround(site.Value);
            }
            return site;
        }
    }
}