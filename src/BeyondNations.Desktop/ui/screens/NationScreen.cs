using System.Collections.Generic;
using ImGuiNET;

namespace beyondnations.desktop.ui.screens {

    /**
    * The nations screen, opened from the world with O (Realm on a touch
    * screen): every known nation, how big it is, and how it stands toward
    * the player's own. A player who leads a nation gets four buttons per
    * row -- send tribute, propose an alliance, declare war (or offer peace
    * once at odds), denounce -- which is the whole of the player's
    * diplomacy. A second tab, Laws, is the court: the nation's laws
    * (LawsPanel).
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
        public const string NationsTab = "Nations";
        public const string LawsTab = "Laws";

        private const float DesignButtonWidth = 96f;
        private const float DesignButtonGap = 8f;
        private const float DesignTabWidth = 120f;

        /** --start-screen laws opens the screen on its Laws tab. */
        public static bool startOnLaws = false;

        private readonly LawsPanel lawsPanel = new LawsPanel();
        private bool showingLaws = startOnLaws;

        /** One nation other than the player's, as a line of text and what can be done about it. */
        public class Row {
            public readonly Nation nation;
            public readonly string text;
            public readonly bool canAct;
            public readonly bool allied;
            public readonly bool atWar;
            /** At war or at odds: the War button becomes Peace. */
            public readonly bool canOfferPeace;

            public Row(Nation nation, string text, bool canAct, bool allied) : this(nation, text, canAct, allied, false, false) {
            }

