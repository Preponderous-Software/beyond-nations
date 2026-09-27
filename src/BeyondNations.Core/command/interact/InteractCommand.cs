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

        public InteractCommand(Environment environment, NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository, RandomSource random) {
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

            AppleTree tree = environment.getNearestTree(player.getPosition());
            Rock rock = environment.getNearestRock(player.getPosition());
            Pawn pawn = (Pawn) environment.getNearestEntityOfType(player.getPosition(), EntityType.PAWN);
            Settlement settlement = (Settlement) environment.getNearestEntityOfType(player.getPosition(), EntityType.SETTLEMENT);
            Chicken chicken = (Chicken) environment.getNearestEntityOfType(player.getPosition(), EntityType.CHICKEN);

            if (settlement != null && Vector3.Distance(player.getPosition(), settlement.getPosition()) < 5) {
                EnterSettlementCommand enterSettlementCommand = new EnterSettlementCommand(entityRepository);
                enterSettlementCommand.execute(player, settlement);
            }
            else if (pawn != null && Vector3.Distance(player.getPosition(), pawn.getPosition()) < 5) {
                if (pawn.getNationId() == null) {
                    player.getStatus().update(pawn.getName() + ": \"I don't belong to any nation.\"");
                    return;
                }
                Nation pawnsNation = nationRepository.getNation(pawn.getNationId());

                List <string> phrases = generatePhrases(pawnsNation, pawn, player);
                string phrase = phrases[random.range(0, phrases.Count)];
                player.getStatus().update(pawn.getName() + ": \"" + phrase + "\"");
            }
            else if (pickUpItemDropsInRange(player)) {
                return;
            }
            else if (tree != null && Vector3.Distance(player.getPosition(), tree.getPosition()) < 5) {
                tree.markForDeletion();
                player.getInventory().transferContentsOfInventory(tree.getInventory());
                player.getStatus().update("Gathered wood from tree.");
            }
            else if (rock != null && Vector3.Distance(player.getPosition(), rock.getPosition()) < 5) {
                rock.markForDeletion();
                player.getInventory().transferContentsOfInventory(rock.getInventory());
                player.getStatus().update("Gathered stone from rock.");
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

        private List<string> generatePhrases(Nation pawnsNation, Pawn pawn, Player player) {
            List <string> phrases = new List<string>() {
                "Hello!",
                "How are you?",
                "Glory to " + pawnsNation.getName() + "!",
                "What's your name?",
                "I'm " + pawn.getName() + ".",
                "Have you heard of " + pawnsNation.getName() + "?",
                "Nice to meet you!",
                "I'm hungry.",
                "The weather is nice today."
            };
            if (pawnsNation.getNumberOfMembers() > 1) {
                phrases.Add("There are " + pawnsNation.getNumberOfMembers() + " members in " + pawnsNation.getName() + ".");

                if (pawnsNation.getLeaderId() == player.getId()) {
                    phrases.Add("Hey boss!");
                    phrases.Add("How's it going boss?");
                    phrases.Add("What's up boss?");
                    phrases.Add("I'm glad to be a member of " + pawnsNation.getName() + ".");
                    phrases.Add("Please don't kick me out of " + pawnsNation.getName() + ".");
                    phrases.Add("When are we going to build more houses?");
                }
            }

            return phrases;
        }
    }
}