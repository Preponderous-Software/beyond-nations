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
        private readonly EntityRepository entityRepository;

        public PlayerInfoBox(Player player, NationRepository nationRepository) : this(player, nationRepository, null) {
        }

        /**
        * With the entity repository, the box also says whose land the player
        * is standing in.
        */
        public PlayerInfoBox(Player player, NationRepository nationRepository, EntityRepository entityRepository) : base("Player Info") {
            this.player = player;
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public override List<string> getLines() {
            List<string> lines = new List<string>(4);
            lines.Add("Energy: " + (int) player.getEnergy());
            if (isCompact()) {
                // the energy bar alone: the settlement box above already names the nation and leader
                return lines;
            }
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

            if (entityRepository != null) {
                lines.Add("Land: " + Territory.describeFor(player, entityRepository, nationRepository));
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
            // Drawn with the window's draw list and plain text, which the
            // shared UI already uses, rather than ImGui's progress bar.
            float energy = player.getEnergy();
            ImGuiStylePtr style = ImGui.GetStyle();
            float top = Overlay.frameHeight() + style.WindowPadding.Y;
            float barWidth = ImGui.GetWindowSize().X - style.WindowPadding.X * 2f;
            float barHeight = Overlay.frameHeight();
            Vector2 barPosition = new Vector2(style.WindowPadding.X, top);
            Overlay.fillLocalRect(barPosition, new Vector2(barWidth, barHeight), new Vector4(0.2f, 0.2f, 0.22f, 1f));
            Overlay.fillLocalRect(barPosition, new Vector2(barWidth * EnergyBar.getFraction(energy), barHeight), EnergyBar.getColour(energy));
            string label = EnergyBar.getLabel(energy);
            ImGui.SetCursorPos(new Vector2(
                barPosition.X + (barWidth - ImGui.CalcTextSize(label).X) / 2f,
                top + style.FramePadding.Y));
            ImGui.TextUnformatted(label);
            ImGui.SetCursorPos(new Vector2(style.WindowPadding.X, top + barHeight + style.ItemSpacing.Y));
            List<string> lines = getLines();
            for (int i = 1; i < lines.Count; i++) {
                ImGui.TextUnformatted(lines[i]);
            }
        }
    }
}
