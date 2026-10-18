using System.Collections.Generic;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The settlement the player is standing in, ported from
    * Assets/Scripts/ui/boxes/SettlementInfoBox.cs. The leader row says "You"
    * when the player leads, as before.
    */
    public class SettlementInfoBox : InfoBox {
        private readonly Settlement settlement;
        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;

        public SettlementInfoBox(Settlement settlement, NationRepository nationRepository, EntityRepository entityRepository) : base("Settlement Info") {
            this.settlement = settlement;
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public override List<string> getLines() {
            Nation nation = nationRepository.getNation(settlement.getNationId());

            List<string> lines = new List<string>(4);
            lines.Add("Nation: " + nation.getName());

            Entity leader = entityRepository.getEntity(nation.getLeaderId());
            if (leader is Player) {
                lines.Add("Leader: You");
            }
            else if (leader is Pawn) {
                lines.Add("Leader: " + ((Pawn) leader).getName());
            }
            else {
                lines.Add("Leader: " + leader.getType());
            }

            lines.Add("PCIS: " + settlement.getCurrentlyPresentEntitiesCount());
            lines.Add("Funds: " + settlement.getInventory().getNumItems(ItemType.COIN));

            return lines;
        }
    }
}
