namespace beyondnations {

    /**
    * What a player who leads a nation can do toward another: send a gift of
    * coins, propose an alliance, or denounce it. Each answers on the status
    * line. Only the leader speaks for a nation.
    */
    public static class NationDiplomacy {

        /**
        * The player's nation, if the player leads one and the target is
        * another; otherwise null, with the reason on the status line.
        */
        public static Nation getLedNation(Player player, NationRepository nationRepository, Nation target) {
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null) {
                player.getStatus().update("You need a nation before you can deal with other nations.");
                return null;
            }
            if (!own.getLeaderId().Equals(player.getId())) {
                player.getStatus().update("Only the leader of " + own.getName() + " can speak for it.");
                return null;
            }
            if (target == null || target == own || nationRepository.getNation(target.getId()) == null) {
                player.getStatus().update("There is no such nation to deal with.");
                return null;
            }
            return own;
        }

        /** Whether the player can deal with other nations at all: they lead one. */
        public static bool playerLeads(Player player, NationRepository nationRepository) {
            Nation own = nationRepository.getNation(player.getNationId());
            return own != null && own.getLeaderId().Equals(player.getId());
        }
    }

    /** Sends 25 coins to another nation's treasury: +10 relations. */
    public class GiftNationCommand {
        private readonly NationRepository nationRepository;
        private readonly Diplomacy diplomacy;

        public GiftNationCommand(NationRepository nationRepository, Diplomacy diplomacy) {
            this.nationRepository = nationRepository;
            this.diplomacy = diplomacy;
        }

        public void execute(Player player, Nation target) {
            Nation own = NationDiplomacy.getLedNation(player, nationRepository, target);
            if (own == null) {
                return;
            }
            int coins = Diplomacy.PLAYER_GIFT_COINS;
            if (player.getInventory().getNumItems(ItemType.COIN) < coins) {
                player.getStatus().update("A gift to " + target.getName() + " costs " + coins + " coins; you carry "
                    + player.getInventory().getNumItems(ItemType.COIN) + ".");
                return;
            }
            player.getInventory().removeItem(ItemType.COIN, coins);
            diplomacy.giveCoins(target, coins);
            diplomacy.change(own, target, Diplomacy.giftValue(coins));
            player.getStatus().update(target.getName() + " accepts your gift of " + coins + " coins. Relations: "
                + nationRepository.getRelations().describe(own, target) + ".");
        }
    }

    /** Proposes an alliance, which is accepted once relations reach +30. */
    public class ProposeAllianceCommand {
        private readonly NationRepository nationRepository;

        public ProposeAllianceCommand(NationRepository nationRepository) {
            this.nationRepository = nationRepository;
        }

        public void execute(Player player, Nation target) {
            Nation own = NationDiplomacy.getLedNation(player, nationRepository, target);
            if (own == null) {
                return;
            }
            NationRelations relations = nationRepository.getRelations();
            if (relations.isAllied(own, target)) {
                player.getStatus().update("You are already allied with " + target.getName() + ".");
                return;
            }
            if (relations.getScore(own, target) < NationRelations.ALLIANCE_REQUIRES) {
                player.getStatus().update(target.getName() + " declines: relations are " + relations.describe(own, target)
                    + "; an alliance needs +" + NationRelations.ALLIANCE_REQUIRES + ". Gifts help.");
                return;
            }
            relations.formAlliance(own, target);
            player.getStatus().update(target.getName() + " agrees: " + own.getName() + " and " + target.getName() + " are now allies.");
        }
    }

    /** Denounces another nation: breaks any alliance and costs 25 points of relations. */
    public class DenounceNationCommand {
        private readonly NationRepository nationRepository;
        private readonly Diplomacy diplomacy;

        public DenounceNationCommand(NationRepository nationRepository, Diplomacy diplomacy) {
            this.nationRepository = nationRepository;
            this.diplomacy = diplomacy;
        }

        public void execute(Player player, Nation target) {
            Nation own = NationDiplomacy.getLedNation(player, nationRepository, target);
            if (own == null) {
                return;
            }
            NationRelations relations = nationRepository.getRelations();
            relations.breakAlliance(own, target);
            relations.markDenounced(own, target);
            diplomacy.change(own, target, Diplomacy.DENOUNCE);
            player.getStatus().update("You denounced " + target.getName() + ". Relations: " + relations.describe(own, target) + ".");
        }
    }
}
