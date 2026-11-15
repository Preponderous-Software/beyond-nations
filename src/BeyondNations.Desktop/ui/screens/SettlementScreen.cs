using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.ui.boxes;

namespace beyondnations.desktop.ui.screens {

    /**
    * One settlement, on a screen of its own (#178): whose it is -- the
    * nation, and the member who holds it (#176) -- its people, its
    * buildings, its stores and its market, and what the player can do
    * with it.
    *
    * It opens when the player enters a settlement (SettlementScreens), from
    * the Settlement button on the command bar while inside one, and from
    * the Manage tab of the nations screen. Exit Settlement on it leaves the
    * settlement and goes back to the world; Back goes back to wherever it
    * was opened from, leaving the player where they stand. Like the other
    * panels it is laid over the world, which waits while it is open.
    *
    * The leader of the settlement's nation can give it to a member (pick
    * one with the arrows, then Give) and take it back; a member who holds
    * one can hand it back. Prev and Next step through the nation's other
    * settlements. Every button is big enough for a thumb, and no text
    * names a key.
    *
    * The words come from getSections() and getActions(), which need no
    * window, so they are asserted in unit tests and only the drawing needs
    * a screen.
    */
    public class SettlementScreen {
        public const string BackLabel = "Back";
        public const string ExitLabel = "Exit Settlement";
        public const string GiveLabel = "Give";
        public const string TakeBackLabel = "Take Back";
        public const string HandBackLabel = "Hand Back";
        public const string CloseHint = "(Press {Pause} to go back)";
        public const string NoSettlement = "There is no settlement to show.";

        private static readonly Vector4 HeadingColour = new Vector4(0.95f, 0.75f, 0.30f, 1f);

        // which settlement to show; set when the screen is asked for
        private static EntityId shownId;
        private static bool openRequested;
        // the member the Give button would give the settlement to, by index into getActions().candidates
        private int pick;
        private EntityId pickFor;
        private int firstLine;
        private float dragCarry;

        /** Asks the host to open the screen on this settlement (SettlementScreens picks it up before the next frame). */
        public static void requestOpen(EntityId settlementId) {
            shownId = settlementId;
            openRequested = true;
        }

        /** True once after requestOpen. */
        public static bool consumeOpenRequest() {
            bool requested = openRequested;
            openRequested = false;
            return requested;
        }

        public static EntityId getShownId() {
            return shownId;
        }

        /**
        * The settlement to show: the one asked for, while it stands;
        * otherwise the one the player is in, the nearest of their nation's,
        * or their home. Null when there is none.
        */
        public static Settlement resolve(Simulation simulation, EntityId wanted) {
            EntityRepository entities = simulation.getEntityRepository();
            Player player = simulation.getPlayer();
            Settlement settlement = standing(entities, wanted)
                ?? standing(entities, player.getCurrentSettlementId());
            if (settlement != null) {
                return settlement;
            }
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            if (own != null) {
                float best = float.MaxValue;
                foreach (Settlement candidate in simulation.getDiplomacy().getSettlements(own)) {
                    float distance = Territory.groundDistance(player.getPosition(), candidate.getPosition());
                    if (distance < best) {
                        best = distance;
                        settlement = candidate;
                    }
                }
            }
            return settlement ?? standing(entities, player.getHomeSettlementId());
        }

        private static Settlement standing(EntityRepository entities, EntityId id) {
            Settlement settlement = id == null ? null : entities.getEntity(id) as Settlement;
            return settlement == null || settlement.isMarkedForDeletion() ? null : settlement;
        }

        /** The settlements Prev and Next step through: the player's nation's, when this is one of them; otherwise this one alone. */
        public static List<Settlement> getBrowsable(Simulation simulation, Settlement settlement) {
            Nation own = simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());
            if (own != null && settlement.getNationId() == own.getId()) {
                return simulation.getDiplomacy().getSettlements(own);
            }
            return new List<Settlement> { settlement };
        }

