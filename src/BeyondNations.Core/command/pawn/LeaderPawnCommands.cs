using System.Collections.Generic;

namespace beyondnations {

    /**
    * What a nation's leader can ask of one of its member pawns. Only the
    * player's own nation is in scope: these are the levers that make leading
    * a nation more than a title.
    */
    public static class LeaderPawnCommands {

        /** The player leads the nation the pawn belongs to (and the pawn is not the player). */
        public static bool playerLeads(Player player, Pawn pawn, NationRepository nationRepository) {
            if (player.getNationId() == null || pawn.getNationId() != player.getNationId()) {
                return false;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            return nation != null && nation.getLeaderId() == player.getId();
        }

        /** Whether the pawn's home settlement has a stall it could be handed. */
        public static bool canAppointMerchant(Player player, Pawn pawn, NationRepository nationRepository, EntityRepository entityRepository) {
            if (!playerLeads(player, pawn, nationRepository)) {
                return false;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getRole(pawn.getId()) != NationRole.SERF) {
                return false;
            }
            Settlement home = getHome(pawn, entityRepository);
            return home != null && home.getMarket().getNumStallsForSale() > 0;
        }

        /**
        * Whether the leader could make this serf a trader (#167): the pawn
        * has a home, and the nation has fewer traders than settlements. The
        * stake (Traders.TRADER_STAKE) is checked when the command runs, so
        * the button can say what is missing.
        */
        public static bool canAppointTrader(Player player, Pawn pawn, NationRepository nationRepository) {
            if (!playerLeads(player, pawn, nationRepository)) {
                return false;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            return nation.getRole(pawn.getId()) == NationRole.SERF && pawn.getHomeSettlementId() != null
                && Traders.hasRoomForTrader(nation);
        }

        /**
        * Why the leader cannot make this member a trader right now, or null
        * when they can (#487: with the nation's one trader slot taken the
        * button simply vanished, and nothing said why). Only for a member of
        * the player's own nation other than the leader; the command bar
        * shows Appoint Trader greyed with this reason.
        */
        public static string whyNotAppointTrader(Player player, Pawn pawn, NationRepository nationRepository, EntityRepository entityRepository) {
            if (!playerLeads(player, pawn, nationRepository)) {
                return "Only the leader of " + pawn.getName() + "'s nation can appoint it.";
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            NationRole role = nation.getRole(pawn.getId());
            if (role == NationRole.TRADER) {
                return pawn.getName() + " is already your trader.";
            }
            if (role != NationRole.SERF) {
                return pawn.getName() + " is already a " + role.ToString().ToLower() + ".";
            }
            if (pawn.getHomeSettlementId() == null) {
                return pawn.getName() + " has no home settlement to trade from.";
            }
            if (!Traders.hasRoomForTrader(nation)) {
                return describeTraderSlotsFull(nation, entityRepository);
            }
            if (player.getInventory().getNumItems(ItemType.COIN) < Traders.TRADER_STAKE) {
                return "A trader needs a stake of " + Traders.TRADER_STAKE + " coins from you.";
            }
            return null;
        }

        /**
        * "One trader per settlement: Charlotte Evans already trades for the
        * Camp of Farmshire." -- the issue's own wording -- naming who holds
        * each slot, and how to open another.
        */
        public static string describeTraderSlotsFull(Nation nation, EntityRepository entityRepository) {
            List<string> holders = new List<string>();
            foreach (EntityId memberId in nation.getMemberIds()) {
                if (nation.getRole(memberId) != NationRole.TRADER) {
                    continue;
                }
                Pawn trader = entityRepository.getEntity(memberId) as Pawn;
                if (trader == null) {
                    continue;
                }
                Settlement home = getHome(trader, entityRepository);
                holders.Add(trader.getName() + (home == null ? "" : " (" + home.getDisplayName() + ")"));
            }
            string who = holders.Count == 0 ? "" : (holders.Count == 1 ? " " + holders[0] + " already trades for " + nation.getName() + "."
                : " Already trading: " + string.Join(", ", holders) + ".");
            return "One trader per settlement:" + who + " Found another settlement to take on another trader.";
        }

        public static Settlement getHome(Pawn pawn, EntityRepository entityRepository) {
            if (pawn.getHomeSettlementId() == null) {
                return null;
            }
            return entityRepository.getEntity(pawn.getHomeSettlementId()) as Settlement;
        }
    }

    /**
    * A leader orders a member pawn to gather for the nation. The pawn has to
    * know the leader (Acquaintance) to take orders; a stranger shrugs it off.
    * Delivery happens on its own as the pawn goes about its life -- see
    * NationOrderDelivery.
    */
    public class OrderPawnToGatherCommand {
        public const RelationshipTier TIER_REQUIRED = RelationshipTier.ACQUAINTANCE;

        private readonly NationRepository nationRepository;

        public OrderPawnToGatherCommand(NationRepository nationRepository) {
            this.nationRepository = nationRepository;
        }

        public void execute(Player player, Pawn pawn) {
            execute(player, pawn, ItemType.WOOD, NationOrder.DEFAULT_QUANTITY);
        }

        public void execute(Player player, Pawn pawn, ItemType itemType, int quantity) {
            if (!LeaderPawnCommands.playerLeads(player, pawn, nationRepository)) {
                player.getStatus().update("Only the leader of " + pawn.getName() + "'s nation can give it orders.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            string who = PawnRelations.describe(pawn, player);
            if (nation.getNumberOfSettlements() == 0) {
                player.getStatus().update(who + ": \"Gather for what? We have no settlement to bring it to.\"");
                return;
            }
            if (PawnRelations.getTier(pawn, player) < TIER_REQUIRED) {
                player.getStatus().update(who + ": \"I hardly know you. Why should I take your orders?\" (needs " + RelationshipTiers.getName(TIER_REQUIRED) + ")");
                return;
            }
            NationOrder current = pawn.getOrder();
            if (current != null && !current.isComplete() && current.getNationId() == nation.getId()) {
                player.getStatus().update(who + ": \"I'm already on it -- " + current.describe() + ".\"");
                return;
            }
            NationOrder order = new NationOrder(nation.getId(), itemType, quantity);
            pawn.setOrder(order);
            player.getStatus().update(who + ": \"I'll " + order.describeTask() + " for " + nation.getName() + ".\"");
        }
    }

    /**
    * A leader hands a member pawn a free stall in its home settlement,
    * making it a merchant. A stall costs a pawn a hundred coins, so this is
    * the biggest reward a leader can give, and the pawn remembers it.
    */
    public class AppointMerchantCommand {
        public const int RELATIONSHIP_GAIN = 6;

        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;
        private readonly EventProducer eventProducer;

        public AppointMerchantCommand(NationRepository nationRepository, EntityRepository entityRepository, EventProducer eventProducer) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
            this.eventProducer = eventProducer;
        }

        public void execute(Player player, Pawn pawn) {
            if (!LeaderPawnCommands.playerLeads(player, pawn, nationRepository)) {
                player.getStatus().update("Only the leader of " + pawn.getName() + "'s nation can appoint it.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getRole(pawn.getId()) != NationRole.SERF) {
                player.getStatus().update(pawn.getName() + " is already a " + nation.getRole(pawn.getId()).ToString().ToLower() + ".");
                return;
            }
            Settlement home = LeaderPawnCommands.getHome(pawn, entityRepository);
            if (home == null) {
                player.getStatus().update(pawn.getName() + " has no home settlement to keep a stall in.");
                return;
            }
            Stall stall = home.getMarket().getStallForSale();
            if (stall == null) {
                player.getStatus().update("There is no free stall in " + pawn.getName() + "'s settlement. Build one first.");
                return;
            }

            stall.setOwnerId(pawn.getId());
            nation.setRole(pawn.getId(), NationRole.MERCHANT);
            bool tierUp = PawnRelations.raise(pawn, player, RELATIONSHIP_GAIN, eventProducer);
            player.getStatus().update("You appointed " + pawn.getName() + " a merchant. " + PawnRelations.describe(pawn, player)
                + ": \"A stall of my own? I won't let you down!\"" + PawnRelations.tierUpSuffix(tierUp, pawn, player));
        }
    }

    /**
    * A leader makes a member serf a trader (#167, Traders), handing it the
    * stake it buys its first load with. The trader keeps no stall: it buys
    * where goods are cheap and carries them to whichever market in reach
    * pays more. A nation keeps one trader for each settlement it holds.
    */
    public class AppointTraderCommand {
        public const int RELATIONSHIP_GAIN = 4;

        private readonly NationRepository nationRepository;
        private readonly EventProducer eventProducer;
        private readonly EntityRepository entityRepository;

        // entityRepository is optional so existing callers keep compiling;
        // with it, a full set of trader slots names who holds them (#487)
        public AppointTraderCommand(NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository = null) {
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.entityRepository = entityRepository;
        }

        public bool execute(Player player, Pawn pawn) {
            if (!LeaderPawnCommands.playerLeads(player, pawn, nationRepository)) {
                player.getStatus().update("Only the leader of " + pawn.getName() + "'s nation can appoint it.");
                return false;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getRole(pawn.getId()) != NationRole.SERF) {
                player.getStatus().update(pawn.getName() + " is already a " + nation.getRole(pawn.getId()).ToString().ToLower() + ".");
                return false;
            }
            if (!Traders.hasRoomForTrader(nation)) {
                player.getStatus().update(entityRepository != null
                    ? LeaderPawnCommands.describeTraderSlotsFull(nation, entityRepository)
                    : nation.getName() + " already has a trader for each of its settlements.");
                return false;
            }
            if (player.getInventory().getNumItems(ItemType.COIN) < Traders.TRADER_STAKE) {
                player.getStatus().update("A trader needs a stake of " + Traders.TRADER_STAKE + " coins to buy a first load.");
                return false;
            }
            player.getInventory().removeItem(ItemType.COIN, Traders.TRADER_STAKE);
            pawn.getInventory().addItem(ItemType.COIN, Traders.TRADER_STAKE);
            nation.setRole(pawn.getId(), NationRole.TRADER);
            bool tierUp = PawnRelations.raise(pawn, player, RELATIONSHIP_GAIN, eventProducer);
            player.getStatus().update("You made " + pawn.getName() + " a trader, with a stake of " + Traders.TRADER_STAKE + " coins. "
                + PawnRelations.describe(pawn, player) + ": \"Buy low, sell high. I'll be on the road.\""
                + PawnRelations.tierUpSuffix(tierUp, pawn, player));
            eventProducer?.produceTraderEvent(new TraderEvent(TraderEventKind.Appointed, pawn, nation.getId(),
                "You made " + pawn.getName() + " a trader for " + nation.getName() + ", with a stake of " + Traders.TRADER_STAKE + " coins."));
            return true;
        }
    }
}
