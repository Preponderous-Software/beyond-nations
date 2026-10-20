using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui {

    /**
    * The panel a nation's leader sends a caravan from: pick a market, load
    * goods from what you carry, and send it. Opened by Send Caravan on the
    * command bar (CaravanCommands), in one of your settlements with a
    * Storehouse; the world keeps running behind it.
    *
    * Everything is a button -- previous/next market, -5/+5 per good, Send,
    * Close -- so it works the same by touch on a phone as with a mouse; no
    * part of it needs hovering or a keyboard. What it shows is worked out
    * by plain methods (getDestinationLine, getGoodLine, getEstimate) a test
    * can check without a window.
    *
    * The panel's choices are the host's, not the world's: nothing here is
    * saved, and closing it forgets them.
    */
    public static class CaravanDispatchPanel {
        public const string Title = "SEND A CARAVAN";
        public const int LoadStep = 5;

        private static bool isOpenFlag;
        private static int destinationIndex;
        private static readonly Dictionary<ItemType, int> load = new Dictionary<ItemType, int>();

        private const ImGuiWindowFlags PanelFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.AlwaysAutoResize;

        public static bool isOpen() {
            return isOpenFlag;
        }

        public static void open(Simulation simulation) {
            isOpenFlag = true;
            destinationIndex = 0;
            load.Clear();
            string refusal = simulation.getTradeCaravans().getDispatchRefusal();
            if (refusal != null) {
                simulation.getPlayer().getStatus().update(refusal);
                isOpenFlag = false;
            }
        }

        public static void close() {
            isOpenFlag = false;
            load.Clear();
        }

        // --- what it shows ----------------------------------------------------

        public static List<Settlement> getDestinations(Simulation simulation) {
            TradeCaravans trade = simulation.getTradeCaravans();
            Settlement origin = trade.getPlayerSettlement();
            if (origin == null || simulation.getPlayer().getNationId() == null) {
                return new List<Settlement>();
            }
            return trade.getDestinations(origin, simulation.getPlayer().getNationId());
        }

        public static Settlement getDestination(Simulation simulation) {
            List<Settlement> destinations = getDestinations(simulation);
            if (destinations.Count == 0) {
                return null;
            }
            destinationIndex = ((destinationIndex % destinations.Count) + destinations.Count) % destinations.Count;
            return destinations[destinationIndex];
        }

        public static void nextDestination(int step) {
            destinationIndex += step;
        }

        /** "To the Town of Yland, Friendly (+25), 420 away  [2/5]" */
        public static string getDestinationLine(Simulation simulation) {
            List<Settlement> destinations = getDestinations(simulation);
            Settlement destination = getDestination(simulation);
            if (destination == null) {
                return "No market within " + (int) TradeCaravans.MAX_ROUTE_DISTANCE + " that is not at war with you.";
            }
            Settlement origin = simulation.getTradeCaravans().getPlayerSettlement();
            int distance = (int) Vector3.Distance(new Vector3(origin.getPosition().X, 0, origin.getPosition().Z),
                new Vector3(destination.getPosition().X, 0, destination.getPosition().Z));
            string standing = "";
            NationRepository nations = simulation.getNationRepository();
            Nation own = nations.getNation(simulation.getPlayer().getNationId());
            Nation theirs = nations.getNation(destination.getNationId());
            if (own != null && theirs != null && own != theirs) {
                standing = ", " + nations.getRelations().describe(own, theirs);
            }
            return "To the " + destination.getDisplayName() + standing + ", " + distance + " away  [" + (destinationIndex + 1) + "/" + destinations.Count + "]";
        }

        public static int getLoad(ItemType itemType) {
            int quantity;
            return load.TryGetValue(itemType, out quantity) ? quantity : 0;
        }

        public static int getTotalLoad() {
            int total = 0;
            foreach (int quantity in load.Values) {
                total += quantity;
            }
            return total;
        }

        /** Loads or unloads step of a good, within what is carried and the caravan's limit. */
        public static void changeLoad(Simulation simulation, ItemType itemType, int step) {
            int held = simulation.getPlayer().getInventory().getNumItems(itemType);
            int current = getLoad(itemType);
            int room = TradeCaravans.MAX_PLAYER_CARGO - (getTotalLoad() - current);
            int next = System.Math.Clamp(current + step, 0, System.Math.Min(held, room));
            if (next == 0) {
                load.Remove(itemType);
            }
            else {
                load[itemType] = next;
            }
        }

        /** "Wood: 10 of 20   here 49, there 72" -- sell prices at each end. */
        public static string getGoodLine(Simulation simulation, ItemType itemType) {
            Settlement origin = simulation.getTradeCaravans().getPlayerSettlement();
            Settlement destination = getDestination(simulation);
            string line = ItemNames.getDisplayName(itemType) + ": " + getLoad(itemType) + " of "
                + simulation.getPlayer().getInventory().getNumItems(itemType);
            if (origin != null && destination != null) {
                line += "   here " + origin.getMarket().getSellPrice(itemType) + ", there " + destination.getMarket().getSellPrice(itemType);
            }
            return line;
        }

        /** "20 goods, worth about 540 there" */
        public static string getEstimate(Simulation simulation) {
            Settlement destination = getDestination(simulation);
            int total = getTotalLoad();
            if (destination == null || total == 0) {
                return "Load goods with + (you carry them; the takings go to the treasury).";
            }
            long worth = 0;
            foreach (KeyValuePair<ItemType, int> entry in load) {
                worth += TradeCaravans.estimateSale(destination.getMarket(), entry.Key, entry.Value);
            }
            worth = System.Math.Min(worth, TradeCaravans.getPurchasingPower(destination));
            return total + " goods, worth about " + worth + " coins there.";
        }

        /** Sends the caravan; closes the panel when it goes. */
        public static bool send(Simulation simulation) {
            Settlement destination = getDestination(simulation);
            Caravan sent = simulation.getTradeCaravans().dispatchPlayerCaravan(destination, new Dictionary<ItemType, int>(load),
                simulation.getTickCounter().getTotalTicks());
            if (sent != null) {
                close();
                return true;
            }
            return false;
        }

        // --- drawing ----------------------------------------------------------

        /** Draws the panel when open, centred under topY. */
        public static void draw(float width, float height, Simulation simulation, float topY) {
            if (!isOpenFlag) {
                return;
            }
            // the player walked out, or lost the right to send one
            if (simulation.getTradeCaravans().getDispatchRefusal() != null) {
                close();
                return;
            }
            float buttonHeight = System.MathF.Max(Overlay.frameHeight() + UiScale.px(8f), UiScale.px(36f));
            float small = UiScale.px(56f);
            float wide = UiScale.px(120f);
            float maxWidth = width - UiScale.px(32f);

            ImGui.SetNextWindowPos(new Vector2(width / 2f, topY), ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowBgAlpha(0.92f);
            if (ImGui.Begin("##caravan-dispatch", PanelFlags)) {
                ImGui.PushTextWrapPos(maxWidth);
                ImGui.TextUnformatted(Title + " from the " + simulation.getTradeCaravans().getPlayerSettlement().getDisplayName());
                if (ImGui.Button("<##dest-prev", new Vector2(small, buttonHeight))) {
                    nextDestination(-1);
                }
                ImGui.SameLine();
                if (ImGui.Button(">##dest-next", new Vector2(small, buttonHeight))) {
                    nextDestination(1);
                }
                ImGui.SameLine();
                ImGui.TextUnformatted(getDestinationLine(simulation));

                List<ItemType> goods = simulation.getTradeCaravans().getLoadableGoods();
                if (goods.Count == 0) {
                    Overlay.textColoured(new Vector4(1f, 0.72f, 0.35f, 1f), "You carry nothing a market buys.");
                }
                foreach (ItemType itemType in goods) {
                    ItemType good = itemType;
                    if (ImGui.Button("-" + LoadStep + "##less-" + good, new Vector2(small, buttonHeight))) {
                        changeLoad(simulation, good, -LoadStep);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+" + LoadStep + "##more-" + good, new Vector2(small, buttonHeight))) {
                        changeLoad(simulation, good, LoadStep);
                    }
                    ImGui.SameLine();
                    ImGui.TextUnformatted(getGoodLine(simulation, good));
                }
                Overlay.textColoured(new Vector4(0.85f, 0.85f, 0.75f, 1f), getEstimate(simulation));

                if (ImGui.Button("Send##caravan", new Vector2(wide, buttonHeight))) {
                    send(simulation);
                }
                ImGui.SameLine();
                if (ImGui.Button("Close##caravan", new Vector2(wide, buttonHeight))) {
                    close();
                }
                ImGui.PopTextWrapPos();
            }
            ImGui.End();
        }
    }
}
