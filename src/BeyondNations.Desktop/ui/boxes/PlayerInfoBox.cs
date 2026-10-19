using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

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

        /**
        * The energy line is drawn as a bar coloured by hunger (see EnergyBar),
        * with the number written on it; the remaining lines follow as text.
        * getLines() still starts with the energy line, so the text a test
        * reads is unchanged.
        */
        protected override void drawBody() {
            float energy = player.getEnergy();
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, EnergyBar.getColour(energy));
            ImGui.ProgressBar(EnergyBar.getFraction(energy), new Vector2(-1f, 0f), EnergyBar.getLabel(energy));
            ImGui.PopStyleColor();
            List<string> lines = getLines();
            for (int i = 1; i < lines.Count; i++) {
                ImGui.TextUnformatted(lines[i]);
            }
        }
    }
}
