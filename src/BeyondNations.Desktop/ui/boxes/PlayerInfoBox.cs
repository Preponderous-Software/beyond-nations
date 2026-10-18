using System.Collections.Generic;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The player's own summary, ported from
    * Assets/Scripts/ui/boxes/PlayerInfoBox.cs. Energy is truncated to a whole
    * number as it was, and a player with no nation still gets both rows, saying
    * "none", so the box does not change height as the player joins and leaves.
    */
    public class PlayerInfoBox : InfoBox {
        private readonly Player player;
        private readonly NationRepository nationRepository;

        public PlayerInfoBox(Player player, NationRepository nationRepository) : base("Player Info") {
            this.player = player;
            this.nationRepository = nationRepository;
        }

        public override List<string> getLines() {
            List<string> lines = new List<string>(4);
            lines.Add("Energy: " + (int) player.getEnergy());
            lines.Add("Relationships: " + player.getRelationships().Count);

            if (player.getNationId() != null) {
                Nation nation = nationRepository.getNation(player.getNationId());
                lines.Add("Nation: " + nation.getName());
                lines.Add("Role: " + nation.getRole(player.getId()));
            }
            else {
                lines.Add("Nation: none");
                lines.Add("Role: none");
            }

            return lines;
        }
    }
}