            public Row(Nation nation, string text, bool canAct, bool allied, bool atWar, bool canOfferPeace) {
                this.nation = nation;
                this.text = text;
                this.canAct = canAct;
                this.allied = allied;
                this.atWar = atWar;
                this.canOfferPeace = canOfferPeace;
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
                lines.Add("Tribute " + Diplomacy.PLAYER_GIFT_COINS + " coins for +" + Diplomacy.giftValue(Diplomacy.PLAYER_GIFT_COINS)
                    + " (and a truce from a stronger foe). An alliance needs +" + NationRelations.ALLIANCE_REQUIRES + ". Hostile neighbours raid and besiege.");
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
            Realm realm = simulation.getRealm();
            Nation own = nationRepository.getNation(player.getNationId());
            bool leads = NationDiplomacy.playerLeads(player, nationRepository);

            foreach (Nation nation in nationRepository.getNations()) {
                if (nation == own) {
                    continue;
                }
                string text = nation.getName() + ": " + describeSize(simulation, nation);
                bool allied = false;
                bool atWar = false;
                bool peace = false;
                if (own != null) {
                    text += ". " + nationRepository.getRelations().describe(own, nation);
                    allied = nationRepository.getRelations().isAllied(own, nation);
                    atWar = realm.isAtWar(own, nation);
                    peace = OfferPeaceCommand.isApplicable(nationRepository, realm, own, nation);
                    if (atWar) {
                        text += ", at war";
                    }
                    int truce = realm.getTruceTicksLeft(own, nation);
                    if (truce > 0) {
                        text += ", truce " + (truce + TickCounter.DefaultTicksPerSecond - 1) / TickCounter.DefaultTicksPerSecond + " s";
                    }
                    if (diplomacy.areNeighbours(own, nation)) {
                        text += ", neighbour";
                    }
                }
                rows.Add(new Row(nation, text, leads, allied, atWar, peace));
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

        public bool isShowingLaws() {
            return showingLaws;
        }

        public void showLaws(bool laws) {
            showingLaws = laws;
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            float buttonWidth = UiScale.px(DesignButtonWidth);
            float buttonGap = UiScale.px(DesignButtonGap);
            float lineHeight = System.MathF.Max(UiScale.px(26f), height / 22f);
            float buttonHeight = System.MathF.Max(lineHeight - 4f, UiScale.px(30f));
            float left = System.MathF.Max(width * 0.04f, UiScale.px(16f));
            // below the corner Close on a touch screen (TouchLayout)
            float y = System.MathF.Max(System.MathF.Max(height / 18f, UiScale.px(12f)), Overlay.touchCloseBottom());

            Overlay.beginFullScreen("##nation-screen", width, height);
            Overlay.dimBackground(width, height);

            // the two tabs, as big buttons so a thumb finds them
            float tabWidth = System.MathF.Min(UiScale.px(DesignTabWidth), (width - left * 2f - buttonGap) / 2f);
            float tabsX = (width - tabWidth * 2f - buttonGap) / 2f;
            if (Overlay.buttonAt((showingLaws ? NationsTab : "> " + NationsTab + " <") + "##tab-nations", tabsX, y, tabWidth, buttonHeight)) {
                showingLaws = false;
            }
            if (Overlay.buttonAt((showingLaws ? "> " + LawsTab + " <" : LawsTab) + "##tab-laws", tabsX + tabWidth + buttonGap, y, tabWidth, buttonHeight)) {
                showingLaws = true;
            }
            y += buttonHeight + lineHeight * 0.9f;

            Player player = simulation.getPlayer();
            System.Action pending = showingLaws
                ? lawsPanel.draw(width, height, simulation, y, left, lineHeight, buttonGap, buttonHeight)
                : drawNations(width, height, simulation, y, left, lineHeight, buttonWidth, buttonGap, buttonHeight);

            // the answer goes on the status line, shown here too since the HUD is hidden
            string status = player.getStatus().getStatus();
            if (!string.IsNullOrEmpty(status)) {
                drawWrappedCentred(status, height / 34f, height - lineHeight * 3.1f, width, left, lineHeight * 0.75f, 3);
            }
            Overlay.textCentered(CloseHint, height / 34f, height - lineHeight * 0.7f, width);
            Overlay.end();
            // a finger has no O or Escape (TouchLayout)
            UiAction action = Overlay.touchCloseButton("nations", width) ? UiAction.ResumeWorld : UiAction.None;

            // run after the rows are drawn, so a nation disbanding cannot upset the loop
            pending?.Invoke();
            return action;
        }

        /** Centred text wrapped to the screen's width, at most maxLines lines. Returns the y after the last line. */
        internal static float drawWrappedCentred(string text, float fontSize, float y, float width, float margin, float step, int maxLines) {
            float scale = Overlay.fontScaleFor(fontSize);
            List<string> lines = TextWrap.wrap(text, width - margin * 2f, line => ImGui.CalcTextSize(line).X * scale);
            for (int i = 0; i < lines.Count && i < maxLines; i++) {
                Overlay.textCentered(lines[i], fontSize, y, width);
                y += step;
            }
            return y;
        }

        /** Left-aligned text wrapped to maxWidth, at most maxLines lines. Returns the y after the last line. */
        internal static float drawWrappedLeft(string text, float fontSize, float x, float y, float maxWidth, float step, int maxLines) {
            float scale = Overlay.fontScaleFor(fontSize);
            List<string> lines = TextWrap.wrap(text, maxWidth, line => ImGui.CalcTextSize(line).X * scale);
            for (int i = 0; i < lines.Count && i < maxLines; i++) {
                Overlay.textAtScaled(lines[i], fontSize, x, y);
                y += step;
            }
            return y;
        }

        private System.Action drawNations(float width, float height, Simulation simulation, float y, float left, float lineHeight,
                float buttonWidth, float buttonGap, float buttonHeight) {
            foreach (string line in getHeaderLines(simulation)) {
                y = drawWrappedCentred(line, height / 36f, y, width, left, lineHeight * 0.7f, 4);
            }
            y += lineHeight * 0.3f;

            List<Row> rows = getRows(simulation);
            if (rows.Count == 0) {
                Overlay.textCentered(NoOtherNations, height / 32f, y, width);
            }

            // four buttons to a row: beside the text on a wide screen, under it on a phone
            float buttonsWidth = 4f * buttonWidth + 3f * buttonGap;
            bool stacked = width - left * 2f - buttonsWidth < width * 0.45f;
            if (stacked) {
                buttonWidth = (width - left * 2f - 3f * buttonGap) / 4f;
                buttonsWidth = width - left * 2f;
            }
            float buttonsX = width - left - buttonsWidth;

            System.Action pending = null;
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            Realm realm = simulation.getRealm();
            float textWidth = stacked ? width - left * 2f : buttonsX - left - buttonGap;
            float textStep = lineHeight * 0.75f;
            for (int i = 0; i < rows.Count; i++) {
                Row row = rows[i];
                int textLines = System.Math.Min(3, TextWrap.wrap(row.text, textWidth,
                    line => ImGui.CalcTextSize(line).X * Overlay.fontScaleFor(height / 34f)).Count);
                float rowStep = stacked
                    ? textStep * textLines + buttonHeight + buttonGap * 2f
                    : System.MathF.Max(textStep * textLines + buttonGap, buttonHeight + buttonGap);
                if (y + rowStep > height - lineHeight * 3.2f) {
                    break;
                }
                float textBottom = drawWrappedLeft(row.text, height / 34f, left, y, textWidth, textStep, 3);
                if (row.canAct) {
                    float x = buttonsX;
                    float by = stacked ? textBottom : y;
                    Nation target = row.nation;
                    if (Overlay.buttonAt("Tribute##gift" + i, x, by, buttonWidth, buttonHeight)) {
                        pending = () => new GiftNationCommand(nationRepository, simulation.getDiplomacy(), realm).execute(player, target);
                    }
                    x += buttonWidth + buttonGap;
                    if (!row.allied && !row.atWar && Overlay.buttonAt("Ally##ally" + i, x, by, buttonWidth, buttonHeight)) {
                        pending = () => new ProposeAllianceCommand(nationRepository, realm).execute(player, target);
                    }
                    x += buttonWidth + buttonGap;
                    if (row.canOfferPeace) {
                        if (Overlay.buttonAt("Peace##peace" + i, x, by, buttonWidth, buttonHeight)) {
                            pending = () => new OfferPeaceCommand(nationRepository, realm).execute(player, target);
                        }
                    }
                    else if (Overlay.buttonAt("War##war" + i, x, by, buttonWidth, buttonHeight)) {
                        pending = () => new DeclareWarCommand(nationRepository, realm).execute(player, target);
                    }
                    x += buttonWidth + buttonGap;
                    if (Overlay.buttonAt("Denounce##denounce" + i, x, by, buttonWidth, buttonHeight)) {
                        pending = () => new DenounceNationCommand(nationRepository, simulation.getDiplomacy()).execute(player, target);
                    }
                }
                y += rowStep;
            }
            return pending;
        }
    }
}
