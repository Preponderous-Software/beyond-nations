using System.Collections.Generic;

namespace beyondnations {

    /**
    * No rain: a nation's farms bring in nothing for a day unless something
    * is done. Irrigation shortens it, bought grain carries the nation
    * through it, and waiting costs the leader goodwill.
    */
    public class DroughtEvent : IWorldEvent {
        public const string Id = "drought";
        public const int DURATION_SECONDS = 600;
        public const int IRRIGATED_DURATION_SECONDS = 150;
        public const int IRRIGATION_WOOD = 15;
        public const int IRRIGATION_STONE = 10;
        public const int GRAIN_COINS = 25;
        public const int GRAIN_APPLES = 12;
        public const int RESTLESS_RELATIONSHIP = 2;

        public string getId() { return Id; }
        public string getTitle() { return "Drought"; }
        public bool isPersonal() { return false; }

        public int getWeight(WorldEventContext context, Nation nation) {
            // no drought in winter, once the world has seasons (WorldEventContext.getSeason)
            if (nation == null || context.isWinter() || context.getWorldEvents().isDrought(nation.getId())) {
                return 0;
            }
            int farms = context.countFarms(nation);
            return farms == 0 ? 0 : 6 + 3 * farms;
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            Settlement most = null;
            foreach (Settlement settlement in context.settlementsOf(nation)) {
                if (most == null || settlement.getNumBuildings(BuildingType.FARM) > most.getNumBuildings(BuildingType.FARM)) {
                    most = settlement;
                }
            }
            if (most == null || most.getNumBuildings(BuildingType.FARM) == 0) {
                return false;
            }
            instance.setSettlementId(most.getId());
            instance.setMagnitude(context.countFarms(nation));
            return true;
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            Nation nation = context.getNation(instance.getNationId());
            string n = nation == null ? "the land" : nation.getName();
            string farms = instance.getMagnitude() == 1 ? "Its farm is" : "Its " + instance.getMagnitude() + " farms are";
            return "No rain has fallen on " + n + " for weeks. " + farms + " drying out and will bring in nothing for a day.";
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            bool canDig = context.available(instance, ItemType.WOOD) >= IRRIGATION_WOOD && context.available(instance, ItemType.STONE) >= IRRIGATION_STONE;
            return new List<WorldEventChoice> {
                WorldEventChoice.requiring(canDig, "Dig irrigation ditches (" + IRRIGATION_WOOD + " wood, " + IRRIGATION_STONE + " stone)",
                    "The farms are dry for a quarter of a day instead of a whole one.",
                    "Needs " + IRRIGATION_WOOD + " wood and " + IRRIGATION_STONE + " stone"),
                WorldEventChoice.requiring(context.availableCoins(instance) >= GRAIN_COINS, "Buy grain from traders (" + GRAIN_COINS + " coins)",
                    "The drought lasts the day, but " + GRAIN_APPLES + " apples go into the stores now.",
                    "Needs " + GRAIN_COINS + " coins"),
                new WorldEventChoice("Ration and wait for rain",
                    "Costs nothing, but the drought lasts the day and your people grow restless with you."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            List<WorldEventChoice> choices = getChoices(context, instance);
            return choices[0].isAvailable() ? 0 : choices[1].isAvailable() ? 1 : 2;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Nation nation = context.getNation(instance.getNationId());
            if (nation == null) {
                return null;
            }
            string n = nation.getName();
            string who = context.chooser(instance);
            int duration = DURATION_SECONDS;
            string text;
            if (choice == 0) {
                context.pay(instance, ItemType.WOOD, IRRIGATION_WOOD);
                context.pay(instance, ItemType.STONE, IRRIGATION_STONE);
                duration = IRRIGATED_DURATION_SECONDS;
                text = "drought struck " + n + "; " + who + " had irrigation ditches dug, and the farms were dry only a little while.";
            }
            else if (choice == 1) {
                context.payCoins(instance, GRAIN_COINS);
                Settlement stores = context.getSettlement(instance.getSettlementId());
                if (stores != null) {
                    stores.getInventory().addItem(ItemType.APPLE, GRAIN_APPLES);
                }
                text = "drought struck " + n + "; " + who + " bought grain from passing traders to see it through.";
            }
            else {
                if (instance.isForPlayer()) {
                    foreach (Pawn member in context.membersOf(nation)) {
                        member.decreaseRelationship(context.getPlayer(), RESTLESS_RELATIONSHIP);
                    }
                }
                text = "drought struck " + n + ", and " + who + " rationed the stores and waited for rain.";
            }
            context.getWorldEvents().addEffect(new WorldEffect(WorldEffectKind.DROUGHT, nation.getId(), null,
                context.getTick(), context.getTick() + context.ticks(duration), 0));
            return text;
        }
    }
}
