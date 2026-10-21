using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations.desktop.ui.feel {

    /** What happened, as far as sound and effects care. */
    public enum FeelEventKind {
        Chop,
        Mine,
        Hunt,
        Pickup,
        Craft,
        Coin,
        BuildComplete,
        HungerWarning,
        ObjectiveComplete,
        Raided,
        /** "+5 stone" over the player; carries no sound of its own. */
        Gain
    }

    public struct FeelEvent {
        public FeelEventKind kind;
        /** Where it happened, in world space. */
        public Vector3 position;
        /** For Gain: the item and the change (negative for a loss). */
        public ItemType item;
        public int amount;
        /** For BuildComplete: true when a building went up (it rises), false for a new settlement or stall. */
        public bool rises;
    }

    /**
    * Notices what the player would want to hear and see, by comparing the
    * world with how it was on the previous frame: the player's inventory
    * (wood from a tree, stone from a rock, coins, crafted goods), their
    * energy tier, the objective count, and how many buildings each
    * settlement has. A raid on the player's nation arrives as an event.
    *
    * It only reads. It holds no simulation state that a save would need,
    * draws no random numbers, and the simulation never learns it exists;
    * the first frame after attach() only records, so loading a save or
    * starting a world is not mistaken for a haul.
    */
    public class FeelObserver {
        /** Within this, a tree, rock or chicken that vanishes with a gain was the source. */
        public const float SourceDistance = 6f;

        private static readonly ItemType[] Items = (ItemType[]) Enum.GetValues(typeof(ItemType));
        private static readonly HashSet<ItemType> Crafted = new HashSet<ItemType> {
            ItemType.AXE, ItemType.PICKAXE, ItemType.COOKED_MEAT, ItemType.PLANKS, ItemType.BRICKS
        };

        private Simulation simulation;
        private readonly int[] lastCounts = new int[Items.Length];
        private bool primed;
        private bool hadTree, hadRock, hadChicken;
        private Vector3 lastTree, lastRock, lastChicken;
        private EnergyBar.Tier lastTier;
        private int lastCompleted;
        private readonly Dictionary<EntityId, int> buildingCounts = new Dictionary<EntityId, int>();
        private readonly List<FeelEvent> pending = new List<FeelEvent>();

        /** Starts watching a world; the next observe() only records where things stand. */
        public void attach(Simulation world) {
            simulation = world;
            primed = false;
            buildingCounts.Clear();
            pending.Clear();
            if (world == null) {
                return;
            }
            Simulation watched = world;
            // Raids and new settlements are events; they are queued here and
            // handed out on the next frame. The listener stays with that
            // world and goes quiet once another one is attached.
            world.getEventRepository().subscribe(EventType.NationRaid, e => onRaid(watched, (NationRaidEvent) e));
            world.getEventRepository().subscribe(EventType.SettlementFounded, e => onFounded(watched, (SettlementFoundedEvent) e));
            world.getEventRepository().subscribe(EventType.StallBuilt, e => onStallBuilt(watched, (StallBuiltEvent) e));
        }

        public Simulation getSimulation() {
            return simulation;
        }

        private void onRaid(Simulation world, NationRaidEvent raid) {
            if (world != simulation || raid.getVictim() == null) {
                return;
            }
            NationId playerNation = world.getPlayer().getNationId();
            if (playerNation != null && playerNation.Equals(raid.getVictim().getId())) {
                pending.Add(new FeelEvent {
                    kind = FeelEventKind.Raided,
                    position = raid.getSettlement() != null ? raid.getSettlement().getPosition() : world.getPlayer().getPosition()
                });
            }
        }

        private void onFounded(Simulation world, SettlementFoundedEvent founded) {
            if (world != simulation || founded.getSettlement() == null) {
                return;
            }
            pending.Add(new FeelEvent { kind = FeelEventKind.BuildComplete, position = founded.getSettlement().getPosition() });
        }

        private void onStallBuilt(Simulation world, StallBuiltEvent built) {
            if (world != simulation || built.getSettlement() == null) {
                return;
            }
            pending.Add(new FeelEvent { kind = FeelEventKind.BuildComplete, position = built.getSettlement().getPosition() });
        }

        /**
        * Compares the world with the last frame and appends what changed to
        * output. Call once a frame, after the simulation has stepped.
        */
        public void observe(List<FeelEvent> output) {
            if (simulation == null) {
                return;
            }
            Player player = simulation.getPlayer();
            Vector3 at = player.getPosition();
            Inventory inventory = player.getInventory();
            EnergyBar.Tier tier = EnergyBar.getTier(player.getEnergy());
            int completed = simulation.getObjectiveTracker().getCompletedCount();

            if (!primed) {
                for (int i = 0; i < Items.Length; i++) {
                    lastCounts[i] = inventory.getNumItems(Items[i]);
                }
                lastTier = tier;
                lastCompleted = completed;
                scanWorld(at, null);
                pending.Clear();
                primed = true;
                return;
            }

            observeInventory(inventory, at, output);

            if (tier > lastTier && tier >= EnergyBar.Tier.Hungry) {
                output.Add(new FeelEvent { kind = FeelEventKind.HungerWarning, position = at });
            }
            lastTier = tier;

            if (completed > lastCompleted) {
                output.Add(new FeelEvent { kind = FeelEventKind.ObjectiveComplete, position = at });
            }
            lastCompleted = completed;

            scanWorld(at, output);

            output.AddRange(pending);
            pending.Clear();
        }

        private void observeInventory(Inventory inventory, Vector3 at, List<FeelEvent> output) {
            bool gainedCrafted = false;
            bool lostSomething = false;
            int wood = 0, stone = 0, meat = 0, coins = 0, other = 0;
            for (int i = 0; i < Items.Length; i++) {
                ItemType item = Items[i];
                int now = inventory.getNumItems(item);
                int delta = now - lastCounts[i];
                lastCounts[i] = now;
                if (delta == 0) {
                    continue;
                }
                if (delta > 0 || item == ItemType.COIN) {
                    output.Add(new FeelEvent { kind = FeelEventKind.Gain, position = at, item = item, amount = delta });
                }
                if (delta < 0) {
                    lostSomething = true;
                    if (item == ItemType.COIN) {
                        coins += delta;
                    }
                    continue;
                }
                switch (item) {
                    case ItemType.WOOD: wood += delta; break;
                    case ItemType.STONE: stone += delta; break;
                    case ItemType.CHICKEN_MEAT: meat += delta; break;
                    case ItemType.COIN: coins += delta; break;
                    default:
                        if (Crafted.Contains(item)) {
                            gainedCrafted = true;
                        }
                        other += delta;
                        break;
                }
            }

            // One sound for the frame's haul, from its most likely cause.
            if (gainedCrafted && lostSomething) {
                output.Add(new FeelEvent { kind = FeelEventKind.Craft, position = at });
            } else if (wood > 0 && hadTree) {
                output.Add(new FeelEvent { kind = FeelEventKind.Chop, position = lastTree });
            } else if (stone > 0 && hadRock) {
                output.Add(new FeelEvent { kind = FeelEventKind.Mine, position = lastRock });
            } else if (meat > 0 && hadChicken) {
                output.Add(new FeelEvent { kind = FeelEventKind.Hunt, position = lastChicken });
            } else if (coins != 0) {
                output.Add(new FeelEvent { kind = FeelEventKind.Coin, position = at });
            } else if (wood + stone + meat + other > 0) {
                output.Add(new FeelEvent { kind = FeelEventKind.Pickup, position = at });
            }
        }

        /**
        * One walk over the entities: the nearest tree, rock and chicken in
        * reach (for next frame's gains) and each settlement's building count.
        * Indexed, so it allocates nothing once the dictionary is warm.
        */
        private void scanWorld(Vector3 at, List<FeelEvent> output) {
            float limit = SourceDistance * SourceDistance;
            float bestTree = limit, bestRock = limit, bestChicken = limit;
            hadTree = hadRock = hadChicken = false;
            EntityRepository entities = simulation.getEntityRepository();
            int count = entities.getEntityCount();
            for (int i = 0; i < count; i++) {
                Entity entity = entities.getEntityAt(i);
                EntityType type = entity.getType();
                if (type == EntityType.SETTLEMENT) {
                    noteBuildings((Settlement) entity, output);
                    continue;
                }
                if (type != EntityType.TREE && type != EntityType.ROCK && type != EntityType.CHICKEN) {
                    continue;
                }
                if (entity.isMarkedForDeletion()) {
                    continue;
                }
                float distance = Vector3.DistanceSquared(at, entity.getPosition());
                if (type == EntityType.TREE && distance < bestTree) {
                    bestTree = distance; lastTree = entity.getPosition(); hadTree = true;
                } else if (type == EntityType.ROCK && distance < bestRock) {
                    bestRock = distance; lastRock = entity.getPosition(); hadRock = true;
                } else if (type == EntityType.CHICKEN && distance < bestChicken) {
                    bestChicken = distance; lastChicken = entity.getPosition(); hadChicken = true;
                }
            }
        }

        private void noteBuildings(Settlement settlement, List<FeelEvent> output) {
            IReadOnlyList<Building> buildings = settlement.getBuildings();
            int now = buildings.Count;
            if (buildingCounts.TryGetValue(settlement.getId(), out int before) && output != null && now > before) {
                for (int b = before; b < now; b++) {
                    Vector3 slot = SettlementLayout.getBuildingSlotOffset(buildings[b].getSlot());
                    Vector3 centre = settlement.getPosition();
                    output.Add(new FeelEvent {
                        kind = FeelEventKind.BuildComplete,
                        position = new Vector3(centre.X + slot.X, SettlementLayout.GroundTopY, centre.Z + slot.Z),
                        rises = true
                    });
                }
            }
            buildingCounts[settlement.getId()] = now;
        }
    }
}
