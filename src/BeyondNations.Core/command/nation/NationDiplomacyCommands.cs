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

        /**
        * Whether an alliance is worth proposing: not allied already, not at
        * war, and not at odds (Rival or worse) -- no nation allies with one
        * it is quarrelling with, so the button is not offered (playtest
        * 0.4.1: Propose Alliance stayed on the bar for a Hostile nation).
        * Peace comes first. The nations screen and the command bar both ask
        * this, so they never disagree.
        */
        public static bool canProposeAlliance(NationRepository nationRepository, Realm realm, Nation own, Nation target) {
            if (own == null || target == null || own == target) {
                return false;
            }
            NationRelations relations = nationRepository.getRelations();
            return !relations.isAllied(own, target)
                && (realm == null || !realm.isAtWar(own, target))
                && relations.getState(own, target) > NationRelationState.RIVAL;
        }

        /** Whether war can be declared: on any nation the player's is not already at war with, a Rival included. */
        public static bool canDeclareWar(Realm realm, Nation own, Nation target) {
            return own != null && target != null && own != target && !realm.isAtWar(own, target);
        }

        /** Whether the player can deal with other nations at all: they lead one. */
        public static bool playerLeads(Player player, NationRepository nationRepository) {
            Nation own = nationRepository.getNation(player.getNationId());
            return own != null && own.getLeaderId().Equals(player.getId());
        }
    }

    /**
    * Sends 25 coins to another nation's treasury: +10 relations. Sent to a
    * stronger nation the player is at war or at odds with, it is tribute,
    * and buys a truce (realm/Realm) in which that nation neither raids nor
    * besieges the player's.
    */
    public class GiftNationCommand {
        private readonly NationRepository nationRepository;
        private readonly Diplomacy diplomacy;
        private readonly Realm realm;

        public GiftNationCommand(NationRepository nationRepository, Diplomacy diplomacy) : this(nationRepository, diplomacy, null) {
        }

        public GiftNationCommand(NationRepository nationRepository, Diplomacy diplomacy, Realm realm) {
            this.nationRepository = nationRepository;
            this.diplomacy = diplomacy;
            this.realm = realm;
        }

        /** Whether a gift to this nation would be tribute: it is stronger, and at war or at odds with the player's. */
        public static bool isTribute(NationRepository nationRepository, Diplomacy diplomacy, Realm realm, Nation own, Nation target) {
            if (realm == null || own == null || target == null) {
                return false;
            }
            bool atOdds = realm.isAtWar(own, target) || nationRepository.getRelations().getState(own, target) <= NationRelationState.RIVAL;
            return atOdds && diplomacy.getStrength(target) > diplomacy.getStrength(own);
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
            bool tribute = isTribute(nationRepository, diplomacy, realm, own, target);
            player.getInventory().removeItem(ItemType.COIN, coins);
            diplomacy.giveCoins(target, coins);
            diplomacy.change(own, target, Diplomacy.giftValue(coins));
            if (tribute) {
                realm.grantTruce(own, target);
                player.getStatus().update(target.getName() + " accepts your tribute of " + coins + " coins and will hold off its raiders for "
                    + (Realm.TRUCE_TICKS / TickCounter.DefaultTicksPerSecond) + " seconds. Relations: " + nationRepository.getRelations().describe(own, target) + ".");
                return;
            }
            player.getStatus().update(target.getName() + " accepts your gift of " + coins + " coins. Relations: "
                + nationRepository.getRelations().describe(own, target) + ".");
        }
    }

    /**
    * Proposes an alliance, which is accepted once relations reach +30. With
    * the realm in hand the answer weighs strength too: no nation allies
    * with one it is at war with, and a nation twice the player's strength
    * wants +60 before it takes on so small a partner.
    */
    public class ProposeAllianceCommand {
        /** What a nation at least twice the player's strength wants before it allies. */
        public const int STRONG_PARTNER_REQUIRES = 60;

        private readonly NationRepository nationRepository;
        private readonly Realm realm;

        public ProposeAllianceCommand(NationRepository nationRepository) : this(nationRepository, null) {
        }

        public ProposeAllianceCommand(NationRepository nationRepository, Realm realm) {
            this.nationRepository = nationRepository;
            this.realm = realm;
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
            if (realm != null && realm.isAtWar(own, target)) {
                player.getStatus().update(target.getName() + " will not ally with a nation it is at war with. Offer peace first.");
                return;
            }
            if (relations.getState(own, target) <= NationRelationState.RIVAL) {
                player.getStatus().update(target.getName() + " will not talk of an alliance while relations are " + relations.describe(own, target)
                    + ". Offer peace or send tribute first.");
                return;
            }
            if (relations.getScore(own, target) < NationRelations.ALLIANCE_REQUIRES) {
                player.getStatus().update(target.getName() + " declines: relations are " + relations.describe(own, target)
                    + "; an alliance needs +" + NationRelations.ALLIANCE_REQUIRES + ". Gifts help.");
                return;
            }
            if (realm != null) {
                int ours = realm.getDiplomacy().getStrength(own);
                int theirs = realm.getDiplomacy().getStrength(target);
                if (theirs >= 2 * ours && relations.getScore(own, target) < STRONG_PARTNER_REQUIRES) {
                    player.getStatus().update(target.getName() + " declines: they are " + theirs + " to your " + ours
                        + " and want relations of +" + STRONG_PARTNER_REQUIRES + " before taking on so small a partner (now "
                        + relations.describe(own, target) + ").");
                    return;
                }
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
