using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui.screens {

    /**
    * The Manage tab of the nations screen (#175, #176, #178): the leader
    * renames the nation, hands its rule to another member or disbands it,
    * and every member sees the nation's settlements -- who holds each --
    * with an Open button for the settlement screen.
    *
    * Renaming: type a name, or tap Suggest for one (a phone's keyboard does
    * not open for the game's own text field, so on such a platform the
    * name button asks through the browser's prompt instead). Hand Over
    * and Disband take two presses, as War does (ConfirmGate). Disbanding
    * is the leader leaving a nation of one (NationLeaveCommand); with
    * other members still in it, the rule has to be handed on first.
    *
    * The words come from getLines(), getSettlementRows() and the static
    * describe methods, which need no window, so they are asserted in unit
    * tests.
    */
    public class ManagePanel {
        public const string NoNation = "You belong to no nation to manage. Found one ({CreateNewNation}) or join one ({JoinNation}).";
        public const string RenameLabel = "Rename";
        public const string SuggestLabel = "Suggest";
        public const string HandOverLabel = "Hand Over";
        public const string DisbandLabel = "Disband";
        public const string OpenLabel = "Open";

        /** Where a typed line comes from on a platform that cannot type into the game (ISharePlatform.promptsForText). */
        private ISharePlatform textPlatform;
        private string typedName = "";
        private int suggestion;
        private int heirPick;
        private int firstRow;
        private readonly ConfirmGate confirmGate = new ConfirmGate();

        public void setTextPlatform(ISharePlatform platform) {
            textPlatform = platform;
        }

        /** One of the nation's settlements, as the list shows it. */
        public class SettlementRow {
            public readonly Settlement settlement;
            /** "Village of Blackburg: held by you, 4/6 residents, 12 paces north (your home)." */
            public readonly string text;

            public SettlementRow(Settlement settlement, string text) {
                this.settlement = settlement;
                this.text = text;
            }
        }

        /** The lines over the controls: whose nation it is and what the leader may do with it. */
        public static List<string> getLines(Simulation simulation) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            if (own == null) {
                lines.Add(ObjectiveKeyNames.resolve(NoNation));
                return lines;
            }
            bool leads = own.getLeaderId() == player.getId();
            if (leads) {
                lines.Add("You lead " + own.getName() + ": rename it, hand it on, or disband it.");
            } else {
                Entity leader = simulation.getEntityRepository().getEntity(own.getLeaderId());
                lines.Add((leader != null ? leader.getName() : "Its leader") + " leads " + own.getName()
                    + ". Only the leader can rename it, hand it on or disband it.");
            }
            return lines;
        }

        /** The nation's settlements, in the order it founded them, with who holds each. */
        public static List<SettlementRow> getSettlementRows(Simulation simulation) {
            List<SettlementRow> rows = new List<SettlementRow>();
            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            if (own == null) {
                return rows;
            }
            foreach (Settlement settlement in simulation.getDiplomacy().getSettlements(own)) {
                EntityId holder = SettlementOwnership.holderOf(settlement, own);
                Entity entity = holder == null ? null : simulation.getEntityRepository().getEntity(holder);
                string who = holder == player.getId() ? "you" : entity != null ? entity.getName() : "no one";
                int distance = (int) Territory.groundDistance(player.getPosition(), settlement.getPosition());
                string where = player.getCurrentSettlementId() == settlement.getId() || distance < 1
                    ? "here" : distance + " paces " + Compass.describe(player.getPosition(), settlement.getPosition());
                bool home = settlement.getId() == player.getHomeSettlementId();
                rows.Add(new SettlementRow(settlement, MembersPanel.describeSettlement(simulation, own, settlement.getId())
                    + ": held by " + who + ", " + settlement.getResidentCount() + "/" + settlement.getResidentCap() + " residents, "
                    + where + (home ? " (your home)" : "") + "."));
            }
            return rows;
        }

        /**
        * Why Disband cannot be pressed now, or null when it can: other
        * members are still in the nation, or the player is inside a
        * settlement (NationLeaveCommand's own rule).
        */
        public static string describeDisbandRefusal(Nation own, Player player) {
            int others = own.getNumberOfMembers() - 1;
            if (others > 0) {
                return others + (others == 1 ? " other member still belongs" : " other members still belong")
                    + ". Hand the rule on first.";
            }
            if (player.isCurrentlyInSettlement()) {
                return "Leave the settlement first.";
            }
            return null;
        }

        /** What Disband does, said before the second press. */
        public static string describeDisband(Nation own) {
            int settlements = own.getNumberOfSettlements();
            return "Disbanding " + own.getName() + " ends it" + (settlements > 0
                ? " and razes its " + (settlements == 1 ? "settlement" : settlements + " settlements")
                : "") + ".";
        }

        /** "Hand to: Ada Smith (Friend, 17)" */
        public static string describeHeirPick(Pawn pawn, Player player) {
            int loyalty = PawnRelations.getRelationship(pawn, player);
            return "Hand to: " + pawn.getName() + " (" + RelationshipTiers.getName(RelationshipTiers.fromValue(loyalty)) + ", " + loyalty + ")";
        }

        public System.Action draw(float width, float height, Simulation simulation, float y, float left, float lineHeight,
                float buttonGap, float buttonHeight, LawsPanel.ScrollReader readScroll) {
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            EntityRepository entityRepository = simulation.getEntityRepository();
            Nation own = nationRepository.getNation(player.getNationId());
            foreach (string line in getLines(simulation)) {
                y = NationScreen.drawWrappedCentred(line, height / 36f, y, width, left, lineHeight * 0.7f, 3);
            }
            if (own == null) {
                return null;
            }
            y += lineHeight * 0.2f;
            double now = ImGui.GetTime();
            confirmGate.update(now, player.getStatus());
            System.Action pending = null;
            float rowWidth = width - left * 2f;
            bool leads = own.getLeaderId() == player.getId();

            if (leads) {
                // rename: the field (or, where typing cannot work, a button that asks), Suggest and Rename
                float small = System.MathF.Min(UiScale.px(120f), rowWidth / 4f);
                float field = rowWidth - small * 2f - buttonGap * 2f;
                if (textPlatform != null && textPlatform.promptsForText()) {
                    string shown = typedName.Length > 0 ? "Name: " + typedName : "Type a new name";
                    if (Overlay.buttonAt(shown + "##rename-field", left, y, field, buttonHeight)) {
                        string typed = textPlatform.promptText("A new name for " + own.getName() + ":", typedName);
                        if (typed != null) {
                            typedName = RenameNationCommand.tidy(typed);
                        }
                    }
                } else {
                    string text = typedName;
                    ImGui.SetCursorPos(new Vector2(left, y + (buttonHeight - Overlay.frameHeight()) / 2f));
                    ImGui.SetNextItemWidth(field);
                    bool entered = ImGui.InputTextWithHint("##rename-field", "A new name for " + own.getName(), ref text,
                        (uint) RenameNationCommand.MaxLength + 8, ImGuiInputTextFlags.EnterReturnsTrue);
                    typedName = text;
                    if (entered) {
                        string name = typedName;
                        pending = () => rename(simulation, player, name);
                    }
                }
                if (Overlay.buttonAt(SuggestLabel + "##rename-suggest", left + field + buttonGap, y, small, buttonHeight)) {
                    typedName = nextSuggestion(nationRepository);
                }
                if (Overlay.buttonAt(RenameLabel + "##rename", left + field + small + buttonGap * 2f, y, small, buttonHeight)) {
                    string name = typedName;
                    pending = () => rename(simulation, player, name);
                }
                y += buttonHeight + buttonGap * 1.5f;

                // hand on the rule: pick a member with the arrows, then Hand Over twice
                List<Pawn> heirs = TransferLeadershipCommand.candidates(own, entityRepository);
                if (heirs.Count == 0) {
                    y = NationScreen.drawWrappedCentred("No one else is in " + own.getName() + " to hand its rule to.", height / 38f,
                        y, width, left, lineHeight * 0.65f, 1) + buttonGap;
                } else {
                    heirPick = SettlementScreen.wrap(heirPick, heirs.Count);
                    Pawn heir = heirs[heirPick];
                    float arrow = System.MathF.Min(UiScale.px(56f), rowWidth / 8f);
                    float hand = System.MathF.Min(UiScale.px(130f), rowWidth / 4f);
                    float label = rowWidth - arrow * 2f - hand - buttonGap * 3f;
                    float x = left;
                    if (Overlay.buttonAt("<##heir-prev", x, y, arrow, buttonHeight)) {
                        heirPick = SettlementScreen.wrap(heirPick - 1, heirs.Count);
                        confirmGate.disarm();
                    }
                    x += arrow + buttonGap;
                    ImGui.BeginDisabled();
                    Overlay.buttonAt(describeHeirPick(heir, player) + "##heir-who", x, y, label, buttonHeight);
                    ImGui.EndDisabled();
                    x += label + buttonGap;
                    if (Overlay.buttonAt(">##heir-next", x, y, arrow, buttonHeight)) {
                        heirPick = SettlementScreen.wrap(heirPick + 1, heirs.Count);
                        confirmGate.disarm();
                    }
                    x += arrow + buttonGap;
                    string key = "hand:" + heir.getId();
                    if (Overlay.buttonAt((confirmGate.isArmed(key, now) ? NationScreen.ArmedLabel : HandOverLabel) + "###hand-over", x, y, hand, buttonHeight)) {
                        Pawn chosen = heir;
                        pending = confirmGate.press(key, now)
                            ? () => new TransferLeadershipCommand(nationRepository, entityRepository, simulation.getEventProducer()).execute(player, chosen)
                            : () => confirmGate.prompt(player.getStatus(), (TouchLayout.isActive() ? "Tap" : "Click") + " Hand Over again to make "
                                + chosen.getName() + " the leader of " + own.getName() + ". You stay one of its people.");
                    }
                    y += buttonHeight + buttonGap * 1.5f;
                }

                // disband: only a nation of one, from outside its settlements
                string refusal = describeDisbandRefusal(own, player);
                float disbandWidth = System.MathF.Min(UiScale.px(140f), rowWidth / 3f);
                if (refusal != null) {
                    ImGui.BeginDisabled();
                }
                string disbandKey = "disband:" + own.getId();
                if (Overlay.buttonAt((confirmGate.isArmed(disbandKey, now) ? NationScreen.ArmedLabel : DisbandLabel) + "###disband", left, y, disbandWidth, buttonHeight)
                        && refusal == null) {
                    pending = confirmGate.press(disbandKey, now)
                        ? () => new NationLeaveCommand(nationRepository, simulation.getEventProducer(), entityRepository).execute(player)
                        : () => confirmGate.prompt(player.getStatus(), (TouchLayout.isActive() ? "Tap" : "Click") + " Disband again: " + describeDisband(own));
                }
                if (refusal != null) {
                    ImGui.EndDisabled();
                }
                float disbandBottom = NationScreen.drawWrappedLeft(refusal ?? describeDisband(own), height / 40f, left + disbandWidth + buttonGap,
                    y, rowWidth - disbandWidth - buttonGap, lineHeight * 0.55f, 3);
                y = System.MathF.Max(y + buttonHeight, disbandBottom) + buttonGap * 2f;
            }

            // the settlements, each with Open
            List<SettlementRow> rows = getSettlementRows(simulation);
            y = NationScreen.drawWrappedCentred("Settlements (" + rows.Count + ")" + (rows.Count == 0 ? "." : ": open one to choose who holds it."),
                height / 36f, y, width, left, lineHeight * 0.7f, 2);
            float open = System.MathF.Min(UiScale.px(96f), rowWidth / 4f);
            float textWidth = rowWidth - open - buttonGap;
            float fontSize = height / 38f;
            // a row is as tall as its text, up to five lines on a phone
            float textScale = Overlay.fontScaleFor(fontSize);
            float textStep = lineHeight * 0.6f;
            float[] steps = new float[rows.Count];
            int[] rowLines = new int[rows.Count];
            for (int i = 0; i < steps.Length; i++) {
                rowLines[i] = System.Math.Min(5, TextWrap.wrap(rows[i].text, textWidth, line => ImGui.CalcTextSize(line).X * textScale).Count);
                steps[i] = System.MathF.Max(buttonHeight, textStep * rowLines[i]) + buttonGap;
            }
            float step = steps.Length > 0 ? steps[0] : buttonHeight;
            float listBottom = height - lineHeight * 3.2f;
            float stripHeight = buttonHeight + buttonGap;
            LawsPanel.Window window = LawsPanel.fit(steps, y, listBottom, stripHeight, firstRow);
            window = LawsPanel.fit(steps, y, listBottom, stripHeight, firstRow + readScroll(window.listTop, listBottom, step));
            firstRow = window.firstRow;
            if (window.overflow) {
                float arrowWidth = System.MathF.Min(UiScale.px(80f), rowWidth / 5f);
                float ax = width - left - arrowWidth * 2f - buttonGap;
                int page = System.Math.Max(1, window.visible - 1);
                NationScreen.drawWrappedLeft((firstRow + 1) + "-" + (firstRow + window.visible) + " of " + rows.Count, fontSize, left,
                    y + (buttonHeight - lineHeight * 0.6f) * 0.5f, ax - left - buttonGap, lineHeight * 0.6f, 1);
                if (Overlay.buttonAt("Up##manage-up", ax, y, arrowWidth, buttonHeight)) {
                    window = LawsPanel.fit(steps, y, listBottom, stripHeight, firstRow - page);
                }
                if (Overlay.buttonAt("Down##manage-down", ax + arrowWidth + buttonGap, y, arrowWidth, buttonHeight)) {
                    window = LawsPanel.fit(steps, y, listBottom, stripHeight, firstRow + page);
                }
                firstRow = window.firstRow;
            }
            y = window.listTop;
            for (int i = firstRow; i < firstRow + window.visible && i < rows.Count; i++) {
                NationScreen.drawWrappedLeft(rows[i].text, fontSize, left, y, textWidth, textStep, rowLines[i]);
                if (Overlay.buttonAt(OpenLabel + "##open-settlement" + i, left + textWidth + buttonGap, y, open, buttonHeight)) {
                    EntityId id = rows[i].settlement.getId();
                    pending = () => SettlementScreen.requestOpen(id);
                }
                y += steps[i];
            }
            return pending;
        }

        private void rename(Simulation simulation, Player player, string name) {
            bool renamed = new RenameNationCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getNationNameGenerator())
                .execute(player, name);
            if (renamed) {
                typedName = "";
            }
        }

        /** The next suggested name that no nation has. */
        private string nextSuggestion(NationRepository nationRepository) {
            for (int tries = 0; tries < 400; tries++) {
                string name = NationNameGenerator.suggest(suggestion++);
                if (RenameNationCommand.validate(name, null, nationRepository) == null) {
                    return name;
                }
            }
            return typedName;
        }
    }
}
