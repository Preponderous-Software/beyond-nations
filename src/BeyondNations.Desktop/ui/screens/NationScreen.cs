using System.Collections.Generic;
using ImGuiNET;

namespace beyondnations.desktop.ui.screens {

    /**
    * The nations screen, opened from the world with O (Realm on a touch
    * screen): every known nation, how big it is, and how it stands toward
    * the player's own. A player who leads a nation gets up to five buttons
    * per row, each in a fixed slot -- send tribute, propose an alliance,
    * offer peace, declare war, denounce -- offered by the same rules as the
    * command bar (NationDiplomacy), which is the whole of the player's
    * diplomacy. A second tab, Laws, is the court: the nation's laws
    * (LawsPanel).
    *
    * The list puts the nations that matter first -- at war, at odds,
    * neighbours, allies, then the rest nearest first -- and scrolls when
    * there are more than fit: the mouse wheel, Up/Down and Page Up/Page
    * Down (read by the host), a drag across the list, or the Up/Down
    * buttons beside the count line for a thumb.
    *
    * Like the inventory screen it is laid over the world, which stops while
    * it is open. The text is produced by getHeaderLines() and getRows(),
    * which need no window, so it is asserted in unit tests and only the
    * drawing needs a screen. O and Escape are read by the host.
    */
    public class NationScreen {
        public const string Title = "NATIONS";
        public const string CloseHint = "(Press {ToggleNations} or {Pause} to close)";
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
        /** The first row of the list on screen; the list scrolls when the nations outnumber the screen. */
        private int firstRow;
        // War and Denounce take two clicks, as on the command bar (#368)
        private readonly ConfirmGate confirmGate = new ConfirmGate();
        // the row order shown when the screen opened; kept until it is
        // closed, so a click never re-sorts a row out from under the pointer
        private readonly List<NationId> shownOrder = new List<NationId>();
        // when the screen was last drawn: a longer gap means it was closed
        private double lastDrawn = double.NegativeInfinity;
        private const double ReopenGapSeconds = 1.0;
        // "Confirm Denounce" would not fit a button; the status line names the action
        public const string ArmedLabel = "Confirm";
        /** How far a drag across the list has gone toward the next row. */
        private float dragCarry;

        /** One nation other than the player's, as a line of text and what can be done about it. */
        public class Row {
            public readonly Nation nation;
            public readonly string text;
            public readonly bool canAct;
            public readonly bool allied;
            public readonly bool atWar;
            /** At war or at odds (OfferPeaceCommand.isApplicable): Peace is offered. */
            public readonly bool canOfferPeace;
            /** Not allied, at war or at odds (NationDiplomacy.canProposeAlliance): Ally is offered. */
            public readonly bool canProposeAlliance;
            /** Not already at war (NationDiplomacy.canDeclareWar): War is offered, to a Rival too. */
            public readonly bool canDeclareWar;
            /** How much the nation matters to the player, most first: 0 at war, 1 at odds, 2 neighbour, 3 allied, 4 the rest. */
            public readonly int rank;
            /** From the player to the nation's nearest settlement; MaxValue for a nation without one. */
            public readonly float distance;

            public Row(Nation nation, string text, bool canAct, bool allied, bool atWar, bool canOfferPeace,
                    bool canProposeAlliance, bool canDeclareWar, int rank, float distance) {
                this.nation = nation;
                this.text = text;
                this.canAct = canAct;
                this.allied = allied;
                this.atWar = atWar;
                this.canOfferPeace = canOfferPeace;
                this.canProposeAlliance = canProposeAlliance;
                this.canDeclareWar = canDeclareWar;
                this.rank = rank;
                this.distance = distance;
            }
        }

        private const int RankAtWar = 0;
        private const int RankAtOdds = 1;
        private const int RankNeighbour = 2;
        private const int RankAllied = 3;
        private const int RankRest = 4;

