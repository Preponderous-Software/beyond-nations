using System.Collections.Generic;

namespace beyondnations {

    /**
    * The player crafts one batch of a recipe from what they carry, and the
    * status line says what was made -- or exactly what is still missing.
    */
    public class CraftItemCommand {
        private readonly EntityRepository entityRepository;

        public CraftItemCommand(EntityRepository entityRepository) {
            this.entityRepository = entityRepository;
        }

        public bool execute(Player player, Recipe recipe) {
            Settlement settlement = getCurrentSettlement(player, entityRepository);
            Inventory inventory = player.getInventory();
            List<string> missing = Crafting.getMissing(recipe, inventory, settlement);
            if (missing.Count > 0) {
                player.getStatus().update("Can't craft " + recipe.getName() + " yet: need " + string.Join(", ", missing) + ".");
                return false;
            }
            Crafting.craft(recipe, inventory, settlement);
            player.getStatus().update("Crafted " + recipe.describeOutput() + describeEffect(recipe) + ".");
            return true;
        }

        /** The settlement the player is standing in, or null out in the world. */
        public static Settlement getCurrentSettlement(Player player, EntityRepository entityRepository) {
            EntityId id = player.getCurrentSettlementId();
            if (id == null) {
                return null;
            }
            return entityRepository.getEntity(id) as Settlement;
        }

        private static string describeEffect(Recipe recipe) {
            if (Tools.isTool(recipe.getOutput())) {
                return " (" + recipe.getBenefit() + ")";
            }
            if (FoodItems.isFood(recipe.getOutput())) {
                return " (" + FoodItems.getEnergyRestored(recipe.getOutput()) + " energy each)";
            }
            return "";
        }
    }
}
