using System.Collections.Generic;

namespace beyondnations {

    /**
    * A good year: more apples than anyone can eat. Store them, sell them, or
    * feast -- which fills everyone up and wins hearts, but keeps nothing.
    */
    public class BountifulHarvestEvent : IWorldEvent {
        public const string Id = "harvest";
        public const int BASE_APPLES = 10;
        public const int APPLES_PER_FARM = 4;
        public const int COINS_PER_APPLE = 2;
        public const int FEAST_RELATIONSHIP = 3;

        public string getId() { return Id; }
        public string getTitle() { return "A bountiful harvest"; }
        public bool isPersonal() { return false; }

        public int getWeight(WorldEventContext context, Nation nation) {
            // no harvest in winter, once the world has seasons (WorldEventContext.getSeason)
            if (nation == null || context.isWinter() || context.getWorldEvents().isDrought(nation.getId())) {
                return 0;
            }
            if (context.settlementsOf(nation).Count == 0) {
                return 0;
            }
            return 4 + 2 * System.Math.Min(4, context.countFarms(nation));
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            List<Settlement> settlements = context.settlementsOf(nation);
            if (settlements.Count == 0) {
                return false;
            }
            instance.setSettlementId(settlements[0].getId());
            instance.setMagnitude(BASE_APPLES + APPLES_PER_FARM * System.Math.Min(4, context.countFarms(nation)));
            return true;
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            Nation nation = context.getNation(instance.getNationId());
            return "The trees and fields of " + (nation == null ? "your land" : nation.getName()) + " are heavy with fruit: "
                + instance.getMagnitude() + " apples more than anyone expected. What should be done with them?";
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            int apples = instance.getMagnitude();
            return new List<WorldEventChoice> {
                new WorldEventChoice("Fill the stores", apples + " apples go into the stockpile for leaner days."),
                new WorldEventChoice("Sell the surplus", apples * COINS_PER_APPLE + " coins into the treasury now."),
                new WorldEventChoice("Hold a feast", "Every member eats their fill and thinks better of you; nothing is kept."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 0;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            return context.availableFood(instance) < instance.getMagnitude() ? 0 : 1;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Nation nation = context.getNation(instance.getNationId());
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            if (nation == null || settlement == null) {
                return null;
            }
            string who = context.chooser(instance);
            int apples = instance.getMagnitude();
            if (choice == 0) {
                settlement.getInventory().addItem(ItemType.APPLE, apples);
                return "a bountiful harvest came in across " + nation.getName() + ", and " + who + " filled the stores with " + apples + " apples.";
            }
            if (choice == 1) {
                settlement.addFunds(apples * COINS_PER_APPLE);
                return "a bountiful harvest came in across " + nation.getName() + "; " + who + " sold the surplus for " + apples * COINS_PER_APPLE + " coins.";
            }
            foreach (Pawn member in context.membersOf(nation)) {
                member.setEnergy(100f);
                if (instance.isForPlayer()) {
                    context.warmToPlayer(member, FEAST_RELATIONSHIP);
                }
            }
            if (instance.isForPlayer()) {
                context.getPlayer().setEnergy(100f);
            }
            return "a bountiful harvest came in across " + nation.getName() + ", and " + who + " held a feast that was talked about for days.";
        }
    }
}
