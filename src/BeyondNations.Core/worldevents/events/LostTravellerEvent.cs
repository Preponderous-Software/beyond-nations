using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * A lost traveller with a map to a spot rich in stone, and a cache of
    * coin. The map costs a meal or some coin; following it puts real rocks
    * and a real cache in the world, a walk away in the direction it names.
    * Personal: it happens to the player, nation or not.
    */
    public class LostTravellerEvent : IWorldEvent {
        public const string Id = "traveller";
        public const float DISTANCE = 160f;
        public const int MAP_COINS = 15;
        public const int ROCKS = 4;
        public const int EXTRA_STONE_PER_ROCK = 10;
        public const int CACHE_COINS = 30;

        private static readonly string[] Directions = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

        public string getId() { return Id; }
        public string getTitle() { return "A lost traveller"; }
        public bool isPersonal() { return true; }

        public int getWeight(WorldEventContext context, Nation nation) {
            return 5;
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            // the map points into the highlands, where stone lies thick, when any
            // direction leads there; otherwise anywhere
            int start = context.getRandom().range(0, Directions.Length);
            instance.setMagnitude(start);
            Vector3 from = context.getPlayer().getPosition();
            for (int i = 0; i < Directions.Length; i++) {
                int direction = (start + i) % Directions.Length;
                instance.setMagnitude(direction);
                if (context.regionAt(getSpot(from, instance)) == Region.HIGHLANDS) {
                    return true;
                }
            }
            instance.setMagnitude(start);
            return true;
        }

        public static string getDirection(WorldEventInstance instance) {
            return Directions[((instance.getMagnitude() % Directions.Length) + Directions.Length) % Directions.Length];
        }

        /** Where the map leads, from where the player stands. Angle 0 is +X, and "north" is +Z. */
        public static Vector3 getSpot(Vector3 from, WorldEventInstance instance) {
            float angle = (instance.getMagnitude() % Directions.Length) * (System.MathF.PI / 4f);
            return new Vector3(from.X + System.MathF.Cos(angle) * DISTANCE, 1f, from.Z + System.MathF.Sin(angle) * DISTANCE);
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            Region region = context.regionAt(getSpot(context.getPlayer().getPosition(), instance));
            return "A lost traveller stumbles up to you, half-starved. In return for help they offer a map, marked with a spot rich in stone and "
                + "a buried cache, about " + (int) DISTANCE + " paces to the " + getDirection(instance) + ", in the "
                + RegionCatalog.name(region).ToLowerInvariant() + ".";
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            Inventory inventory = context.getPlayer().getInventory();
            return new List<WorldEventChoice> {
                WorldEventChoice.requiring(FoodItems.hasFood(inventory), "Share a meal for the map (1 food)",
                    "The map is yours, for one meal.", "You carry no food"),
                WorldEventChoice.requiring(inventory.getNumItems(ItemType.COIN) >= MAP_COINS, "Buy the map (" + MAP_COINS + " coins)",
                    "The map is yours, and you keep your food.", "Needs " + MAP_COINS + " coins"),
                new WorldEventChoice("Send them on their way", "Nothing gained, nothing spent."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Player player = context.getPlayer();
            if (choice == 2) {
                return "a lost traveller asked you for help, and you sent them on their way.";
            }
            if (choice == 0) {
                ItemType? food = FoodItems.getMostNourishingFood(player.getInventory());
                // the meal shared is the plainest carried
                foreach (ItemType type in FoodItems.getFoodInDescendingEnergyOrder()) {
                    if (player.getInventory().getNumItems(type) > 0) {
                        food = type;
                    }
                }
                if (food != null) {
                    player.getInventory().removeItem(food.Value, 1);
                }
            } else {
                player.getInventory().removeItem(ItemType.COIN, MAP_COINS);
            }
            Vector3 spot = getSpot(player.getPosition(), instance);
            EntityRepository entities = context.getSimulation().getEntityRepository();
            for (int i = 0; i < ROCKS; i++) {
                float angle = i * (System.MathF.PI * 2f / ROCKS);
                Rock rock = new Rock(new Vector3(spot.X + System.MathF.Cos(angle) * 6f, 1f, spot.Z + System.MathF.Sin(angle) * 6f));
                rock.getInventory().addItem(ItemType.STONE, EXTRA_STONE_PER_ROCK);
                entities.addEntity(rock);
            }
            entities.addEntity(new ItemDrop(spot, new ItemStack(ItemType.COIN, CACHE_COINS)));
            string paid = choice == 0 ? "a meal" : MAP_COINS + " coins";
            return "you traded " + paid + " to a lost traveller for a map; it marks stone and a cache about " + (int) DISTANCE
                + " paces to the " + getDirection(instance) + ".";
        }
    }
}
