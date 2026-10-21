using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * Who takes the player's place when they die, and what passing the rule
    * to them means.
    *
    * The heir is a member pawn of the player's nation -- never a pawn who
    * leads it, when the player is only a member there. Family comes first: a
    * pawn who shares the ruler's surname (the first ruler has none, but an
    * heir brings theirs, and children born in a settlement take a parent's,
    * so a dynasty fills out as it goes). Then whoever is most loyal to the
    * ruler, and among equals whoever joined the nation first. A pawn that is
    * itself starving to death this tick is passed over.
    *
    * The player is one entity for the whole game -- every leader id,
    * relationship and stall that names the player keeps working -- so the
    * heir is folded into it: the player takes the heir's name, place,
    * strength, belongings, home and, where they had one and the ruler did
    * not, stall; the heir pawn leaves the world. The nation, its
    * settlements and its relations are untouched. The people's loyalty to
    * the new ruler starts at half what they felt for the old one, and
    * the heir's own family warm to them at once.
    */
    public static class Succession {
        /** An heir never starts with less energy than this, so the new reign does not end at once. */
        public const float HEIR_MIN_ENERGY = 50f;
        /** How much more the heir's own family think of the new ruler. */
        public const int FAMILY_LOYALTY = 10;

        /** The last word of a name, or null for a one-word name. */
        public static string surnameOf(string name) {
            if (string.IsNullOrEmpty(name)) {
                return null;
            }
            int space = name.LastIndexOf(' ');
            return space < 0 || space == name.Length - 1 ? null : name.Substring(space + 1);
        }

        /**
        * The member who should succeed the player, or null if there is none;
        * reason says why it is them ("of your family, the Smiths"), or why
        * there is no one.
        */
        public static Pawn chooseHeir(Nation nation, Player player, EntityRepository entityRepository, out string reason) {
            string surname = surnameOf(player.getName());
            Pawn best = null;
            bool bestFamily = false;
            int bestLoyalty = 0;
            foreach (EntityId id in nation.getMemberIds()) {
                // a pawn who leads the nation keeps its crown rather than take a member's place
                if (id == nation.getLeaderId()) {
                    continue;
                }
                Pawn pawn = entityRepository.getEntity(id) as Pawn;
                if (pawn == null || pawn.isMarkedForDeletion() || pawn.getEnergy() <= 0) {
                    continue;
                }
                bool family = surname != null && surname == surnameOf(pawn.getName());
                int loyalty = PawnRelations.getRelationship(pawn, player);
                if (best == null || (family && !bestFamily) || (family == bestFamily && loyalty > bestLoyalty)) {
                    best = pawn;
                    bestFamily = family;
                    bestLoyalty = loyalty;
                }
            }
            if (best == null) {
                reason = "No one in " + nation.getName() + " was left to take your place.";
            }
            else if (bestFamily) {
                reason = "of your family, the " + surname + "s";
            }
            else if (bestLoyalty > 0) {
                reason = "the most loyal of your people";
            }
            else {
                reason = "the longest sworn of your people";
            }
            return best;
        }

        /** Folds the heir into the player, as the class comment describes. */
        public static void crown(Pawn heir, Nation nation, Player player, EntityRepository entityRepository, Diplomacy diplomacy, GameConfig gameConfig) {
            // the old ruler's belongings: left where they fell, unless the world keeps inventories
            if (!gameConfig.getKeepInventoryOnDeath()) {
                foreach (ItemDrop drop in ItemDrop.dropContentsOf(player.getInventory(), player.getPosition())) {
                    entityRepository.addEntity(drop);
                }
            }
            player.getInventory().transferContentsOfInventory(heir.getInventory());

            // where the heir stands; out of doors, if they were inside a settlement
            Vector3 at = heir.getPosition();
            if (heir.isCurrentlyInSettlement()) {
                Settlement inside = entityRepository.getEntity(heir.getCurrentSettlementId()) as Settlement;
                if (inside != null) {
                    inside.removeCurrentlyPresentEntity(heir.getId());
                    at = inside.getPosition() + new Vector3(SettlementLayout.PalisadeRadius + 5f, 0f, 0f);
                }
                heir.clearCurrentSettlementId();
            }

            // the heir's stall: theirs to keep only if the player had none and did not lead
            NationRole playerRole = nation.getRole(player.getId());
            NationRole heirRole = nation.getRole(heir.getId());
            foreach (Settlement settlement in diplomacy.getSettlements(nation)) {
                Market market = settlement.getMarket();
                foreach (Stall stall in market.getStalls()) {
                    if (stall.getOwnerId() != heir.getId()) {
                        continue;
                    }
                    if (playerRole != NationRole.LEADER && heirRole == NationRole.MERCHANT && market.getStall(player.getId()) == null) {
                        stall.setOwnerId(player.getId());
                        nation.setRole(player.getId(), NationRole.MERCHANT);
                        playerRole = NationRole.MERCHANT;
                    }
                    else {
                        stall.setOwnerId(null);
                    }
                }
            }

            string heirSurname = surnameOf(heir.getName());
            EntityId heirHome = heir.getHomeSettlementId();
            float heirEnergy = heir.getEnergy();
            // chooseHeir never names the leader, but should the heir lead, the player takes up that rule
            // too: the nation is never left led by a pawn that has left the world
            if (nation.getLeaderId() == heir.getId()) {
                nation.setLeaderId(player.getId());
                nation.setRole(player.getId(), NationRole.LEADER);
            }
            nation.removeMember(heir.getId());
            heir.setTargetEntity(null);
            heir.markForDeletion();

            // a new ruler starts with half the loyalty of the old; the heir's family warm to them
            foreach (EntityId id in nation.getMemberIds()) {
                Pawn pawn = entityRepository.getEntity(id) as Pawn;
                if (pawn == null) {
                    continue;
                }
                int before = PawnRelations.getRelationship(pawn, player);
                int after = before / 2;
                if (heirSurname != null && heirSurname == surnameOf(pawn.getName())) {
                    after += FAMILY_LOYALTY;
                }
                if (after > before) {
                    pawn.increaseRelationship(player, after - before);
                    player.increaseRelationship(pawn, after - before);
                }
                else if (after < before) {
                    pawn.decreaseRelationship(player, before - after);
                    player.decreaseRelationship(pawn, before - after);
                }
            }

            player.rename(heir.getName());
            player.setPosition(new Vector3(at.X, System.Math.Max(at.Y, 1.5f), at.Z));
            player.setVelocity(Vector3.Zero);
            player.setEnergy(System.Math.Max(heirEnergy, HEIR_MIN_ENERGY));
            if (heirHome != null) {
                player.setHomeSettlementId(heirHome);
            }
            player.setColor(nation.getColor());
            player.setCurrentSettlementId(null);
            player.setVisible(true);
        }
    }
}
