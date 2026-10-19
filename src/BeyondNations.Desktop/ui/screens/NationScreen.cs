using System.Collections.Generic;

namespace beyondnations.desktop.ui.screens {

    /**
    * The nations screen, opened from the world with O: every known nation,
    * how big it is, and how it stands toward the player's own. A player who
    * leads a nation gets three buttons per row -- send a gift of coins,
    * propose an alliance, denounce -- which is the whole of the player's
    * diplomacy.
    *
    * Like the inventory screen it is laid over the world, which stops while
    * it is open. The text is produced by getHeaderLines() and getRows(),
    * which need no window, so it is asserted in unit tests and only the
    * drawing needs a screen. O and Escape are read by the host.
    */
    public class NationScreen {
        public const string Title = "NATIONS";
        public const string CloseHint = "(Press O or ESCAPE to close)";
        public const string NoOtherNations = "No other nation has been founded yet.";

        private const float DesignButtonWidth = 96f;
        private const float DesignButtonGap = 8f;

        /** One nation other than the player's, as a line of text and what can be done about it. */
        public class Row {
            public readonly Nation nation;
            public readonly string text;
            public readonly bool canAct;
            public readonly bool allied;

            public Row(Nation nation, string text, bool canAct, bool allied) {
                this.nation = nation;
                this.text = text;
                this.canAct = canAct;
                this.allied = allied;
            }
        }

        public static List<string> getHeaderLines(Simulation simulation) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null) {
                lines.Add("You belong to no nation. Press N to found one, or J to join one.");
                return lines;
            }
            bool leads = own.getLeaderId().Equals(player.getId());
            lines.Add("Your nation: " + own.getName() + ", " + describeSize(simulation, own) + (leads ? ". You lead it." : "."));
            if (leads) {
                lines.Add("Gift " + Diplomacy.PLAYER_GIFT_COINS + " coins for +" + Diplomacy.giftValue(Diplomacy.PLAYER_GIFT_COINS)
                    + ". An alliance needs +" + NationRelations.ALLIANCE_REQUIRES + ". Hostile neighbours raid and besiege.");
            }
            else {
                lines.Add("Only the leader of " + own.getName() + " can speak for it.");
            }
            foreach (Settlement settlement in simulation.getDiplomacy().getSettlements(own)) {
                if (settlement.getSiegeText() != null) {
                    lines.Add("Your " + SettlementTiers.getDisplayName(settlement.getTier()).ToLowerInvariant() + ": " + settlement.getSiegeText() + "!");
                }
            }
            return lines;
        }

        public static List<Row> getRows(Simulation simulation) {
            List<Row> rows = new List<Row>();
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            Diplomacy diplomacy = simulation.getDiplomacy();
            Nation own = nationRepository.getNation(player.getNationId());
            bool leads = NationDiplomacy.playerLeads(player, nationRepository);

            foreach (Nation nation in nationRepository.getNations()) {
                if (nation == own) {
                    continue;
                }
                string text = nation.getName() + ": " + describeSize(simulation, nation);
                bool allied = false;
                if (own != null) {
                    text += ". " + nationRepository.getRelations().describe(own, nation);
                    allied = nationRepository.getRelations().isAllied(own, nation);
                    if (diplomacy.areNeighbours(own, nation)) {
                        text += ", neighbour";
                    }
                }
                rows.Add(new Row(nation, text, leads, allied));
            }
            return rows;
        }

        /** "5 members, 2 settlements (best: village)", or "1 member, no settlement". */
        public static string describeSize(Simulation simulation, Nation nation) {
            int members = nation.getNumberOfMembers();
            string text = members + (members == 1 ? " member" : " members");
            List<Settlement> settlements = simulation.getDiplomacy().getSettlements(nation);
            if (settlements.Count == 0) {
                return text + ", no settlement";
            }
            SettlementTier best = SettlementTier.CAMP;
            foreach (Settlement settlement in settlements) {
                if (settlement.getTier() > best) {
                    best = settlement.getTier();
                }
            }
            string tier = SettlementTiers.getDisplayName(best).ToLowerInvariant();
            if (settlements.Count == 1) {
                return text + ", 1 " + tier;
            }
            return text + ", " + settlements.Count + " settlements (best: " + tier + ")";
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            float buttonWidth = UiScale.px(DesignButtonWidth);
            float buttonGap = UiScale.px(DesignButtonGap);
            float lineHeight = System.MathF.Max(UiScale.px(26f), height / 22f);
            float y = height / 6f;
            float left = width * 0.06f;
            float buttonsX = width - left - 3f * buttonWidth - 2f * buttonGap;

            Overlay.beginFullScreen("##nation-screen", width, height);
            Overlay.dimBackground(width, height);
            Overlay.textCentered(Title, height / 12f, y, width);
            y += lineHeight * 1.5f;
            foreach (string line in getHeaderLines(simulation)) {
                Overlay.textCentered(line, height / 36f, y, width);
                y += lineHeight * 0.8f;
            }
            y += lineHeight * 0.5f;

            List<Row> rows = getRows(simulation);
            if (rows.Count == 0) {
                Overlay.textCentered(NoOtherNations, height / 32f, y, width);
                y += lineHeight;
            }

            System.Action pending = null;
            Player player = simulation.getPlayer();
            for (int i = 0; i < rows.Count && y < height - lineHeight * 2f; i++) {
                Row row = rows[i];
                Overlay.textAtScaled(row.text, height / 34f, left, y);
                if (row.canAct) {
                    float x = buttonsX;
                    float buttonHeight = lineHeight - 4f;
                    Nation target = row.nation;
                    if (Overlay.buttonAt("Gift " + Diplomacy.PLAYER_GIFT_COINS + "##gift" + i, x, y, buttonWidth, buttonHeight)) {
                        pending = () => new GiftNationCommand(simulation.getNationRepository(), simulation.getDiplomacy()).execute(player, target);
                    }
                    x += buttonWidth + buttonGap;
                    if (!row.allied && Overlay.buttonAt("Ally##ally" + i, x, y, buttonWidth, buttonHeight)) {
                        pending = () => new ProposeAllianceCommand(simulation.getNationRepository()).execute(player, target);
                    }
                    x += buttonWidth + buttonGap;
                    if (Overlay.buttonAt("Denounce##denounce" + i, x, y, buttonWidth, buttonHeight)) {
                        pending = () => new DenounceNationCommand(simulation.getNationRepository(), simulation.getDiplomacy()).execute(player, target);
                    }
                }
                y += lineHeight;
            }

            // the answer goes on the status line, shown here too since the HUD is hidden
            string status = player.getStatus().getStatus();
            if (!string.IsNullOrEmpty(status)) {
                Overlay.textCentered(status, height / 34f, height - lineHeight * 2.2f, width);
            }
            Overlay.textCentered(CloseHint, height / 34f, height - lineHeight, width);
            Overlay.end();

            // run after the rows are drawn, so a nation disbanding cannot upset the loop
            pending?.Invoke();
            return UiAction.None;
        }
    }
}
