using System.Collections.Generic;

namespace beyondnations {

    /**
    * Checks and performs crafts. Shared by the player's craft command, the
    * crafting screen (which asks what is missing) and merchant pawns, so
    * every crafter plays by the same rules.
    *
    * settlement is the settlement the crafter is standing in, or null when
    * they are out in the world. A recipe that needs a building can only be
    * made inside a settlement that has one; any settlement will do, so a
    * traveller can use a neighbour's workshop.
    */
    public static class Crafting {

        public static bool hasRequiredBuilding(Recipe recipe, Settlement settlement) {
            BuildingType? required = recipe.getRequiredBuilding();
            if (required == null) {
                return true;
            }
            return settlement != null && settlement.getNumBuildings(required.Value) > 0;
        }

        public static bool canCraft(Recipe recipe, Inventory inventory, Settlement settlement) {
            return getMissing(recipe, inventory, settlement).Count == 0;
        }

        /**
        * What stands between the crafter and this recipe, one phrase each:
        * "2 more stone", "a Workshop". Empty when it can be crafted now.
        */
        public static List<string> getMissing(Recipe recipe, Inventory inventory, Settlement settlement) {
            List<string> missing = new List<string>();
            foreach (ItemStack input in recipe.getInputs()) {
                int shortBy = input.getQuantity() - inventory.getNumItems(input.getItemType());
                if (shortBy > 0) {
                    string name = ItemNames.getSingularName(input.getItemType());
                    bool plural = shortBy != 1 && !ItemNames.isUncountable(input.getItemType());
                    missing.Add(shortBy + " more " + name + (plural ? "s" : ""));
                }
            }
            if (!hasRequiredBuilding(recipe, settlement)) {
                missing.Add("a " + BuildingCatalog.getDisplayName(recipe.getRequiredBuilding().Value)
                    + (settlement == null ? " (inside a settlement)" : " in this settlement"));
            }
            return missing;
        }

        /** How many times over the inventory could make this right now. */
        public static int getMaxCrafts(Recipe recipe, Inventory inventory, Settlement settlement) {
            if (!hasRequiredBuilding(recipe, settlement)) {
                return 0;
            }
            int max = int.MaxValue;
            foreach (ItemStack input in recipe.getInputs()) {
                int times = inventory.getNumItems(input.getItemType()) / input.getQuantity();
                if (times < max) {
                    max = times;
                }
            }
            return max == int.MaxValue ? 0 : max;
        }

        /**
        * Takes the inputs and adds the output. Returns false, and changes
        * nothing, when the recipe cannot be made here with this inventory.
        */
        public static bool craft(Recipe recipe, Inventory inventory, Settlement settlement) {
            if (!canCraft(recipe, inventory, settlement)) {
                return false;
            }
            foreach (ItemStack input in recipe.getInputs()) {
                inventory.removeItem(input.getItemType(), input.getQuantity());
            }
            inventory.addItem(recipe.getOutput(), recipe.getOutputQuantity());
            return true;
        }
    }
}
