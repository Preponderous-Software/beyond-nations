using System.Collections.Generic;

namespace beyondnations {

    /**
    * A claimant to another nation's leadership comes looking for backing.
    * Backing them costs coin and puts a friend at the head of that nation;
    * warning its leader earns a little goodwill and gets the claimant
    * thrown out; staying out of it may see the claimant take power anyway,
    * remembering who would not help.
    */
    public class ClaimantEvent : IWorldEvent {
        public const string Id = "claimant";
        public const int BACKING_COINS = 20;
        public const int BACKED_RELATIONS = 15;
        public const int WARNED_RELATIONS = 8;
        public const int SPURNED_RELATIONS = -10;
        public const int MIN_MEMBERS = 3;

        public string getId() { return Id; }
        public string getTitle() { return "A claimant stirs"; }
        public bool isPersonal() { return false; }

        public int getWeight(WorldEventContext context, Nation nation) {
            if (nation == null) {
                return 0;
            }
            int weight = 0;
            NationRelations relations = context.getSimulation().getNationRepository().getRelations();
            foreach (Nation other in eligibleRivals(context, nation)) {
                NationRelationState state = relations.getState(nation, other);
                int w = state == NationRelationState.HOSTILE || state == NationRelationState.RIVAL ? 4 : 2;
                weight = System.Math.Max(weight, w);
            }
            return weight;
        }

        private static List<Nation> eligibleRivals(WorldEventContext context, Nation nation) {
            List<Nation> rivals = new List<Nation>();
            foreach (Nation other in context.getSimulation().getNationRepository().getNations()) {
                if (other == nation || context.isLedByPlayer(other) || other.getNumberOfMembers() < MIN_MEMBERS) {
                    continue;
                }
                if (claimantsIn(context, other).Count > 0) {
                    rivals.Add(other);
                }
            }
            return rivals;
        }

        private static List<Pawn> claimantsIn(WorldEventContext context, Nation nation) {
            List<Pawn> claimants = new List<Pawn>();
            foreach (Pawn member in context.membersOf(nation)) {
                if (!member.getId().Equals(nation.getLeaderId())) {
                    claimants.Add(member);
                }
            }
            return claimants;
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            List<Nation> rivals = eligibleRivals(context, nation);
            if (rivals.Count == 0) {
                return false;
            }
            // the most hostile neighbour is the likeliest to be stirring
            NationRelations relations = context.getSimulation().getNationRepository().getRelations();
            Nation other = rivals[0];
            foreach (Nation candidate in rivals) {
                if (relations.getScore(nation, candidate) < relations.getScore(nation, other)) {
                    other = candidate;
                }
            }
            List<Pawn> claimants = claimantsIn(context, other);
            Pawn claimant = claimants[context.getRandom().range(0, claimants.Count)];
            foreach (Pawn pawn in claimants) {
                if (pawn.hasTrait(PawnTrait.AMBITIOUS)) {
                    claimant = pawn;
                    break;
                }
            }
            instance.setOtherNationId(other.getId());
            instance.setEntityId(claimant.getId());
            // 1: left alone, the claimant seizes power anyway
            instance.setMagnitude(context.getRandom().range(0, 2));
            return true;
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            Nation other = context.getNation(instance.getOtherNationId());
            string o = other == null ? "a neighbouring nation" : other.getName();
            string leader = other == null ? "its leader" : context.nameOf(other.getLeaderId());
            return context.nameOf(instance.getEntityId()) + " of " + o + " claims the right to lead it in place of " + leader
                + ", and has come in secret asking for your backing.";
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            Nation other = context.getNation(instance.getOtherNationId());
            string o = other == null ? "them" : other.getName();
            return new List<WorldEventChoice> {
                WorldEventChoice.requiring(context.availableCoins(instance) >= BACKING_COINS, "Back the claimant (" + BACKING_COINS + " coins)",
                    "The claimant takes power, and " + o + " will be friendlier toward you.", "Needs " + BACKING_COINS + " coins"),
                new WorldEventChoice("Warn " + o + "'s leader", "A little goodwill with " + o + "; the claimant is driven out."),
                new WorldEventChoice("Stay out of it", "Nothing now -- but if the claimant wins anyway, they will remember."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            Nation nation = context.getNation(instance.getNationId());
            Nation other = context.getNation(instance.getOtherNationId());
            NationRelationState state = context.getSimulation().getNationRepository().getRelations().getState(nation, other);
            if ((state == NationRelationState.HOSTILE || state == NationRelationState.RIVAL) && context.availableCoins(instance) >= BACKING_COINS) {
                return 0;
            }
            return state == NationRelationState.FRIENDLY || state == NationRelationState.ALLIED ? 1 : 2;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Nation nation = context.getNation(instance.getNationId());
            Nation other = context.getNation(instance.getOtherNationId());
            Pawn claimant = context.getEntity(instance.getEntityId()) as Pawn;
            string who = context.chooser(instance);
            if (nation == null || other == null || claimant == null || !other.isMember(claimant.getId())
                    || claimant.getId().Equals(other.getLeaderId())) {
                return "the claimant who had sought " + who + " out vanished before anything came of it.";
            }
            string c = claimant.getName();
            Diplomacy diplomacy = context.getSimulation().getDiplomacy();
            if (choice == 0) {
                context.payCoins(instance, BACKING_COINS);
                seizePower(context, other, claimant);
                diplomacy.change(nation, other, BACKED_RELATIONS);
                return "with " + nation.getName() + "'s gold behind them, " + c + " seized the leadership of " + other.getName() + ".";
            }
            if (choice == 1) {
                exile(context, other, claimant);
                diplomacy.change(nation, other, WARNED_RELATIONS);
                return who + " warned " + other.getName() + " of " + c + "'s plotting, and " + c + " was driven out of it.";
            }
            if (instance.getMagnitude() == 1) {
                seizePower(context, other, claimant);
                diplomacy.change(nation, other, SPURNED_RELATIONS);
                return c + " seized the leadership of " + other.getName() + " without " + nation.getName() + "'s help, and has not forgotten it.";
            }
            return c + "'s claim to lead " + other.getName() + " came to nothing, and " + who + " kept well out of it.";
        }

        private static void seizePower(WorldEventContext context, Nation nation, Pawn claimant) {
            EntityId previous = nation.getLeaderId();
            nation.setRole(previous, NationRole.SERF);
            nation.setLeaderId(claimant.getId());
            nation.setRole(claimant.getId(), NationRole.LEADER);
            context.getSimulation().getEventProducer().produceLeadershipChangeEvent(nation, previous, claimant.getId());
        }

        private static void exile(WorldEventContext context, Nation nation, Pawn claimant) {
            foreach (Settlement settlement in context.settlementsOf(nation)) {
                foreach (Stall stall in settlement.getMarket().getStalls()) {
                    if (stall.getOwnerId() != null && stall.getOwnerId().Equals(claimant.getId())) {
                        stall.setOwnerId(null);
                    }
                }
            }
            nation.removeMember(claimant.getId());
            claimant.setNationId(null);
            claimant.setHomeSettlementId(null);
            claimant.setOrder(null);
            context.getSimulation().getEventProducer().produceNationLeaveEvent(nation, claimant.getId());
        }
    }
}
