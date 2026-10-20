using System.Collections.Generic;

namespace beyondnations {

    /**
    * Wanderers at the gate, asking to join. Taking them all in costs food
    * but grows the nation at a stroke; taking one costs nothing; turned
    * away, they stay in the world as nationless pawns -- who may well found
    * a rival nation of their own nearby.
    */
    public class WanderersEvent : IWorldEvent {
        public const string Id = "wanderers";
        public const int FOOD_PER_WANDERER = 2;
        public const float ARRIVAL_DISTANCE = 25f;

        public string getId() { return Id; }
        public string getTitle() { return "Wanderers at the gate"; }
        public bool isPersonal() { return false; }

        public int getWeight(WorldEventContext context, Nation nation) {
            List<Settlement> settlements = context.settlementsOf(nation);
            if (settlements.Count == 0) {
                return 0;
            }
            GameConfig config = context.getSimulation().getGameConfig();
            if (config.getLagPreventionEnabled()
                    && context.getSimulation().getEntityRepository().getNumEntitiesOfType(EntityType.PAWN) + 3 > config.getMaxNumPawns()) {
                return 0;
            }
            foreach (Settlement settlement in settlements) {
                if (settlement.hasRoomForResident()) {
                    return 9;
                }
            }
            return 4;
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            List<Settlement> settlements = context.settlementsOf(nation);
            if (settlements.Count == 0) {
                return false;
            }
            Settlement gate = settlements[0];
            foreach (Settlement settlement in settlements) {
                if (settlement.hasRoomForResident()) {
                    gate = settlement;
                    break;
                }
            }
            instance.setSettlementId(gate.getId());
            instance.setMagnitude(context.getRandom().range(2, 4));
            return true;
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            Nation nation = context.getNation(instance.getNationId());
            return instance.getMagnitude() + " wanderers have come to the gates of the " + (settlement == null ? "settlement" : settlement.getDisplayName())
                + ", footsore and hungry. They ask to join " + (nation == null ? "your nation" : nation.getName()) + ".";
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            int food = instance.getMagnitude() * FOOD_PER_WANDERER;
            return new List<WorldEventChoice> {
                WorldEventChoice.requiring(context.availableFood(instance) >= food, "Welcome them all (" + food + " food)",
                    instance.getMagnitude() + " new members, fed from your stores.", "Needs " + food + " food"),
                new WorldEventChoice("Take in only the strongest", "One new member, at no cost. The rest move on."),
                new WorldEventChoice("Turn them away", "Costs nothing, but they stay in these lands with no nation -- and may found one of their own."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 1;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            return context.availableFood(instance) >= instance.getMagnitude() * FOOD_PER_WANDERER * 2 ? 0 : 1;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Nation nation = context.getNation(instance.getNationId());
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            if (nation == null || settlement == null) {
                return null;
            }
            int arriving = instance.getMagnitude();
            int joining = choice == 0 ? arriving : choice == 1 ? 1 : 0;
            if (choice == 0) {
                context.payFood(instance, arriving * FOOD_PER_WANDERER);
            }
            List<string> joined = new List<string>();
            int spawned = 0;
            for (int i = 0; i < arriving; i++) {
                if (choice == 1 && i > 0) {
                    break; // the rest move on, out of the story
                }
                Pawn pawn = context.spawnPawnNear(settlement.getPosition(), ARRIVAL_DISTANCE);
                if (pawn == null) {
                    break;
                }
                spawned++;
                if (i < joining) {
                    context.join(pawn, nation);
                    joined.Add(pawn.getName());
                    if (instance.isForPlayer()) {
                        context.warmToPlayer(pawn, 5);
                    }
                }
            }
            string who = context.chooser(instance);
            string at = "the " + settlement.getDisplayName();
            if (choice == 2) {
                return spawned + " wanderers asked to join " + nation.getName() + " at " + at + "; " + who + " turned them away, and they made camp in the wilds.";
            }
            if (joined.Count == 0) {
                return "wanderers came to " + at + ", but there was no room in the world for them.";
            }
            return joined.Count == 1
                ? "wanderers came to " + at + "; " + who + " took in only " + joined[0] + ", and the rest moved on."
                : "wanderers came to " + at + ", and " + who + " welcomed them all: " + string.Join(", ", joined) + " joined " + nation.getName() + ".";
        }
    }
}
