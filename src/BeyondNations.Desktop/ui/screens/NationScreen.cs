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
    * (LawsPanel). A third, Members, lists the player's own people, their
    * loyalty and the heir (MembersPanel). A fourth, Manage, is where the
    * leader renames the nation, hands it on or disbands it, and where its
    * settlements are listed with who holds each and an Open button for the
    * settlement screen (ManagePanel, #175, #176, #178).
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
        public const string MembersTab = "Members";
        public const string ManageTab = "Manage";

        private const float DesignButtonWidth = 96f;
        private const float DesignButtonGap = 8f;
        private const float DesignTabWidth = 120f;

        /** --start-screen laws opens the screen on its Laws tab. */
        public static bool startOnLaws = false;
        /** --start-screen members opens the screen on its Members tab. */
        public static bool startOnMembers = false;
        /** --start-screen manage opens the screen on its Manage tab. */
        public static bool startOnManage = false;

        private readonly LawsPanel lawsPanel = new LawsPanel();
        private readonly MembersPanel membersPanel = new MembersPanel();
        private readonly ManagePanel managePanel = new ManagePanel();
        private bool showingLaws = startOnLaws;
        private bool showingMembers = startOnMembers && !startOnLaws;
        private bool showingManage = startOnManage && !startOnLaws && !startOnMembers;

        /** Where the Manage tab asks for a typed name on a platform the game cannot type into. */
        public void setTextPlatform(ISharePlatform platform) {
            managePanel.setTextPlatform(platform);
        }
        /** The first row of the list on screen; the list scrolls when the nations outnumber the screen. */
        private int firstRow;
        // War and Denounce take two clicks, as on the command bar (#368)
        private readonly ConfirmGate confirmGate = new ConfirmGate();
        // the row order shown when the screen opened; kept until it is
        // closed, so a click never re-sorts a row out from under the pointer
        private readonly List<NationId> shownOrder = new List<NationId>();
        // the frame the screen was last drawn on (wasClosed)
        private int lastDrawnFrame = NeverDrawn;
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
                lines.Add("Each dealing sends your envoy for " + NationDiplomacy.secondsOf(Realm.ENVOY_TICKS)
                    + " s of play before you can deal with that nation again; the world waits while this screen is open.");
            }
            else {
                lines.Add("Only the leader of " + own.getName() + " can speak for it.");
            }
            lines.AddRange(describeOwnSettlements(simulation, own, leads));
            foreach (Settlement settlement in simulation.getDiplomacy().getSettlements(own)) {
                if (settlement.getSiegeText() != null) {
                    lines.Add("Your " + SettlementTiers.getDisplayName(settlement.getTier()).ToLowerInvariant() + ": " + settlement.getSiegeText() + "!");
                }
            }
            return lines;
        }

        /** At most this many of the nation's settlements are listed by name; the rest are counted. */
        public const int ListedSettlements = 6;

        /**
        * Which settlements the nation holds, where each lies from the player
        * and which is home (#145), and, for a leader, what founding another
        * costs or why it cannot be founded yet.
        */
        public static List<string> describeOwnSettlements(Simulation simulation, Nation own, bool leads) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            List<Settlement> settlements = simulation.getDiplomacy().getSettlements(own);
            int limit = own.getSettlementLimit();
            string heading = "Settlements: " + settlements.Count + (limit > 0 ? " of the " + limit + " this era allows" : "");
            lines.Add(heading + (settlements.Count == 0 ? "." : ":"));
            for (int i = 0; i < settlements.Count && i < ListedSettlements; i++) {
                Settlement settlement = settlements[i];
                int distance = (int) Territory.groundDistance(player.getPosition(), settlement.getPosition());
                string where = distance < 1 ? "here" : distance + " paces " + Compass.describe(player.getPosition(), settlement.getPosition());
                bool home = settlement.getId().Equals(player.getHomeSettlementId());
                lines.Add("  " + settlement.getDisplayName() + ": " + settlement.getResidentCount() + "/" + settlement.getResidentCap()
                    + " residents, " + where + (home ? " (your home)" : "") + ".");
            }
            if (settlements.Count > ListedSettlements) {
                lines.Add("  and " + (settlements.Count - ListedSettlements) + " more.");
            }
            if (leads && settlements.Count > 0) {
                lines.Add(own.canFoundSettlement()
                    ? ObjectiveKeyNames.resolve("Another settlement costs " + FoundingCost.describe(own) + ": far from other settlements, press {FoundSettlement}.")
                    : "Reach the next era to found another settlement.");
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
                    int envoy = leads ? realm.getEnvoyTicksLeft(own, nation) : 0;
                    if (envoy > 0) {
                        text += ", envoy back in " + NationDiplomacy.secondsOf(envoy) + " s";
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

        /** No frame yet: the first draw always counts as an opening. */
        public const int NeverDrawn = -1;

        /**
        * Whether the screen was closed since it was last drawn: a frame went
        * by without it. Counted in frames, not seconds, so that a slow frame
        * is not taken for a reopen (0.6.4 playtest s04: at 1-3 fps every
        * frame looked like one, so the list re-sorted under the pointer and
        * an armed click was forgotten).
        */
        public static bool wasClosed(int frame, int lastDrawnFrame) {
            return frame - lastDrawnFrame > 1;
        }

        /**
        * Whether a press of War or Denounce is refused because the envoy to
        * the target is still away, with the wait on the status line. Asked
        * before the press arms the gate, so that the player is not asked to
        * confirm something that would only be refused. The press was of
        * something else, so it disarms what was armed.
        */
        public static bool refusedByEnvoy(ConfirmGate gate, Player player, Realm realm, Nation own, Nation target) {
            if (!NationDiplomacy.isEnvoyAway(player, realm, own, target)) {
                return false;
            }
            gate.disarm();
            return true;
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
            if (shown <= 0) {
                // only the header, scrolled into the list, is showing (#511)
                return describeBelow(total, "other nation", "other nations", touch);
            }
            if (shown >= total) {
                return total + (total == 1 ? " other nation, " : " other nations, ") + order;
            }
            return "Nations " + (firstRow + 1) + "-" + (firstRow + shown) + " of " + total + ", " + order
                + (touch ? " Drag or tap Up/Down for more." : " Scroll for more.");
        }

        /**
        * Whether a header of lines lines, step apart, fits in room. When it
        * does not, drawNations scrolls it with the list (#511).
        */
        public static bool headerFits(int lines, float step, float room) {
            return lines * step <= room;
        }

        /**
        * A list's item heights with folded header lines (each step high) in
        * front of its rows: what a tab scrolls when its header does not fit
        * above the list (headerFits).
        */
        public static float[] withHeader(float[] steps, int folded, float step) {
            float[] items = new float[folded + steps.Length];
            for (int i = 0; i < items.Length; i++) {
                items[i] = i < folded ? step : steps[i - folded];
            }
            return items;
        }

        /**
        * Of the items first to first + visible, with folded header lines in
        * front, the first list row shown and how many rows are shown (0 when
        * only header lines are), for the count line.
        */
        public static void shownRows(int first, int visible, int folded, out int firstShown, out int shown) {
            firstShown = System.Math.Max(0, first - folded);
            shown = System.Math.Max(0, first + visible - System.Math.Max(first, folded));
        }

        /** "3 members below. Drag or tap Down.": the count line while only folded header lines show. */
        public static string describeBelow(int total, string one, string many, bool touch) {
            return total + " " + (total == 1 ? one : many) + " below." + (touch ? " Drag or tap Down." : " Scroll down.");
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
            if (laws) {
                showingMembers = false;
                showingManage = false;
            }
        }

        public bool isShowingMembers() {
            return showingMembers;
        }

        public void showMembers(bool members) {
            showingMembers = members;
            if (members) {
                showingLaws = false;
                showingManage = false;
            }
        }

        public bool isShowingManage() {
            return showingManage;
        }

        public void showManage(bool manage) {
            showingManage = manage;
            if (manage) {
                showingLaws = false;
                showingMembers = false;
            }
        }

        /**
        * The status line as this screen shows it: keys or buttons named
        * (ObjectiveKeyNames), less any sentence telling the player to open
        * this screen ("Press Realm to see the nations."), which read oddly
        * with the screen already open.
        */
        public static string statusLine(string raw) {
            if (string.IsNullOrEmpty(raw)) {
                return raw;
            }
            string trimmed = System.Text.RegularExpressions.Regex.Replace(raw, @"\s*[^.!?]*\{ToggleNations\}[^.!?]*[.!?]?", "").Trim();
            return ObjectiveKeyNames.resolve(trimmed);
        }

        /** "> Laws <" for the tab showing, "Laws" for the others. */
        public static string tabLabel(string tab, bool showing) {
            return showing ? "> " + tab + " <" : tab;
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            float buttonWidth = UiScale.px(DesignButtonWidth);
            float buttonGap = UiScale.px(DesignButtonGap);
            // text sized for the screen's width as well as its height, so a
            // phone held upright gets lines that fit it (Overlay.textHeightFor, #467)
            float textHeight = Overlay.textHeightFor(width, height);
            float lineHeight = System.MathF.Max(UiScale.px(26f), textHeight / 22f);
            // a fingertip's height on a touch screen (#469)
            float buttonHeight = Overlay.touchHeight(System.MathF.Max(lineHeight - 4f, UiScale.px(30f)));
            float left = System.MathF.Max(width * 0.04f, UiScale.px(16f));
            float y = System.MathF.Max(height / 18f, UiScale.px(12f));

            // here rather than in drawNations, so that a look at the Laws or
            // Members tab does not count as closing the screen
            int frame = ImGui.GetFrameCount();
            if (wasClosed(frame, lastDrawnFrame)) {
                shownOrder.Clear();
                confirmGate.disarm();
            }
            lastDrawnFrame = frame;
            // a "Click War/Denounce again" prompt whose window has lapsed is taken back
            confirmGate.update(ImGui.GetTime(), simulation.getPlayer().getStatus());

            Overlay.beginFullScreen("##nation-screen", width, height);
            // the Settlement screen's backing: with only a dim the world showed through the text and the tab row's gaps on a phone
            Overlay.panelBackground(width, height);

            // the four tabs, as big buttons so a thumb finds them
            const int tabs = 4;
            float tabWidth = System.MathF.Min(UiScale.px(DesignTabWidth), (width - left * 2f - buttonGap * (tabs - 1)) / tabs);
            float tabsX = (width - tabWidth * tabs - buttonGap * (tabs - 1)) / 2f;
            // below the corner Close on a touch screen (TouchLayout), unless
            // the tabs clear it (a phone on its side: the row under it cost
            // the lists a row, #467)
            if (Overlay.touchCloseBottom() > 0f && tabsX + tabWidth * tabs + buttonGap * (tabs - 1) > Overlay.touchCloseLeft(width) - buttonGap) {
                y = System.MathF.Max(y, Overlay.touchCloseBottom());
            } else if (Overlay.touchCloseBottom() > 0f) {
                y = System.MathF.Max(y, UiScale.px(8f));
            }
            bool showingNations = !showingLaws && !showingMembers && !showingManage;
            if (Overlay.buttonAt(tabLabel(NationsTab, showingNations) + "##tab-nations", tabsX, y, tabWidth, buttonHeight)) {
                showingLaws = false;
                showingMembers = false;
                showingManage = false;
            }
            if (Overlay.buttonAt(tabLabel(MembersTab, showingMembers) + "##tab-members", tabsX + tabWidth + buttonGap, y, tabWidth, buttonHeight)) {
                showMembers(true);
            }
            if (Overlay.buttonAt(tabLabel(LawsTab, showingLaws) + "##tab-laws", tabsX + (tabWidth + buttonGap) * 2f, y, tabWidth, buttonHeight)) {
                showLaws(true);
            }
            if (Overlay.buttonAt(tabLabel(ManageTab, showingManage) + "##tab-manage", tabsX + (tabWidth + buttonGap) * 3f, y, tabWidth, buttonHeight)) {
                showManage(true);
            }
            y += buttonHeight + lineHeight * 0.9f;

            Player player = simulation.getPlayer();
            System.Action pending = showingLaws
                ? lawsPanel.draw(width, height, simulation, y, left, lineHeight, buttonGap, buttonHeight, readScroll)
                : showingMembers
                ? membersPanel.draw(width, height, simulation, y, left, lineHeight, buttonGap, buttonHeight, readScroll)
                : showingManage
                ? managePanel.draw(width, height, simulation, y, left, lineHeight, buttonGap, buttonHeight, readScroll)
                : drawNations(width, height, simulation, y, left, lineHeight, buttonWidth, buttonGap, buttonHeight);

            // the answer goes on the status line, shown here too since the HUD is hidden;
            // it grows up from the footer into a band no list reaches (listBottom, #467)
            string status = statusLine(player.getStatus().getStatus());
            if (!string.IsNullOrEmpty(status)) {
                float statusSize = Overlay.atLeastAtlasFontSize(textHeight / 34f);
                List<string> statusLines = TextWrap.wrap(status, width - left * 2f,
                    line => ImGui.CalcTextSize(line).X * Overlay.fontScaleFor(statusSize));
                int shown = System.Math.Min(StatusLines, statusLines.Count);
                float statusY = height - lineHeight * 1.6f - lineHeight * 0.75f * (shown - 1);
                for (int i = 0; i < shown; i++) {
                    Overlay.textCentered(statusLines[i], statusSize, statusY + lineHeight * 0.75f * i, width);
                }
            }
            Overlay.textCentered(ObjectiveKeyNames.closeHint(CloseHint), textHeight / 34f, height - lineHeight * 0.7f, width);
            Overlay.end();
            // a finger has no O or Escape (TouchLayout)
            UiAction action = Overlay.touchCloseButton("nations", width) ? UiAction.ResumeWorld : UiAction.None;

            // run after the rows are drawn, so a nation disbanding cannot upset the loop
            pending?.Invoke();
            return action;
        }

        /** The most lines the status line takes at the foot of the screen. */
        public const int StatusLines = 3;

        /**
        * Where a tab's list must end: above the band at the foot of the
        * screen that holds the footer and up to StatusLines lines of status,
        * which grow up from the footer. The list used to end a little below
        * where the status started, so a long status was drawn over the last
        * row (#467).
        */
        public static float listBottom(float height, float lineHeight) {
            return height - lineHeight * (1.6f + 0.75f * (StatusLines - 1) + 0.45f);
        }

        /**
        * A tab's header, line by line, wrapped to the screen and never cut
        * mid-sentence by a line cap (#467, #506), but stopping at maxBottom
        * so the list under it keeps room for its first row and its buttons
        * (#511). A header that does not fit ends its last line in "...".
        * Returns the y after the last line drawn.
        */
        internal static float drawHeader(List<string> header, float fontSize, float y, float width, float margin, float step, float maxBottom) {
            fontSize = Overlay.atLeastAtlasFontSize(fontSize);
            float scale = Overlay.fontScaleFor(fontSize);
            List<string> lines = wrapHeader(header, width - margin * 2f, line => ImGui.CalcTextSize(line).X * scale,
                System.Math.Max(1, (int) ((maxBottom - y) / step)));
            foreach (string line in lines) {
                Overlay.textCentered(line, fontSize, y, width);
                y += step;
            }
            return y;
        }

        /**
        * The header's lines wrapped to maxWidth, at most maxLines of them;
        * when some are left out, the last kept line ends in "..." so the cut
        * shows. Kept apart from drawing so it can be tested.
        */
        public static List<string> wrapHeader(List<string> header, float maxWidth, System.Func<string, float> measure, int maxLines) {
            List<string> all = new List<string>();
            foreach (string line in header) {
                all.AddRange(TextWrap.wrap(line, maxWidth, measure));
            }
            if (all.Count <= maxLines) {
                return all;
            }
            List<string> kept = all.GetRange(0, System.Math.Max(1, maxLines));
            int last = kept.Count - 1;
            string cut = CommandBarLayout.fitLabel(kept[last], measure, maxWidth - measure(CommandBarLayout.Ellipsis));
            kept[last] = cut.EndsWith(CommandBarLayout.Ellipsis) ? cut : cut + CommandBarLayout.Ellipsis;
            return kept;
        }

        /** Centred text wrapped to the screen's width, at most maxLines lines. Returns the y after the last line. */
        internal static float drawWrappedCentred(string text, float fontSize, float y, float width, float margin, float step, int maxLines) {
            fontSize = Overlay.atLeastAtlasFontSize(fontSize);
            float scale = Overlay.fontScaleFor(fontSize);
            List<string> lines = TextWrap.wrap(text, width - margin * 2f, line => ImGui.CalcTextSize(line).X * scale);
            for (int i = 0; i < lines.Count && i < maxLines; i++) {
                Overlay.textCentered(lines[i], fontSize, y, width);
                y += step;
            }
            return y;
        }

        /**
        * The count line beside a list's Up/Down ("Nations 1-2 of 5, ...
        * Drag or tap Up/Down for more."), centred in the strip's height, on
        * two lines when the strip is tall enough for them (a touch screen's
        * 44-pixel buttons; upright it was cut after its first line, #467).
        */
        internal static void drawCountLine(string text, float fontSize, float y, float lineWidth, float margin, float step, float stripHeight) {
            int lines = stripHeight >= step * 2f ? 2 : 1;
            fontSize = Overlay.atLeastAtlasFontSize(fontSize);
            float scale = Overlay.fontScaleFor(fontSize);
            int count = System.Math.Min(lines, TextWrap.wrap(text, lineWidth - margin * 2f, line => ImGui.CalcTextSize(line).X * scale).Count);
            float top = y + (stripHeight - step * count) * 0.5f + step * 0.5f;
            drawWrappedCentred(text, fontSize, top, lineWidth, margin, step, lines);
        }

        /** Left-aligned text wrapped to maxWidth, at most maxLines lines. Returns the y after the last line. */
        internal static float drawWrappedLeft(string text, float fontSize, float x, float y, float maxWidth, float step, int maxLines) {
            fontSize = Overlay.atLeastAtlasFontSize(fontSize);
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
            float textHeight = Overlay.textHeightFor(width, height);
            double now = ImGui.GetTime();
            List<Row> rows = keepOrder(getRows(simulation), shownOrder);

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
            float fontSize = Overlay.atLeastAtlasFontSize(textHeight / 34f);
            float[] steps = new float[rows.Count];
            float tallest = 0f;
            for (int i = 0; i < rows.Count; i++) {
                int textLines = System.Math.Min(3, TextWrap.wrap(rows[i].text, textWidth,
                    line => ImGui.CalcTextSize(line).X * Overlay.fontScaleFor(fontSize)).Count);
                steps[i] = stacked
                    ? textStep * textLines + buttonHeight + buttonGap * 2f
                    : System.MathF.Max(textStep * textLines + buttonGap, buttonHeight + buttonGap);
                tallest = System.MathF.Max(tallest, steps[i]);
            }
            float listBottom = NationScreen.listBottom(height, lineHeight);

            // The header and the list share the screen. When the header
            // leaves no room under it for the count line, Up/Down and one
            // whole row with its buttons (a phone: the rows went under the
            // status line, and Ally could not be reached, #511), its lines
            // scroll with the list, above the first nation, so nothing is
            // cut and every row's buttons can be reached.
            float headerSize = Overlay.atLeastAtlasFontSize(textHeight / 36f);
            float headerStep = lineHeight * 0.7f;
            List<string> header = wrapHeader(getHeaderLines(simulation), width - left * 2f,
                line => ImGui.CalcTextSize(line).X * Overlay.fontScaleFor(headerSize), int.MaxValue);
            float headerRoom = listBottom - lineHeight * 0.3f - (buttonHeight + buttonGap) - tallest - y;
            int folded = rows.Count > 0 && !headerFits(header.Count, headerStep, headerRoom) ? header.Count : 0;
            if (folded == 0) {
                foreach (string line in header) {
                    Overlay.textCentered(line, headerSize, y, width);
                    y += headerStep;
                }
            }
            y += lineHeight * 0.3f;

            if (rows.Count == 0) {
                Overlay.textCentered(NoOtherNations, textHeight / 32f, y, width);
            }

            // the list's items: the folded header lines, then the nations
            float[] items = withHeader(steps, folded, headerStep);

            // the count line and Up/Down sit above the list
            float listTop = y + buttonHeight + buttonGap;
            int move = readScroll(listTop, listBottom, System.MathF.Max(buttonHeight, lineHeight));
            int lastPage = countFittingFromEnd(items, listBottom - listTop);
            firstRow = clampFirstRow(firstRow + move, items.Length, lastPage);
            int visible = countFitting(items, firstRow, listBottom - listTop);

            if (rows.Count > 0) {
                bool overflow = visible < items.Length;
                float arrowWidth = overflow ? System.MathF.Min(UiScale.px(80f), (width - left * 2f) / 5f) : 0f;
                float lineWidth = width - (overflow ? (arrowWidth + buttonGap) * 2f : 0f);
                if (overflow) {
                    float ax = width - left - arrowWidth * 2f - buttonGap;
                    if (Overlay.buttonAt("Up##nations-up", ax, y, arrowWidth, buttonHeight)) {
                        firstRow = clampFirstRow(firstRow - System.Math.Max(1, visible - 1), items.Length, lastPage);
                    }
                    if (Overlay.buttonAt("Down##nations-down", ax + arrowWidth + buttonGap, y, arrowWidth, buttonHeight)) {
                        firstRow = clampFirstRow(firstRow + System.Math.Max(1, visible - 1), items.Length, lastPage);
                    }
                    visible = countFitting(items, firstRow, listBottom - listTop);
                }
                int firstNation;
                int shownNations;
                shownRows(firstRow, visible, folded, out firstNation, out shownNations);
                drawCountLine(describeShown(firstNation, shownNations, rows.Count, TouchLayout.isActive()), textHeight / 38f,
                    y, lineWidth, left, lineHeight * 0.7f, buttonHeight);
            }
            y = listTop;

            System.Action pending = null;
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            Realm realm = simulation.getRealm();
            int last = System.Math.Min(items.Length, firstRow + visible);
            for (int item = firstRow; item < last; item++) {
                if (item < folded) {
                    Overlay.textCentered(header[item], headerSize, y + headerStep * 0.5f, width);
                    y += headerStep;
                    continue;
                }
                int i = item - folded;
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
                        if (!refusedByEnvoy(confirmGate, player, realm, own, target)) {
                            pending = confirmGate.press(warKey, now)
                                ? () => new DeclareWarCommand(nationRepository, realm).execute(player, target)
                                : () => confirmGate.prompt(player.getStatus(), getWarPrompt(realm, own, target, TouchLayout.isActive()));
                        }
                    }
                    string denounceKey = "denounce:" + target.getId();
                    string denounceLabel = confirmGate.isArmed(denounceKey, now) ? ArmedLabel : "Denounce";
                    if (Overlay.buttonAt(denounceLabel + "###denounce" + i, x + slot * 4f, by, buttonWidth, buttonHeight)) {
                        Nation own = nationRepository.getNation(player.getNationId());
                        if (!refusedByEnvoy(confirmGate, player, realm, own, target)) {
                            pending = confirmGate.press(denounceKey, now)
                                ? () => new DenounceNationCommand(nationRepository, simulation.getDiplomacy(), realm).execute(player, target)
                                : () => confirmGate.prompt(player.getStatus(), (TouchLayout.isActive() ? "Tap" : "Click") + " Denounce again to denounce " + target.getName() + ".");
                        }
                    }
                }
                y += steps[i];
            }
            return pending;
        }

        /**
        * Rows to move whichever list is showing by this frame: the wheel, the
        * keys (via the host), or a drag between listTop and listBottom. Asked
        * by both tabs, so that keys pressed on one do not wait for the other.
        */
        private int readScroll(float listTop, float listBottom, float rowHeight) {
            ImGuiIOPtr io = ImGui.GetIO();
            int move = pendingScroll;
            pendingScroll = 0;
            if (io.MouseWheel != 0f) {
                move += io.MouseWheel > 0f ? -1 : 1;
            }
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && io.MousePos.Y >= listTop && io.MousePos.Y <= listBottom) {
                dragCarry -= io.MouseDelta.Y;
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
            return move;
        }

        /** How many rows from first fit in the height; at least one, so a tall row is never stuck off screen. */
        public static int countFitting(float[] steps, int first, float space) {
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
        public static int countFittingFromEnd(float[] steps, float space) {
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