        public static List<string> getHeaderLines(Simulation simulation) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null) {
                lines.Add(ObjectiveKeyNames.resolve("You belong to no nation. Press {CreateNewNation} to found one, or {JoinNation} to join one."));
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
                bool ally = false;
                bool war = false;
                int rank = RankRest;
                if (own != null) {
                    NationRelations relations = nationRepository.getRelations();
                    text += ". " + relations.describe(own, nation);
                    allied = relations.isAllied(own, nation);
                    atWar = realm.isAtWar(own, nation);
                    peace = OfferPeaceCommand.isApplicable(nationRepository, realm, own, nation);
                    ally = NationDiplomacy.canProposeAlliance(nationRepository, realm, own, nation);
                    war = NationDiplomacy.canDeclareWar(realm, own, nation);
                    if (atWar) {
                        text += ", at war";
                    }
                    int truce = realm.getTruceTicksLeft(own, nation);
                    if (truce > 0) {
                        text += ", truce " + (truce + TickCounter.DefaultTicksPerSecond - 1) / TickCounter.DefaultTicksPerSecond + " s";
                    }
                    bool neighbour = diplomacy.areNeighbours(own, nation);
                    if (neighbour) {
                        text += ", neighbour";
                    }
                    rank = atWar ? RankAtWar
                        : relations.getState(own, nation) <= NationRelationState.RIVAL ? RankAtOdds
                        : neighbour ? RankNeighbour
                        : allied ? RankAllied
                        : RankRest;
                }
                rows.Add(new Row(nation, text, leads, allied, atWar, peace, ally, war, rank, distanceTo(simulation, nation)));
            }
            // the nations that matter first (playtest 0.4.1: with ~20 nations the
            // oldest filled the screen and the player's real neighbours were never listed)
            rows.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank)
                : a.distance != b.distance ? a.distance.CompareTo(b.distance)
                : string.CompareOrdinal(a.nation.getName(), b.nation.getName()));
            return rows;
        }

        /**
        * The rows in the order last shown, with any nation not seen before at
        * the end in its sorted place; order is updated to match. An empty
        * order takes the rows' own (sorted) order.
        */
        public static List<Row> keepOrder(List<Row> rows, List<NationId> order) {
            List<Row> kept = new List<Row>(rows.Count);
            foreach (NationId id in order) {
                Row row = rows.Find(r => r.nation.getId() == id);
                if (row != null) {
                    kept.Add(row);
                }
            }
            foreach (Row row in rows) {
                if (!kept.Contains(row)) {
                    kept.Add(row);
                }
            }
            order.Clear();
            foreach (Row row in kept) {
                order.Add(row.nation.getId());
            }
            return kept;
        }

        /** "Click again to declare war on Blackport." -- and to break a truce, when there is one. */
        public static string getWarPrompt(Realm realm, Nation own, Nation target, bool touch) {
            string click = touch ? "Tap" : "Click";
            return own != null && realm.getTruceTicksLeft(own, target) > 0
                ? click + " War again to break the truce with " + target.getName() + " and declare war."
                : click + " War again to declare war on " + target.getName() + ".";
        }

        /** From the player to the nation's nearest settlement, on the ground; MaxValue without one. */
        private static float distanceTo(Simulation simulation, Nation nation) {
            float nearest = float.MaxValue;
            System.Numerics.Vector3 position = simulation.getPlayer().getPosition();
            foreach (Settlement settlement in simulation.getDiplomacy().getSettlements(nation)) {
                nearest = System.MathF.Min(nearest, Territory.groundDistance(position, settlement.getPosition()));
            }
            return nearest;
        }

        /**
        * The line over the list: which rows are shown, of how many, and how
        * to see the rest. "Nations 1-7 of 20, neighbours and enemies first.
        * Scroll for more."
        */
        public static string describeShown(int firstRow, int shown, int total, bool touch) {
            string order = "neighbours and enemies first.";
            if (shown >= total) {
                return total + (total == 1 ? " other nation, " : " other nations, ") + order;
            }
            return "Nations " + (firstRow + 1) + "-" + (firstRow + shown) + " of " + total + ", " + order
                + (touch ? " Drag or tap Up/Down for more." : " Scroll for more.");
        }

        /** The first row to show, kept so that the list never scrolls past its ends. */
        public static int clampFirstRow(int firstRow, int total, int visible) {
            return System.Math.Clamp(firstRow, 0, System.Math.Max(0, total - System.Math.Max(1, visible)));
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
            string status = ObjectiveKeyNames.resolve(player.getStatus().getStatus());
            if (!string.IsNullOrEmpty(status)) {
                drawWrappedCentred(status, height / 34f, height - lineHeight * 3.1f, width, left, lineHeight * 0.75f, 3);
            }
            Overlay.textCentered(ObjectiveKeyNames.closeHint(CloseHint), height / 34f, height - lineHeight * 0.7f, width);
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

        /**
        * Rows to move the list by, asked for by the host from the keyboard
        * (Up/Down, Page Up/Page Down) and taken on the next frame.
        */
        private static int pendingScroll;

        public static void scroll(int rows) {
            pendingScroll += rows;
        }

        /** Rows that Page Up and Page Down move the list by. */
        public const int PageRows = 5;

        /** Up/Down move the list a row, Page Up/Page Down a page; the host calls this while the screen is open. */
        public static void readScrollKeys(beyondnations.desktop.input.InputService input) {
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.Up)) {
                scroll(-1);
            }
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.Down)) {
                scroll(1);
            }
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.PageUp)) {
                scroll(-PageRows);
            }
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.PageDown)) {
                scroll(PageRows);
            }
        }

        private System.Action drawNations(float width, float height, Simulation simulation, float y, float left, float lineHeight,
                float buttonWidth, float buttonGap, float buttonHeight) {
            foreach (string line in getHeaderLines(simulation)) {
                y = drawWrappedCentred(line, height / 36f, y, width, left, lineHeight * 0.7f, 4);
            }
            y += lineHeight * 0.3f;

            double now = ImGui.GetTime();
            if (now - lastDrawn > ReopenGapSeconds) {
                shownOrder.Clear();
                confirmGate.disarm();
            }
            lastDrawn = now;
            // a "Click War/Denounce again" prompt whose window has lapsed is taken back
            confirmGate.update(now, simulation.getPlayer().getStatus());
            List<Row> rows = keepOrder(getRows(simulation), shownOrder);
            if (rows.Count == 0) {
                Overlay.textCentered(NoOtherNations, height / 32f, y, width);
            }

            // five slots to a row, each always in the same place so a button
            // never slides under the pointer: beside the text on a wide
            // screen, under it on a phone
            const int slots = 5;
            float buttonsWidth = slots * buttonWidth + (slots - 1) * buttonGap;
            bool stacked = width - left * 2f - buttonsWidth < width * 0.45f;
            if (stacked) {
                buttonWidth = (width - left * 2f - (slots - 1) * buttonGap) / slots;
                buttonsWidth = width - left * 2f;
            }
            float buttonsX = width - left - buttonsWidth;
            float textWidth = stacked ? width - left * 2f : buttonsX - left - buttonGap;
            float textStep = lineHeight * 0.75f;
            float fontSize = height / 34f;

            // the count line and Up/Down sit above the list
            float listTop = y + buttonHeight + buttonGap;
            float listBottom = height - lineHeight * 3.2f;
            float[] steps = new float[rows.Count];
            for (int i = 0; i < rows.Count; i++) {
                int textLines = System.Math.Min(3, TextWrap.wrap(rows[i].text, textWidth,
                    line => ImGui.CalcTextSize(line).X * Overlay.fontScaleFor(fontSize)).Count);
                steps[i] = stacked
                    ? textStep * textLines + buttonHeight + buttonGap * 2f
                    : System.MathF.Max(textStep * textLines + buttonGap, buttonHeight + buttonGap);
            }

            // scrolling: the wheel, the keys (via the host), a drag across the list, or Up/Down
            ImGuiIOPtr io = ImGui.GetIO();
            int move = pendingScroll;
            pendingScroll = 0;
            if (io.MouseWheel != 0f) {
                move += io.MouseWheel > 0f ? -1 : 1;
            }
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && io.MousePos.Y >= listTop && io.MousePos.Y <= listBottom) {
                dragCarry -= io.MouseDelta.Y;
                float rowHeight = System.MathF.Max(buttonHeight, lineHeight);
                while (dragCarry >= rowHeight) {
                    move++;
                    dragCarry -= rowHeight;
                }
                while (dragCarry <= -rowHeight) {
                    move--;
                    dragCarry += rowHeight;
                }
            }
            else {
                dragCarry = 0f;
            }
            int lastPage = countFittingFromEnd(steps, listBottom - listTop);
            firstRow = clampFirstRow(firstRow + move, rows.Count, lastPage);
            int visible = countFitting(steps, firstRow, listBottom - listTop);

            if (rows.Count > 0) {
                bool overflow = visible < rows.Count;
                float arrowWidth = overflow ? System.MathF.Min(UiScale.px(80f), (width - left * 2f) / 5f) : 0f;
                float lineWidth = width - (overflow ? (arrowWidth + buttonGap) * 2f : 0f);
                drawWrappedCentred(describeShown(firstRow, visible, rows.Count, TouchLayout.isActive()), height / 38f,
                    y + (buttonHeight - lineHeight * 0.7f) * 0.5f, lineWidth, left, lineHeight * 0.7f, 1);
                if (overflow) {
                    float ax = width - left - arrowWidth * 2f - buttonGap;
                    if (Overlay.buttonAt("Up##nations-up", ax, y, arrowWidth, buttonHeight)) {
                        firstRow = clampFirstRow(firstRow - System.Math.Max(1, visible - 1), rows.Count, lastPage);
                    }
                    if (Overlay.buttonAt("Down##nations-down", ax + arrowWidth + buttonGap, y, arrowWidth, buttonHeight)) {
                        firstRow = clampFirstRow(firstRow + System.Math.Max(1, visible - 1), rows.Count, lastPage);
                    }
                    visible = countFitting(steps, firstRow, listBottom - listTop);
                }
            }
            y = listTop;

            System.Action pending = null;
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            Realm realm = simulation.getRealm();
            int last = System.Math.Min(rows.Count, firstRow + visible);
            for (int i = firstRow; i < last; i++) {
                Row row = rows[i];
                float textBottom = drawWrappedLeft(row.text, fontSize, left, y, textWidth, textStep, 3);
                if (row.canAct) {
                    float x = buttonsX;
                    float by = stacked ? textBottom : y;
                    float slot = buttonWidth + buttonGap;
                    Nation target = row.nation;
                    if (Overlay.buttonAt("Tribute##gift" + i, x, by, buttonWidth, buttonHeight)) {
                        pending = () => new GiftNationCommand(nationRepository, simulation.getDiplomacy(), realm).execute(player, target);
                    }
                    if (row.canProposeAlliance && Overlay.buttonAt("Ally##ally" + i, x + slot, by, buttonWidth, buttonHeight)) {
                        pending = () => new ProposeAllianceCommand(nationRepository, realm).execute(player, target);
                    }
                    if (row.canOfferPeace && Overlay.buttonAt("Peace##peace" + i, x + slot * 2f, by, buttonWidth, buttonHeight)) {
                        pending = () => new OfferPeaceCommand(nationRepository, realm).execute(player, target);
                    }
                    string warKey = "war:" + target.getId();
                    string warLabel = confirmGate.isArmed(warKey, now) ? ArmedLabel : "War";
                    if (row.canDeclareWar && Overlay.buttonAt(warLabel + "###war" + i, x + slot * 3f, by, buttonWidth, buttonHeight)) {
                        Nation own = nationRepository.getNation(player.getNationId());
                        pending = confirmGate.press(warKey, now)
                            ? () => new DeclareWarCommand(nationRepository, realm).execute(player, target)
                            : () => confirmGate.prompt(player.getStatus(), getWarPrompt(realm, own, target, TouchLayout.isActive()));
                    }
                    string denounceKey = "denounce:" + target.getId();
                    string denounceLabel = confirmGate.isArmed(denounceKey, now) ? ArmedLabel : "Denounce";
                    if (Overlay.buttonAt(denounceLabel + "###denounce" + i, x + slot * 4f, by, buttonWidth, buttonHeight)) {
                        pending = confirmGate.press(denounceKey, now)
                            ? () => new DenounceNationCommand(nationRepository, simulation.getDiplomacy()).execute(player, target)
                            : () => confirmGate.prompt(player.getStatus(), (TouchLayout.isActive() ? "Tap" : "Click") + " Denounce again to denounce " + target.getName() + ".");
                    }
                }
                y += steps[i];
            }
            return pending;
        }

        /** How many rows from first fit in the height; at least one, so a tall row is never stuck off screen. */
        private static int countFitting(float[] steps, int first, float space) {
            int count = 0;
            float used = 0f;
            for (int i = first; i < steps.Length; i++) {
                if (count > 0 && used + steps[i] > space) {
                    break;
                }
                used += steps[i];
                count++;
            }
            return count;
        }

        /** How many rows at the end of the list fit in the height, which is how far down the list may scroll. */
        private static int countFittingFromEnd(float[] steps, float space) {
            int count = 0;
            float used = 0f;
            for (int i = steps.Length - 1; i >= 0; i--) {
                if (count > 0 && used + steps[i] > space) {
                    break;
                }
                used += steps[i];
                count++;
            }
            return count;
        }
    }
}
