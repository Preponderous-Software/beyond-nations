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

        public override string getChipText() { return "Settlement"; }

        /** Under siege, the collapsed chip says so. */
        public override HudBadge getBadge() {
            return settlement.getSiegeText() != null ? HudBadge.Danger : HudBadge.None;
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
            // given to a member (#176): whose it is, when not the leader's
            if (SettlementOwnership.isGranted(settlement, nation)) {
                EntityId holder = SettlementOwnership.holderOf(settlement, nation);
                Entity held = entityRepository.getEntity(holder);
                lines.Add("Held by: " + (held is Player ? "You" : held != null ? held.getName() : "no one"));
            }

            // no "PCIS" (pawns currently in settlement): the floating label
            // says "N here", and the F1 debug box keeps the count (#350)
            lines.Add("Funds: " + settlement.getInventory().getNumItems(ItemType.COIN));
            lines.Add("Tier: " + SettlementTiers.getDisplayName(settlement.getTier()) + " (" + settlement.getNumBuildings() + " buildings)");
            lines.Add("Residents: " + settlement.getResidentCount() + "/" + settlement.getResidentCap());
            BuildingType? next = BuildingPlanner.suggestNext(settlement);
            if (next != null) {
                lines.Add((TouchLayout.isActive() ? "Next: " : ObjectiveKeyNames.resolve("Next ({BuildBuilding}): ")) + BuildingCatalog.getDisplayName(next.Value) + ", " + BuildingCatalog.describeCost(next.Value));
            }

            return lines;
        }
    }
}
