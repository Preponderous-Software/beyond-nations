using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    public class InteractCommand {
        private RandomSource random;
        private Environment environment;
        private NationRepository nationRepository;
        private EventProducer eventProducer;
        private EntityRepository entityRepository;
        private TickCounter tickCounter;

        public InteractCommand(Environment environment, NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository, RandomSource random)
            : this(environment, nationRepository, eventProducer, entityRepository, random, null) {
        }

        /**
        * The tick counter times the talk cooldown (see TalkToPawnCommand).
        * Without one every conversation happens at tick 0, so only the first
        * ever warms a relationship.
        */
        public InteractCommand(Environment environment, NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository, RandomSource random, TickCounter tickCounter) {
            this.tickCounter = tickCounter;
            this.random = random;
            this.environment = environment;
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            if (player.isCurrentlyInSettlement()) {
                ExitSettlementCommand exitSettlementCommand = new ExitSettlementCommand(entityRepository);
                exitSettlementCommand.execute(player);
                return;
            }

            // Drops come first: a pawn can die within reach of its settlement,
            // and if entering the settlement won, those drops could never be
            // collected. Once they are picked up, the next press enters.
            if (pickUpItemDropsInRange(player)) {
                return;
            }

            AppleTree tree = environment.getNearestTree(player.getPosition());
            Rock rock = environment.getNearestRock(player.getPosition());
            // a resident strolling about a plaza is as reachable as one in the wilds
            Pawn pawn = environment.getNearestVisiblePawn(player.getPosition());
            Settlement settlement = (Settlement) environment.getNearestEntityOfType(player.getPosition(), EntityType.SETTLEMENT);
            Chicken chicken = (Chicken) environment.getNearestEntityOfType(player.getPosition(), EntityType.CHICKEN);

            // The well comes before a pawn: residents mill 3 to 16 units from
            // it, so if a nearer pawn won, E by the well would rarely enter.
            // Anywhere else on the plaza E talks, and inside, the command bar,
            // G and R reach the same pawns while E leaves.
            if (settlement != null && Vector3.Distance(player.getPosition(), settlement.getPosition()) < 5) {
                EnterSettlementCommand enterSettlementCommand = new EnterSettlementCommand(entityRepository);
                enterSettlementCommand.execute(player, settlement);
            }
            else if (pawn != null && Vector3.Distance(player.getPosition(), pawn.getPosition()) < 5) {
                new TalkToPawnCommand(nationRepository, eventProducer, tickCounter).execute(player, pawn);
            }
            else if (tree != null && Vector3.Distance(player.getPosition(), tree.getPosition()) < 5) {
                tree.markForDeletion();
                // A tree carries apples as well as wood, and the message used
                // to name only the wood, so apples -- the food a hungry player
                // is looking for -- arrived unannounced.
                int wood = tree.getInventory().getNumItems(ItemType.WOOD);
                int apples = tree.getInventory().getNumItems(ItemType.APPLE);
                player.getInventory().transferContentsOfInventory(tree.getInventory());
                GatherBonus bonus = Tools.applyGatherBonus(player.getInventory(), EntityType.TREE);
                player.getStatus().update((apples > 0
                    ? "Gathered " + (wood + bonus.getAmount()) + " wood and " + apples + (apples == 1 ? " apple" : " apples") + " from tree"
                    : "Gathered " + (wood + bonus.getAmount()) + " wood from tree") + describeBonus(bonus, player.getInventory()));
            }
            else if (rock != null && Vector3.Distance(player.getPosition(), rock.getPosition()) < 5) {
                rock.markForDeletion();
                int stone = rock.getInventory().getNumItems(ItemType.STONE);
                player.getInventory().transferContentsOfInventory(rock.getInventory());
                GatherBonus bonus = Tools.applyGatherBonus(player.getInventory(), EntityType.ROCK);
                player.getStatus().update("Gathered " + (stone + bonus.getAmount()) + " stone from rock" + describeBonus(bonus, player.getInventory()));
            }
            else if (chicken != null && Vector3.Distance(player.getPosition(), chicken.getPosition()) < 5) {
                chicken.markForDeletion();
                player.getInventory().transferContentsOfInventory(chicken.getInventory());
                player.getStatus().update("Harvested chicken meat.");
            }
            else {
                player.getStatus().update("No entities within range to interact with.");
            }
        }

        /**
        * Whether a press of Interact out in the world would act on something:
        * a drop, settlement, pawn, tree, rock or chicken within the same reach
        * execute() uses. Inside a settlement it is false, since there the
        * press means "leave", which the command bar offers by name. Touch
        * screens use this so a tap on the world interacts only when there is
        * something to interact with, rather than answering every stray tap
        * with "No entities within range". Changes nothing.
        */
        public bool hasTargetInRange(Player player) {
            if (player.isCurrentlyInSettlement()) {
                return false;
            }
            Vector3 position = player.getPosition();
            foreach (Entity entity in entityRepository.getEntitiesOfType(EntityType.ITEM_DROP)) {
                if (!entity.isMarkedForDeletion() && Vector3.Distance(position, entity.getPosition()) < 5) {
                    return true;
                }
            }
            Entity[] nearest = {
                environment.getNearestEntityOfType(position, EntityType.SETTLEMENT),
                environment.getNearestVisiblePawn(position),
                environment.getNearestTree(position),
                environment.getNearestRock(position),
                environment.getNearestEntityOfType(position, EntityType.CHICKEN),
            };
            foreach (Entity entity in nearest) {
                if (entity != null && Vector3.Distance(position, entity.getPosition()) < 5) {
                    return true;
                }
            }
            return false;
        }

        /**
        * The end of a gather message: what the tool added and how worn it is,
        * or that it broke. Without a tool, a hint that one would help.
        */
        public static string describeBonus(GatherBonus bonus, Inventory inventory) {
            if (bonus.getTool() == null) {
                return ".";
            }
            ItemType tool = bonus.getTool().Value;
            string name = ItemNames.getSingularName(tool);
            string extra = " (+" + bonus.getAmount() + " from your " + name + ")";
            if (bonus.didBreak()) {
                int left = inventory.getNumItems(tool);
                return extra + ". Your " + name + " wore out" + (left > 0 ? "; " + left + " left." : "!");
            }
            return extra + ", " + Tools.getUsesLeft(inventory, tool) + " uses left.";
        }

        /**
        * Picks up every drop within reach at once, since a death leaves one
        * drop per item type stacked on the same spot. Drops already marked for
        * deletion are skipped so a second press before the next tick cannot
        * collect the same stack twice.
        */
        private bool pickUpItemDropsInRange(Player player) {
            List<string> pickedUp = new List<string>();
            foreach (Entity entity in entityRepository.getEntitiesOfType(EntityType.ITEM_DROP)) {
                if (entity.isMarkedForDeletion() || Vector3.Distance(player.getPosition(), entity.getPosition()) >= 5) {
                    continue;
                }
                ItemStack stack = ((ItemDrop) entity).getItemStack();
                player.getInventory().addItem(stack.getItemType(), stack.getQuantity());
                entity.markForDeletion();
                pickedUp.Add(entity.getName());
            }
            if (pickedUp.Count == 0) {
                return false;
            }
            player.getStatus().update("Picked up " + string.Join(", ", pickedUp) + ".");
            return true;
        }
    }
}