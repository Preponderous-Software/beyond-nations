using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui.screens {

    /**
    * The Members tab of the nations screen: everyone in the player's
    * nation, with their role, home settlement, loyalty to the player and
    * traits, and the heir marked. Before it the succession rules (family
    * first, then the most loyal, then the longest sworn) could be seen only
    * after a death (#358).
    *
    * The heir is the one Succession.chooseHeir would pick at this moment, so
    * the mark always agrees with what a death would do. The rows keep the
    * nation's own order -- the player first, then the members oldest
    * first -- which does not change while the screen is open, so the list
    * never re-sorts under a finger; a change of heir only moves the mark.
    *
    * The list scrolls like the Laws tab: the wheel, the keys, a drag, or the
    * Up/Down buttons for a thumb. The rows hold no buttons. The words come
    * from getLines() and getRows(), which need no window, so they are
    * asserted in unit tests.
    */
    public class MembersPanel {
        public const string NoNation = "You belong to no nation, so no one is sworn to you. Found one ({CreateNewNation}) or join one ({JoinNation}).";
        public const string HeirMark = "HEIR";

        private static readonly Vector4 HeirColour = new Vector4(0.95f, 0.75f, 0.30f, 1f);
        private static readonly Vector4 DetailColour = new Vector4(0.85f, 0.85f, 0.80f, 1f);

        /** The first member on screen. */
        private int firstRow;

        /** One member, as the panel shows it. */
        public class Row {
            public readonly EntityId id;
            public readonly bool isPlayer;
            public readonly bool isHeir;
            /** "Alice Smith - Merchant, Village of Farmfield", with "HEIR: " before it for the heir. */
            public readonly string title;
            /** "Loyalty: Friend (17). Industrious, Greedy." */
            public readonly string detail;
            public readonly string role;
            public readonly string settlement;
            /** The member's regard for the player; 0 on the player's own row. */
            public readonly int loyalty;
            public readonly string traits;

            public Row(EntityId id, bool isPlayer, bool isHeir, string title, string detail, string role, string settlement, int loyalty, string traits) {
                this.id = id;
                this.isPlayer = isPlayer;
                this.isHeir = isHeir;
                this.title = title;
                this.detail = detail;
                this.role = role;
                this.settlement = settlement;
                this.loyalty = loyalty;
                this.traits = traits;
            }
        }

        public static string getRoleName(NationRole role) {
            switch (role) {
                case NationRole.LEADER: return "Leader";
                case NationRole.MERCHANT: return "Merchant";
                case NationRole.SERF: return "Serf";
                default: return "Member";
            }
        }

        /** The lines above the list: how many there are, who the heir is and why, and the rule. */
        public static List<string> getLines(Simulation simulation) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            if (own == null) {
                lines.Add(ObjectiveKeyNames.resolve(NoNation));
                return lines;
            }
            int members = own.getNumberOfMembers();
            lines.Add("The people of " + own.getName() + ": " + members + (members == 1 ? " member" : " members")
                + ". Loyalty is how each feels about you.");
            string reason;
            Pawn heir = Succession.chooseHeir(own, player, simulation.getEntityRepository(), out reason);
            lines.Add(heir == null
                ? "No heir: if you die now, your line ends."
                : "Your heir: " + heir.getName() + ", " + reason + ".");
            lines.Add("When you die, your family comes first, then the most loyal, then the longest sworn.");
            return lines;
        }

        /** The player first, then every other member, oldest first; empty without a nation. */
        public static List<Row> getRows(Simulation simulation) {
            List<Row> rows = new List<Row>();
            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            if (own == null) {
                return rows;
            }
            EntityRepository entities = simulation.getEntityRepository();
            string ignored;
            Pawn heir = Succession.chooseHeir(own, player, entities, out ignored);

            string playerRole = getRoleName(own.getRole(player.getId()));
            string playerHome = describeSettlement(simulation, own, player.getHomeSettlementId());
            rows.Add(new Row(player.getId(), true, false, "You (" + player.getName() + ") - " + playerRole + ", " + playerHome,
                "The one they are loyal to.", playerRole, playerHome, 0, ""));

            foreach (EntityId id in own.getMemberIds()) {
                if (id == player.getId()) {
                    continue;
                }
                Pawn pawn = entities.getEntity(id) as Pawn;
                if (pawn == null || pawn.isMarkedForDeletion()) {
                    continue;
                }
                bool isHeir = pawn == heir;
                string role = getRoleName(own.getRole(id));
                string home = describeSettlement(simulation, own, pawn.getHomeSettlementId());
                int loyalty = PawnRelations.getRelationship(pawn, player);
                string traits = PawnTraits.describe(pawn);
                string title = (isHeir ? HeirMark + ": " : "") + pawn.getName() + " - " + role + ", " + home;
                string detail = "Loyalty: " + RelationshipTiers.getName(RelationshipTiers.fromValue(loyalty)) + " (" + loyalty + ")."
                    + (traits.Length > 0 ? " " + traits + "." : "");
                rows.Add(new Row(id, false, isHeir, title, detail, role, home, loyalty, traits));
            }
            return rows;
        }

        /**
        * "Village of Farmfield", "Camp of Farmfield (2)" when the nation has
        * more than one of that kind, or "no home".
        */
        public static string describeSettlement(Simulation simulation, Nation nation, EntityId settlementId) {
            Settlement home = settlementId == null ? null : simulation.getEntityRepository().getEntity(settlementId) as Settlement;
            if (home == null || home.isMarkedForDeletion()) {
                return "no home";
            }
            string name = home.getDisplayName();
            List<Settlement> settlements = simulation.getDiplomacy().getSettlements(nation);
            int sameKind = 0;
            int index = 0;
            foreach (Settlement settlement in settlements) {
                if (settlement.getTier() != home.getTier()) {
                    continue;
                }
                sameKind++;
                if (settlement == home) {
                    index = sameKind;
                }
            }
            return sameKind > 1 && index > 0 ? name + " (" + index + ")" : name;
        }

        /** "Members 1-5 of 9. Scroll for more.", for the line over a list that does not all fit. */
        public static string describeShown(int firstRow, int shown, int total, bool touch) {
            string members = shown == 1 ? "Member " + (firstRow + 1) : "Members " + (firstRow + 1) + "-" + (firstRow + shown);
            return members + " of " + total + "." + (touch ? " Drag or tap Up/Down for more." : " Scroll for more.");
        }

        /** Draws the panel from y down. It holds no commands, so it returns null. */
        public System.Action draw(float width, float height, Simulation simulation, float y, float left, float lineHeight,
                float buttonGap, float buttonHeight, LawsPanel.ScrollReader readScroll) {
            foreach (string line in getLines(simulation)) {
                y = NationScreen.drawWrappedCentred(line, height / 36f, y, width, left, lineHeight * 0.7f, 3);
            }
            y += lineHeight * 0.3f;

            float columnWidth = width - left * 2f;
            float titleSize = height / 32f;
            float titleScale = Overlay.fontScaleFor(titleSize);
            float detailSize = height / 38f;
            float detailScale = Overlay.fontScaleFor(detailSize);
            float titleStep = lineHeight * 0.75f;
            float detailStep = lineHeight * 0.65f;
            List<Row> rows = getRows(simulation);
            List<List<string>> titles = new List<List<string>>(rows.Count);
            List<List<string>> details = new List<List<string>>(rows.Count);
            float[] steps = new float[rows.Count];
            for (int i = 0; i < rows.Count; i++) {
                List<string> title = TextWrap.wrap(rows[i].title, columnWidth, line => ImGui.CalcTextSize(line).X * titleScale);
                List<string> detail = TextWrap.wrap(rows[i].detail, columnWidth, line => ImGui.CalcTextSize(line).X * detailScale);
                titles.Add(title);
                details.Add(detail);
                steps[i] = titleStep * System.Math.Min(2, title.Count) + detailStep * System.Math.Min(2, detail.Count) + buttonGap * 1.5f;
            }

            float listBottom = height - lineHeight * 3.2f;
            float stripHeight = buttonHeight + buttonGap;
            LawsPanel.Window window = LawsPanel.fit(steps, y, listBottom, stripHeight, firstRow);
            int move = readScroll(window.listTop, listBottom, rows.Count > 0 ? steps[0] : lineHeight);
            window = LawsPanel.fit(steps, y, listBottom, stripHeight, firstRow + move);
            firstRow = window.firstRow;
            if (window.overflow) {
                float arrowWidth = System.MathF.Min(UiScale.px(80f), columnWidth / 5f);
                float lineWidth = width - (arrowWidth + buttonGap) * 2f;
                NationScreen.drawWrappedCentred(describeShown(firstRow, window.visible, rows.Count, TouchLayout.isActive()), height / 38f,
                    y + (buttonHeight - lineHeight * 0.7f) * 0.5f, lineWidth, left, lineHeight * 0.7f, 1);
                float ax = width - left - arrowWidth * 2f - buttonGap;
                int page = System.Math.Max(1, window.visible - 1);
                if (Overlay.buttonAt("Up##members-up", ax, y, arrowWidth, buttonHeight)) {
                    window = LawsPanel.fit(steps, y, listBottom, stripHeight, firstRow - page);
                }
                if (Overlay.buttonAt("Down##members-down", ax + arrowWidth + buttonGap, y, arrowWidth, buttonHeight)) {
                    window = LawsPanel.fit(steps, y, listBottom, stripHeight, firstRow + page);
                }
                firstRow = window.firstRow;
            }
            y = window.listTop;

            for (int i = firstRow; i < firstRow + window.visible; i++) {
                Row row = rows[i];
                // the heir in gold, so the mark is seen before it is read
                if (row.isHeir) {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(HeirColour));
                }
                List<string> title = titles[i];
                for (int line = 0; line < title.Count && line < 2; line++) {
                    Overlay.textAtScaled(title[line], titleSize, left, y);
                    y += titleStep;
                }
                if (row.isHeir) {
                    ImGui.PopStyleColor();
                }
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(DetailColour));
                List<string> detail = details[i];
                for (int line = 0; line < detail.Count && line < 2; line++) {
                    Overlay.textAtScaled(detail[line], detailSize, left, y);
                    y += detailStep;
                }
                ImGui.PopStyleColor();
                y += buttonGap * 1.5f;
            }
            return null;
        }
    }
}
