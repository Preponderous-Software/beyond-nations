using System.Collections.Generic;

namespace beyondnations {

    /**
    * Gives the player's nation a new name (#175). Only its leader can. The
    * banners and nametags of its settlements take the new name at once; the
    * chronicle keeps the old name in what it has already written, as a
    * history would.
    */
    public class RenameNationCommand {
        public const int MinLength = 3;
        public const int MaxLength = 24;

        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;
        private readonly NationNameGenerator nameGenerator;

        public RenameNationCommand(NationRepository nationRepository, EntityRepository entityRepository, NationNameGenerator nameGenerator) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
            this.nameGenerator = nameGenerator;
        }

        /** The name with its spaces tidied: trimmed, and runs of spaces made one. */
        public static string tidy(string name) {
            if (name == null) {
                return "";
            }
            return string.Join(" ", name.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        /**
        * Why the name cannot be taken, or null when it can: too short, too
        * long, the nation's name already, or another nation's.
        */
        public static string validate(string name, Nation own, NationRepository nationRepository) {
            string tidied = tidy(name);
            if (tidied.Length < MinLength) {
                return "A nation's name needs at least " + MinLength + " letters.";
            }
            if (tidied.Length > MaxLength) {
                return "A nation's name can be at most " + MaxLength + " letters.";
            }
            foreach (char c in tidied) {
                if (!(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '\'')) {
                    return "A nation's name can hold only letters, digits, spaces, hyphens and apostrophes.";
                }
            }
            if (own != null && string.Equals(tidied, own.getName(), System.StringComparison.OrdinalIgnoreCase)) {
                return own.getName() + " already has that name.";
            }
            foreach (Nation nation in nationRepository.getNations()) {
                if (nation != own && string.Equals(tidied, nation.getName(), System.StringComparison.OrdinalIgnoreCase)) {
                    return "Another nation is already called " + nation.getName() + ".";
                }
            }
            return null;
        }

        /** Returns whether the nation was renamed; the status line says what happened either way. */
        public bool execute(Player player, string newName) {
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null) {
                player.getStatus().update("You belong to no nation to rename.");
                return false;
            }
            if (own.getLeaderId() != player.getId()) {
                player.getStatus().update("Only the leader of " + own.getName() + " can rename it.");
                return false;
            }
            string refusal = validate(newName, own, nationRepository);
            if (refusal != null) {
                player.getStatus().update(refusal);
                return false;
            }
            string oldName = own.getName();
            string name = tidy(newName);
            own.setName(name);
            nameGenerator?.reserve(name);
            foreach (EntityId settlementId in own.getSettlements()) {
                Settlement settlement = entityRepository.getEntity(settlementId) as Settlement;
                settlement?.renameNation(name);
            }
            player.getStatus().update(oldName + " is now called " + name + ".");
            return true;
        }
    }

    /**
    * Hands the rule of the player's nation to another member (#175). The
    * player stays in the nation, as a merchant if they keep a stall in it
    * and a serf otherwise, and the new leader rules it as any pawn-led
    * nation is ruled. Settlements nobody was given go with the crown
    * (SettlementOwnership); one given to the player stays theirs.
    */
    public class TransferLeadershipCommand {
        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;
        private readonly EventProducer eventProducer;

        public TransferLeadershipCommand(NationRepository nationRepository, EntityRepository entityRepository, EventProducer eventProducer) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
            this.eventProducer = eventProducer;
        }

        /** The members the rule can go to: living pawns of the nation, oldest first. */
        public static List<Pawn> candidates(Nation nation, EntityRepository entityRepository) {
            List<Pawn> pawns = new List<Pawn>();
            if (nation == null) {
                return pawns;
            }
            foreach (EntityId id in nation.getMemberIds()) {
                if (id == nation.getLeaderId()) {
                    continue;
                }
                Pawn pawn = entityRepository.getEntity(id) as Pawn;
                if (pawn != null && !pawn.isMarkedForDeletion() && pawn.getEnergy() > 0) {
                    pawns.Add(pawn);
                }
            }
            return pawns;
        }

