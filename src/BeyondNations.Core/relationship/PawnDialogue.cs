namespace beyondnations {

    /**
    * What a pawn says when the player talks to it. Replaces the random canned
    * phrases InteractCommand used to pick from: a line is now a greeting that
    * reflects how well the pawn knows the player, followed by the single most
    * pressing thing on its mind -- hunger first, then an order it is working
    * on, then where it stands with nations (and whether it could be
    * recruited), then what it is doing and where it lives.
    *
    * A pawn with traits ends with a word about itself (PawnTraits.aside), and
    * an ambitious or loyal one answers questions of allegiance in character.
    *
    * Deterministic on purpose: the same situation always produces the same
    * line, so what a pawn says is information the player can act on.
    */
    public static class PawnDialogue {

        public static string compose(Pawn pawn, Player player, NationRepository nationRepository) {
            Nation pawnNation = pawn.getNationId() == null ? null : nationRepository.getNation(pawn.getNationId());
            Nation playerNation = player.getNationId() == null ? null : nationRepository.getNation(player.getNationId());
            RelationshipTier tier = PawnRelations.getTier(pawn, player);
            bool playerLeadsPawn = pawnNation != null && pawnNation.getLeaderId() == player.getId();

            string line = greeting(tier, playerLeadsPawn) + " " + remark(pawn, player, pawnNation, playerNation, tier, playerLeadsPawn);
            // and a word about itself, by its first trait (PawnTraits), unless it is starving
            string aside = pawn.needsFood() ? null : PawnTraits.aside(pawn);
            return aside == null ? line : line + " " + aside;
        }

        private static string greeting(RelationshipTier tier, bool playerLeadsPawn) {
            if (playerLeadsPawn) {
                switch (tier) {
                    case RelationshipTier.LOYAL: return "Whatever you need, boss.";
                    case RelationshipTier.FRIEND: return "Good to see you, boss!";
                    case RelationshipTier.ACQUAINTANCE: return "Hello, leader.";
                    default: return "Yes, leader?";
                }
            }
            switch (tier) {
                case RelationshipTier.LOYAL: return "Always good to see you!";
                case RelationshipTier.FRIEND: return "Hello, friend!";
                case RelationshipTier.ACQUAINTANCE: return "Oh, hello again.";
                default: return "Who are you?";
            }
        }

        private static string remark(Pawn pawn, Player player, Nation pawnNation, Nation playerNation, RelationshipTier tier, bool playerLeadsPawn) {
            if (pawn.needsFood()) {
                return tier >= RelationshipTier.FRIEND
                    ? "I'm starving -- could you spare some food?"
                    : "I'm so hungry I can hardly think.";
            }

            NationOrder order = pawn.getOrder();
            if (order != null && !order.isComplete() && pawnNation != null && order.getNationId() == pawnNation.getId()) {
                return "I'm gathering " + order.getRemaining() + " more " + order.getItemType().ToString().ToLower() + " for " + pawnNation.getName() + ".";
            }

            if (pawn.isSettler() && pawnNation != null) {
                return "Home got crowded, so I'm off to found a new settlement for " + pawnNation.getName() + ".";
            }

            if (pawnNation == null) {
                if (pawn.isSettler()) {
                    return "I've left my old nation behind. I'll found one of my own.";
                }
                if (pawn.hasTrait(PawnTrait.AMBITIOUS)) {
                    return "I don't belong to any nation. I mean to found my own.";
                }
                if (playerNation != null && tier >= RelationshipTier.ACQUAINTANCE) {
                    return "I don't belong to any nation. I'd join " + playerNation.getName() + " if you asked.";
                }
                return "I don't belong to any nation.";
            }

            if (playerNation == null || pawnNation.getId() != playerNation.getId()) {
                if (pawnNation.getLeaderId() == pawn.getId()) {
                    return "I lead " + pawnNation.getName() + ". We are " + pawnNation.getNumberOfMembers() + " strong.";
                }
                if (playerNation != null && tier >= RelationshipTier.LOYAL
                        && PawnRelations.getRelationship(pawn, player) < PawnTraits.relationshipToDefect(pawn)) {
                    return "I'd do almost anything for you, but I won't leave " + pawnNation.getName() + ".";
                }
                if (playerNation != null && tier >= RelationshipTier.LOYAL) {
                    return "Between us, I'd leave " + pawnNation.getName() + " for " + playerNation.getName() + " if you asked.";
                }
                return "I'm loyal to " + pawnNation.getName() + ".";
            }

            // Same nation as the player.
            switch (pawn.getCurrentBehaviorType()) {
                case BehaviorType.GATHER_RESOURCES:
                    return "I'm out gathering for " + pawnNation.getName() + ".";
                case BehaviorType.SELL_RESOURCES:
                    return "I'm off to sell at the market.";
                case BehaviorType.GO_TO_HOME_SETTLEMENT:
                    return "I'm heading home.";
                case BehaviorType.PLANT_SAPLING:
                    return "I'm planting a tree for the next generation.";
            }
            if (pawn.getHomeSettlementId() == null) {
                return "I still need a settlement to call home.";
            }
            if (playerLeadsPawn && tier < RelationshipTier.ACQUAINTANCE) {
                return "I hardly know you. Why should I follow you?";
            }
            return pawnNation.getName() + " is " + pawnNation.getNumberOfMembers() + " strong now. I'm glad to be part of it.";
        }
    }
}
