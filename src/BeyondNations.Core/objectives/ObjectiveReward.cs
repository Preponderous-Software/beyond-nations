using System.Collections.Generic;

namespace beyondnations {

    /**
    * What completing an objective gives the player: zero or more stacks of
    * items, coins included, added straight to the player's inventory.
    */
    public class ObjectiveReward {
        public static readonly ObjectiveReward None = new ObjectiveReward();

        private readonly List<KeyValuePair<ItemType, int>> items = new List<KeyValuePair<ItemType, int>>();

        public static ObjectiveReward coins(int amount) {
            return new ObjectiveReward().and(ItemType.COIN, amount);
        }

        public static ObjectiveReward of(ItemType itemType, int quantity) {
            return new ObjectiveReward().and(itemType, quantity);
        }

        public ObjectiveReward and(ItemType itemType, int quantity) {
            if (quantity > 0) {
                items.Add(new KeyValuePair<ItemType, int>(itemType, quantity));
            }
            return this;
        }

        public IReadOnlyList<KeyValuePair<ItemType, int>> getItems() {
            return items;
        }

        public bool isEmpty() {
            return items.Count == 0;
        }

        public void grantTo(Inventory inventory) {
            foreach (KeyValuePair<ItemType, int> item in items) {
                inventory.addItem(item.Key, item.Value);
            }
        }

        /** "+25 coins, +2 apples", or an empty string for no reward. */
        public string describe() {
            List<string> parts = new List<string>(items.Count);
            foreach (KeyValuePair<ItemType, int> item in items) {
                parts.Add("+" + item.Value + " " + displayName(item.Key, item.Value));
            }
            return string.Join(", ", parts);
        }

        /** "coins", "apple", "chicken meat": lower case, plural where it reads right. */
        public static string displayName(ItemType itemType, int quantity) {
            string name = itemType.ToString().ToLowerInvariant().Replace('_', ' ');
            if (quantity == 1 || itemType == ItemType.WOOD || itemType == ItemType.STONE || itemType == ItemType.CHICKEN_MEAT) {
                return name;
            }
            return name + "s";
        }
    }
}