        public bool execute(Player player, Pawn heir) {
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null) {
                player.getStatus().update("You belong to no nation.");
                return false;
            }
            if (own.getLeaderId() != player.getId()) {
                player.getStatus().update("Only the leader of " + own.getName() + " can hand it on.");
                return false;
            }
            if (heir == null || !candidates(own, entityRepository).Contains(heir)) {
                player.getStatus().update("The rule can only go to a living member of " + own.getName() + ".");
                return false;
            }
            own.setLeaderId(heir.getId());
            own.setRole(heir.getId(), NationRole.LEADER);
            own.setRole(player.getId(), keepsStall(own, player) ? NationRole.MERCHANT : NationRole.SERF);
            eventProducer?.produceLeadershipChangeEvent(own, player.getId(), heir.getId());
            player.getStatus().update(heir.getName() + " now leads " + own.getName() + ". You remain one of its people.");
            return true;
        }

        private bool keepsStall(Nation nation, Player player) {
            foreach (EntityId settlementId in nation.getSettlements()) {
                Settlement settlement = entityRepository.getEntity(settlementId) as Settlement;
                if (settlement != null && settlement.getMarket().getStall(player.getId()) != null) {
                    return true;
                }
            }
            return false;
        }
    }

    /**
    * Gives one of the nation's settlements to a member, or takes it back
    * for the leader (#176, SettlementOwnership). The leader gives and takes
    * back; a member who holds one may hand it back.
    */
    public class GrantSettlementCommand {
        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;

        public GrantSettlementCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        /** Gives the settlement to the member, who may be the leader themselves, to keep it through a change of leader. */
        public bool grant(Player player, Settlement settlement, EntityId memberId) {
            Nation own = ownNationLed(player, settlement);
            if (own == null) {
                return false;
            }
            Entity member = memberId == null ? null : entityRepository.getEntity(memberId);
            if (member == null || member.isMarkedForDeletion() || !own.isMember(memberId)) {
                player.getStatus().update("A settlement can only go to a member of " + own.getName() + ".");
                return false;
            }
            if (settlement.getOwnerId() == memberId) {
                player.getStatus().update(describe(member, player) + " already " + (member == player ? "hold" : "holds") + " the " + settlement.getDisplayName() + ".");
                return false;
            }
            settlement.setOwner(memberId, member.getName());
            player.getStatus().update(member == player
                ? "The " + settlement.getDisplayName() + " is yours, and stays yours if you hand on the rule."
                : "The " + settlement.getDisplayName() + " is now held by " + member.getName() + ".");
            return true;
        }

        /** The leader takes the settlement back, or its holder hands it back to the leader. */
        public bool takeBack(Player player, Settlement settlement) {
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null || settlement.getNationId() != own.getId()) {
                player.getStatus().update("That settlement is not your nation's.");
                return false;
            }
            bool leads = own.getLeaderId() == player.getId();
            if (!leads && settlement.getOwnerId() != player.getId()) {
                player.getStatus().update("Only the leader of " + own.getName() + " can take a settlement back.");
                return false;
            }
            if (settlement.getOwnerId() == null) {
                player.getStatus().update("The " + settlement.getDisplayName() + " is already held by the leader.");
                return false;
            }
            settlement.setOwner(null, null);
            player.getStatus().update(leads
                ? "You took back the " + settlement.getDisplayName() + "."
                : "You handed the " + settlement.getDisplayName() + " back to the leader of " + own.getName() + ".");
            return true;
        }

        private Nation ownNationLed(Player player, Settlement settlement) {
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null || settlement.getNationId() != own.getId()) {
                player.getStatus().update("That settlement is not your nation's.");
                return null;
            }
            if (own.getLeaderId() != player.getId()) {
                player.getStatus().update("Only the leader of " + own.getName() + " can give a settlement away.");
                return null;
            }
            return own;
        }

        private static string describe(Entity member, Player player) {
            return member == player ? "You" : member.getName();
        }
    }
}