        /** A heading and the lines under it. */
        public class Section {
            public readonly string heading;
            public readonly List<string> lines;

            public Section(string heading, List<string> lines) {
                this.heading = heading;
                this.lines = lines;
            }
        }

        /** "VILLAGE OF BLACKBURG", with "(2)" when the nation has two of that kind. */
        public static string getTitle(Simulation simulation, Settlement settlement) {
            Nation nation = simulation.getNationRepository().getNation(settlement.getNationId());
            string name = nation != null ? MembersPanel.describeSettlement(simulation, nation, settlement.getId()) : settlement.getDisplayName();
            return name.ToUpperInvariant();
        }

        /** Who it belongs to, its people and buildings, its stores and its market. */
        public static List<Section> getSections(Simulation simulation, Settlement settlement) {
            List<Section> sections = new List<Section>();
            sections.Add(new Section("Ownership", describeOwnership(simulation, settlement)));
            sections.Add(new Section("People", describePeople(simulation, settlement)));
            sections.Add(new Section("Buildings", describeBuildings(settlement)));
            Nation own = simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());
            bool ours = own != null && settlement.getNationId() == own.getId();
            sections.Add(new Section("Stores", ours ? describeStores(settlement)
                : new List<string> { "Only its own people see what " + settlement.getNationName() + " keeps here." }));
            sections.Add(new Section("Market", describeMarket(simulation, settlement)));
            return sections;
        }

        /**
        * "Nation: Blackburg (your nation)." and "Held by: you, as its
        * leader." -- or by the member it was given to, or a foreign leader.
        */
        public static List<string> describeOwnership(Simulation simulation, Settlement settlement) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            NationRepository nations = simulation.getNationRepository();
            Nation nation = nations.getNation(settlement.getNationId());
            Nation own = nations.getNation(player.getNationId());
            if (nation == null) {
                lines.Add("Nation: none. It has been abandoned.");
                return lines;
            }
            string standing;
            if (nation == own) {
                standing = "your nation";
            } else if (own == null) {
                standing = "you belong to no nation";
            } else {
                standing = nations.getRelations().describe(own, nation) + (simulation.getRealm().isAtWar(own, nation) ? ", at war with you" : "");
            }
            lines.Add("Nation: " + nation.getName() + " (" + standing + ").");

            EntityId holder = SettlementOwnership.holderOf(settlement, nation);
            bool granted = SettlementOwnership.isGranted(settlement, nation);
            string who = holder == player.getId() ? "you" : nameOf(simulation, holder);
            if (granted) {
                lines.Add("Held by: " + who + ", given it by the leader of " + nation.getName() + ".");
            } else if (settlement.getOwnerId() != null && holder == settlement.getOwnerId()) {
                lines.Add("Held by: " + who + ", as leader, and kept if the rule passes on.");
            } else {
                lines.Add("Held by: " + who + ", as leader of " + nation.getName() + ".");
            }
            if (settlement.getSiegeText() != null) {
                lines.Add(settlement.getSiegeText() + "!");
            }
            return lines;
        }

        private static string nameOf(Simulation simulation, EntityId id) {
            Entity entity = id == null ? null : simulation.getEntityRepository().getEntity(id);
            return entity == null ? "no one" : entity.getName();
        }

        /** "Residents: 6 of 8 (3 here now)." and, for the player's, where it lies and whether it is home. */
        public static List<string> describePeople(Simulation simulation, Settlement settlement) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            lines.Add("Residents: " + settlement.getResidentCount() + " of " + settlement.getResidentCap()
                + " (" + settlement.getCurrentlyPresentEntitiesCount() + " here now).");
            string where;
            if (player.getCurrentSettlementId() == settlement.getId()) {
                where = "You are inside it";
            } else {
                int distance = (int) Territory.groundDistance(player.getPosition(), settlement.getPosition());
                where = distance + " paces " + Compass.describe(player.getPosition(), settlement.getPosition());
            }
            bool home = settlement.getId() == player.getHomeSettlementId();
            lines.Add(where + (home ? ". It is your home." : "."));
            return lines;
        }

        /** "Town, 5 of 8 buildings: 2 houses, 2 farms, 1 storehouse." and what comes next. */
        public static List<string> describeBuildings(Settlement settlement) {
            List<string> lines = new List<string>();
            string tier = SettlementTiers.getDisplayName(settlement.getTier());
            int count = settlement.getNumBuildings();
            if (count == 0) {
                lines.Add(tier + ", no buildings yet.");
            } else {
                List<string> parts = new List<string>();
                foreach (BuildingType type in System.Enum.GetValues<BuildingType>()) {
                    int n = settlement.getNumBuildings(type);
                    if (n > 0) {
                        string name = BuildingCatalog.getDisplayName(type).ToLowerInvariant();
                        parts.Add(n + " " + name + (n == 1 ? "" : "s"));
                    }
                }
                lines.Add(tier + ", " + count + " of " + BuildingCatalog.MAX_BUILDINGS + " buildings: " + string.Join(", ", parts) + ".");
            }
            BuildingType? next = BuildingPlanner.suggestNext(settlement);
            if (next != null) {
                lines.Add("Next: " + BuildingCatalog.getDisplayName(next.Value) + ", " + BuildingCatalog.describeCost(next.Value) + ".");
            }
            return lines;
        }

        /** "Funds: 30 coins." and "Stockpile: 40 wood, 25 stone." */
        public static List<string> describeStores(Settlement settlement) {
            List<string> lines = new List<string>();
            Inventory inventory = settlement.getInventory();
            lines.Add("Funds: " + inventory.getNumItems(ItemType.COIN) + " coins.");
            List<string> goods = new List<string>();
            foreach (ItemType type in System.Enum.GetValues<ItemType>()) {
                int n = type == ItemType.COIN ? 0 : inventory.getNumItems(type);
                if (n > 0) {
                    goods.Add(ItemNames.describeQuantity(type, n));
                }
            }
            lines.Add(goods.Count == 0 ? "Stockpile: empty." : "Stockpile: " + string.Join(", ", goods) + ".");
            return lines;
        }

        /** "4 of 4 stalls, 1 for sale; merchants: Ann, Bob +1." and the staple prices. */
        public static List<string> describeMarket(Simulation simulation, Settlement settlement) {
            List<string> lines = new List<string>();
            Market market = settlement.getMarket();
            List<string> merchants = new List<string>();
            foreach (Stall stall in market.getStalls()) {
                if (stall.getOwnerId() != null) {
                    merchants.Add(stall.getOwnerId() == simulation.getPlayer().getId() ? "you" : nameOf(simulation, stall.getOwnerId()));
                }
            }
            string stalls = market.getNumStalls() + " of " + market.getMaxNumStalls() + " stalls, " + market.getNumStallsForSale() + " for sale";
            if (merchants.Count == 0) {
                lines.Add(stalls + "; no merchants.");
            } else {
                int shown = System.Math.Min(3, merchants.Count);
                lines.Add(stalls + "; merchants: " + string.Join(", ", merchants.GetRange(0, shown))
                    + (merchants.Count > shown ? " +" + (merchants.Count - shown) : "") + ".");
            }
            MarketInfoBox prices = new MarketInfoBox(market, simulation.getEntityRepository());
            List<string> priced = new List<string>();
            foreach (ItemType type in prices.getListedGoods()) {
                priced.Add(prices.describePrice(type));
            }
            lines.Add("Prices (buy/sell): " + string.Join(", ", priced) + ".");
            return lines;
        }

        /** What the player can do with the settlement, and the members it can be given to. */
        public class Actions {
            /** The player leads its nation: Give and, when given away, Take Back. */
            public readonly bool canGive;
            public readonly bool canTakeBack;
            /** The player holds it as a member, not as leader: Hand Back. */
            public readonly bool canHandBack;
            /** The player is inside it: Exit Settlement. */
            public readonly bool canExit;
            /** Who Give can give it to: the player first, then the members oldest first; never the one holding it. */
            public readonly List<Entity> candidates;
            /** A line saying what the player can and cannot do here. */
            public readonly string note;

            public Actions(bool canGive, bool canTakeBack, bool canHandBack, bool canExit, List<Entity> candidates, string note) {
                this.canGive = canGive;
                this.canTakeBack = canTakeBack;
                this.canHandBack = canHandBack;
                this.canExit = canExit;
                this.candidates = candidates;
                this.note = note;
            }
        }

        public static Actions getActions(Simulation simulation, Settlement settlement) {
            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            bool ours = own != null && settlement.getNationId() == own.getId();
            bool leads = ours && own.getLeaderId() == player.getId();
            bool inside = player.getCurrentSettlementId() == settlement.getId();
            List<Entity> candidates = new List<Entity>();
            string note;
            if (leads) {
                if (settlement.getOwnerId() != player.getId()) {
                    candidates.Add(player);
                }
                foreach (Pawn pawn in TransferLeadershipCommand.candidates(own, simulation.getEntityRepository())) {
                    if (pawn.getId() != settlement.getOwnerId()) {
                        candidates.Add(pawn);
                    }
                }
                note = SettlementOwnership.isGranted(settlement, own)
                    ? "Its holder manages its funds, stockpile and stalls. Take it back to manage them yourself."
                    : "Give it to one of your people and its funds, stockpile and stalls are theirs to manage. Give it to yourself to keep it if the rule passes on.";
            } else if (ours && settlement.getOwnerId() == player.getId()) {
                note = "It is yours to manage: its funds, stockpile and stalls. Hand it back to give it to the leader.";
            } else if (ours) {
                note = "The leader of " + own.getName() + " decides who holds it.";
            } else {
                note = "It is not your nation's.";
            }
            bool granted = ours && settlement.getOwnerId() != null;
            return new Actions(leads && candidates.Count > 0, leads && granted, !leads && ours && settlement.getOwnerId() == player.getId(),
                inside, candidates, note);
        }

        /** "Give to: Ada Smith", or "Give to: you". */
        public static string describePick(Entity candidate, Player player) {
            return "Give to: " + (candidate == player ? "you" : candidate.getName());
        }

        /** Index i of a list of count, wrapped round both ends. */
        public static int wrap(int i, int count) {
            return count <= 0 ? 0 : ((i % count) + count) % count;
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            Player player = simulation.getPlayer();
            Settlement settlement = resolve(simulation, shownId);
            float lineHeight = System.MathF.Max(UiScale.px(26f), height / 22f);
            float buttonHeight = System.MathF.Max(lineHeight - 4f, UiScale.px(TouchLayout.isActive() ? TouchLayout.MinTarget : 30f));
            float gap = UiScale.px(8f);
            float left = System.MathF.Max(width * 0.04f, UiScale.px(16f));
            float y = System.MathF.Max(System.MathF.Max(height / 18f, UiScale.px(12f)), Overlay.touchCloseBottom());

            Overlay.beginFullScreen("##settlement-screen", width, height);
            Overlay.dimBackground(width, height);
            UiAction action = UiAction.None;
            System.Action pending = null;

            if (settlement == null) {
                Overlay.textCentered(NoSettlement, height / 30f, height / 2f, width);
                if (Overlay.buttonAt(BackLabel + "##settlement-back", (width - UiScale.px(160f)) / 2f, height / 2f + lineHeight, UiScale.px(160f), buttonHeight)) {
                    action = UiAction.CloseSettlement;
                }
                Overlay.end();
                return Overlay.touchCloseButton("settlement", width) ? UiAction.CloseSettlement : action;
            }
            if (settlement.getId() != shownId) {
                shownId = settlement.getId();
            }

            bool narrow = width < UiScale.px(600f);
            // the title, with Prev and Next either side of it when the nation has more than one
            List<Settlement> browsable = getBrowsable(simulation, settlement);
            int index = System.Math.Max(0, browsable.IndexOf(settlement));
            float arrowWidth = System.MathF.Min(UiScale.px(90f), width / 5f);
            if (browsable.Count > 1) {
                if (Overlay.buttonAt("Prev##settlement-prev", left, y, arrowWidth, buttonHeight)) {
                    shownId = browsable[wrap(index - 1, browsable.Count)].getId();
                    firstLine = 0;
                }
                if (Overlay.buttonAt("Next##settlement-next", width - left - arrowWidth, y, arrowWidth, buttonHeight)) {
                    shownId = browsable[wrap(index + 1, browsable.Count)].getId();
                    firstLine = 0;
                }
            }
            if (narrow) {
                // a phone: the arrows have the row to themselves, the name goes under them
                y += buttonHeight + gap;
                y = NationScreen.drawWrappedCentred(getTitle(simulation, settlement), height / 30f, y, width, left, lineHeight * 0.75f, 2);
            } else {
                float titleWidth = width - (browsable.Count > 1 ? (arrowWidth + gap) * 2f : 0f);
                NationScreen.drawWrappedCentred(getTitle(simulation, settlement), height / 26f, y + (buttonHeight - lineHeight * 0.8f) * 0.5f,
                    width, (width - titleWidth) / 2f + left * 0.5f, lineHeight * 0.8f, 1);
                y += buttonHeight + gap;
            }
            if (browsable.Count > 1) {
                Overlay.textCentered((index + 1) + " of " + browsable.Count + " settlements of your nation", height / 40f, y, width);
                y += lineHeight * 0.6f;
            }

            // the action strip sits at the bottom, above the status line and the hint
            Actions actions = getActions(simulation, settlement);
            float hintTop = height - lineHeight * 1.2f;
            float statusTop = hintTop - lineHeight * 1.6f;
            // on a phone, Give and Take Back go under the picker, and the note may take three lines
            bool stackGive = narrow && actions.canGive;
            int actionRows = 1 + (actions.canGive || actions.canTakeBack || actions.canHandBack ? 1 : 0) + (stackGive ? 1 : 0);
            int noteLines = narrow ? 3 : 2;
            float noteHeight = lineHeight * 0.62f * noteLines + gap;
            float actionsTop = statusTop - (buttonHeight + gap) * actionRows - noteHeight - gap;

            // the sections: two columns on a wide screen, one on a phone; drag or wheel to scroll
            bool twoColumns = width >= UiScale.px(860f);
            float columnGap = UiScale.px(24f);
            float columnWidth = twoColumns ? (width - left * 2f - columnGap) / 2f : width - left * 2f;
            float headingSize = height / 32f;
            float textSize = height / 36f;
            float textScale = Overlay.fontScaleFor(textSize);
            float headingStep = lineHeight * 0.8f;
            float textStep = lineHeight * 0.68f;
            List<Section> sections = getSections(simulation, settlement);
            List<List<string>> wrapped = new List<List<string>>();
            foreach (Section section in sections) {
                List<string> lines = new List<string>();
                foreach (string line in section.lines) {
                    lines.AddRange(TextWrap.wrap(line, columnWidth, text => ImGui.CalcTextSize(text).X * textScale));
                }
                wrapped.Add(lines);
            }
            float top = y + gap;
            float bottom = actionsTop - gap;
            if (twoColumns) {
                // ownership, people and buildings on the left; stores and market on the right
                float leftY = top;
                float rightY = top;
                for (int i = 0; i < sections.Count; i++) {
                    bool right = i >= 3;
                    float x = right ? left + columnWidth + columnGap : left;
                    float sy = right ? rightY : leftY;
                    sy = drawSection(sections[i].heading, wrapped[i], x, sy, bottom, headingSize, textSize, headingStep, textStep, gap);
                    if (right) {
                        rightY = sy;
                    } else {
                        leftY = sy;
                    }
                }
            } else {
                // one column, scrolled by whole lines
                List<KeyValuePair<bool, string>> flat = new List<KeyValuePair<bool, string>>();
                for (int i = 0; i < sections.Count; i++) {
                    flat.Add(new KeyValuePair<bool, string>(true, sections[i].heading));
                    foreach (string line in wrapped[i]) {
                        flat.Add(new KeyValuePair<bool, string>(false, line));
                    }
                }
                // the last line is kept for "Scroll for more"
                bottom -= textStep;
                int fitting = System.Math.Max(1, (int) ((bottom - top) / textStep) - 1);
                firstLine = System.Math.Clamp(firstLine + readScroll(top, bottom, textStep), 0, System.Math.Max(0, flat.Count - fitting));
                float sy = top;
                for (int i = firstLine; i < flat.Count; i++) {
                    float step = flat[i].Key ? headingStep : textStep;
                    if (sy + step > bottom) {
                        break;
                    }
                    if (flat[i].Key) {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(HeadingColour));
                        Overlay.textAtScaled(flat[i].Value, headingSize, left, sy);
                        ImGui.PopStyleColor();
                    } else {
                        Overlay.textAtScaled(flat[i].Value, textSize, left, sy);
                    }
                    sy += step;
                }
                if (firstLine + fitting < flat.Count) {
                    Overlay.textCentered(TouchLayout.isActive() ? "Drag for more" : "Scroll for more", height / 42f, bottom + textStep * 0.4f, width);
                }
            }

            // the note, then the buttons
            NationScreen.drawWrappedCentred(actions.note, height / 38f, actionsTop, width, left, lineHeight * 0.62f, noteLines);
            float rowY = actionsTop + noteHeight;
            float rowWidth = width - left * 2f;
            if (actions.canGive || actions.canTakeBack || actions.canHandBack) {
                if (actions.canGive) {
                    if (pickFor != settlement.getId()) {
                        pickFor = settlement.getId();
                        pick = 0;
                    }
                    pick = wrap(pick, actions.candidates.Count);
                    Entity candidate = actions.candidates[pick];
                    float small = System.MathF.Min(UiScale.px(56f), rowWidth / 8f);
                    float give = stackGive ? (actions.canTakeBack ? (rowWidth - gap) / 2f : rowWidth) : System.MathF.Min(UiScale.px(110f), rowWidth / 5f);
                    float take = actions.canTakeBack ? (stackGive ? give : System.MathF.Min(UiScale.px(130f), rowWidth / 4f)) : 0f;
                    float label = stackGive ? rowWidth - small * 2f - gap * 2f
                        : rowWidth - small * 2f - give - take - gap * (actions.canTakeBack ? 4f : 3f);
                    float x = left;
                    if (Overlay.buttonAt("<##give-prev", x, rowY, small, buttonHeight)) {
                        pick = wrap(pick - 1, actions.candidates.Count);
                    }
                    x += small + gap;
                    ImGui.BeginDisabled();
                    Overlay.buttonAt(fitLabel(describePick(candidate, player), label) + "##give-who", x, rowY, label, buttonHeight);
                    ImGui.EndDisabled();
                    x += label + gap;
                    if (Overlay.buttonAt(">##give-next", x, rowY, small, buttonHeight)) {
                        pick = wrap(pick + 1, actions.candidates.Count);
                    }
                    x += small + gap;
                    if (stackGive) {
                        x = left;
                        rowY += buttonHeight + gap;
                    }
                    if (Overlay.buttonAt(GiveLabel + "##give", x, rowY, give, buttonHeight)) {
                        Settlement target = settlement;
                        EntityId to = candidate.getId();
                        pending = () => new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository()).grant(player, target, to);
                    }
                    x += give + gap;
                    if (actions.canTakeBack && Overlay.buttonAt(TakeBackLabel + "##take-back", x, rowY, take, buttonHeight)) {
                        Settlement target = settlement;
                        pending = () => new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository()).takeBack(player, target);
                    }
                } else {
                    float w = System.MathF.Min(UiScale.px(180f), rowWidth);
                    string text = actions.canTakeBack ? TakeBackLabel : HandBackLabel;
                    if (Overlay.buttonAt(text + "##hand-back", (width - w) / 2f, rowY, w, buttonHeight)) {
                        Settlement target = settlement;
                        pending = () => new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository()).takeBack(player, target);
                    }
                }
                rowY += buttonHeight + gap;
            }
            // Exit Settlement (when inside it) and Back, side by side
            float half = System.MathF.Min(UiScale.px(200f), (rowWidth - gap) / 2f);
            float pairX = (width - (actions.canExit ? half * 2f + gap : half)) / 2f;
            if (actions.canExit) {
                if (Overlay.buttonAt(ExitLabel + "##settlement-exit", pairX, rowY, half, buttonHeight)) {
                    pending = () => new ExitSettlementCommand(simulation.getEntityRepository()).execute(player);
                    action = UiAction.ResumeWorld;
                }
                pairX += half + gap;
            }
            if (Overlay.buttonAt(BackLabel + "##settlement-back", pairX, rowY, half, buttonHeight)) {
                action = UiAction.CloseSettlement;
            }

            string status = ObjectiveKeyNames.resolve(player.getStatus().getStatus());
            if (!string.IsNullOrEmpty(status)) {
                NationScreen.drawWrappedCentred(status, height / 36f, statusTop, width, left, lineHeight * 0.65f, 2);
            }
            Overlay.textCentered(ObjectiveKeyNames.closeHint(CloseHint), height / 36f, hintTop + lineHeight * 0.4f, width);
            Overlay.end();
            if (Overlay.touchCloseButton("settlement", width)) {
                action = UiAction.CloseSettlement;
            }
            pending?.Invoke();
            return action;
        }

        private static float drawSection(string heading, List<string> lines, float x, float y, float bottom,
                float headingSize, float textSize, float headingStep, float textStep, float gap) {
            if (y + headingStep > bottom) {
                return y;
            }
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(HeadingColour));
            Overlay.textAtScaled(heading, headingSize, x, y);
            ImGui.PopStyleColor();
            y += headingStep;
            foreach (string line in lines) {
                if (y + textStep > bottom) {
                    break;
                }
                Overlay.textAtScaled(line, textSize, x, y);
                y += textStep;
            }
            return y + gap;
        }

        /** The label cut with "..." to fit the width, as the command bar cuts a long name. */
        private static string fitLabel(string text, float width) {
            float padding = ImGui.GetStyle().FramePadding.X * 2f;
            if (ImGui.CalcTextSize(text).X + padding <= width) {
                return text;
            }
            string cut = text;
            while (cut.Length > 4 && ImGui.CalcTextSize(cut + "...").X + padding > width) {
                cut = cut.Substring(0, cut.Length - 1);
            }
            return cut + "...";
        }

        private int readScroll(float top, float bottom, float rowHeight) {
            ImGuiIOPtr io = ImGui.GetIO();
            int move = 0;
            if (io.MouseWheel != 0f) {
                move += io.MouseWheel > 0f ? -1 : 1;
            }
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && io.MousePos.Y >= top && io.MousePos.Y <= bottom) {
                dragCarry -= io.MouseDelta.Y;
                while (dragCarry >= rowHeight) {
                    move++;
                    dragCarry -= rowHeight;
                }
                while (dragCarry <= -rowHeight) {
                    move--;
                    dragCarry += rowHeight;
                }
            } else {
                dragCarry = 0f;
            }
            return move;
        }
    }
}
