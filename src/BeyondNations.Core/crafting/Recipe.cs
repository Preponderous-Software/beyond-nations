using System.Collections.Generic;

namespace beyondnations {

    /**
    * One way of turning items into another item: what goes in, what comes
    * out, and the building (if any) the crafter has to be standing in. The
    * recipes themselves are data in RecipeBook; Crafting does the checking
    * and the exchange.
    */
    public class Recipe {
        private readonly string name;
        private readonly ItemType output;
        private readonly int outputQuantity;
        private readonly List<ItemStack> inputs;
        private readonly BuildingType? requiredBuilding;
        private readonly string benefit;

        public Recipe(string name, ItemType output, int outputQuantity, List<ItemStack> inputs, BuildingType? requiredBuilding, string benefit) {
            this.name = name;
            this.output = output;
            this.outputQuantity = outputQuantity;
            this.inputs = inputs;
            this.requiredBuilding = requiredBuilding;
            this.benefit = benefit;
        }

        /** What the player sees: "Axe", "Cooked Meat". */
        public string getName() {
            return name;
        }

        public ItemType getOutput() {
            return output;
        }

        public int getOutputQuantity() {
            return outputQuantity;
        }

        public IReadOnlyList<ItemStack> getInputs() {
            return inputs;
        }

        /** How many of this item one craft uses, 0 when it is not an input. */
        public int getInputQuantity(ItemType itemType) {
            foreach (ItemStack input in inputs) {
                if (input.getItemType() == itemType) {
                    return input.getQuantity();
                }
            }
            return 0;
        }

        /** The building the crafter must be inside, or null to craft anywhere. */
        public BuildingType? getRequiredBuilding() {
            return requiredBuilding;
        }

        /** One line on why it is worth making: "+3 wood per tree, lasts 20 trees". */
        public string getBenefit() {
            return benefit;
        }

        /** "4 wood, 2 stone" */
        public string describeInputs() {
            List<string> parts = new List<string>();
            foreach (ItemStack input in inputs) {
                parts.Add(ItemNames.describeQuantity(input.getItemType(), input.getQuantity()));
            }
            return string.Join(", ", parts);
        }

        /** "2 planks" */
        public string describeOutput() {
            return ItemNames.describeQuantity(output, outputQuantity);
        }
    }
}
