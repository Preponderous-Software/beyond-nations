using System.Collections.Generic;

namespace beyondnations {

    /**
    * Bandits camp outside a settlement and rob it every minute until they
    * are dealt with: driven off by a militia (which tires everyone who
    * turns out), paid to leave, or waited out for a day and a half. Towns
    * have a palisade, so bandits go after camps and villages.
    *
    * Barring the gates does not shut the other two doors: while the camp
    * stands, the nation's leader can take the choice up again
    * (WorldEvents.offerStandingBanditCamp) and still drive the bandits off,
    * pay them, or keep waiting. Such an instance is marked as a standing
    * camp (isStandingCamp) and acts on the camp already there rather than
    * making a new one.
    */
    public class BanditCampEvent : IWorldEvent {
        public const string Id = "bandits";
        public const int STAY_SECONDS = 900;
        public const int BRIBE_COINS = 30;
        public const int MILITIA_MEMBERS = 3;
        public const float MILITIA_ENERGY = 25f;
        /** The instance magnitude that marks a camp already standing (isStandingCamp). */
        public const int StandingCamp = 1;
        public const string AlreadyGoneReason = "They have already gone";

        public string getId() { return Id; }
        public string getTitle() { return "Bandit camp"; }
        public bool isPersonal() { return false; }

        public int getWeight(WorldEventContext context, Nation nation) {
            Settlement target = richestTarget(context, nation);
            if (target == null) {
                return 0;
            }
            int wealth = target.getFunds() + target.getInventory().getNumItems(ItemType.WOOD) + target.getInventory().getNumItems(ItemType.STONE);
            return 3 + System.Math.Min(9, wealth / 10);
        }

        private static Settlement richestTarget(WorldEventContext context, Nation nation) {
            Settlement richest = null;
            int richestWealth = 9;
            foreach (Settlement settlement in context.settlementsOf(nation)) {
                if (settlement.getTier() == SettlementTier.TOWN || context.getWorldEvents().hasBandits(settlement.getId())) {
                    continue;
                }
                int wealth = settlement.getFunds() + settlement.getInventory().getNumItems(ItemType.WOOD) + settlement.getInventory().getNumItems(ItemType.STONE);
                if (wealth > richestWealth) {
                    richest = settlement;
                    richestWealth = wealth;
                }
            }
            return richest;
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            Settlement target = richestTarget(context, nation);
            if (target == null) {
                return false;
            }
            instance.setSettlementId(target.getId());
            return true;
        }

        /**
        * Whether the instance is about a camp already standing outside its
        * settlement (the gates were barred and the bandits are still there),
        * rather than bandits newly arrived. Kept in the instance's magnitude,
        * so a pending choice about a standing camp is saved and loaded as one.
        */
        public static bool isStandingCamp(WorldEventInstance instance) {
            return instance.getMagnitude() == StandingCamp;
        }

        /** The camp outside the instance's settlement, or null when there is none. */
        private static WorldEffect campOf(WorldEventContext context, WorldEventInstance instance) {
            return instance.getSettlementId() == null ? null
                : context.getWorldEvents().findEffect(WorldEffectKind.BANDITS, null, instance.getSettlementId());
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            string where = settlement == null ? "your settlement" : "the " + settlement.getDisplayName();
            string theft = WorldEvents.BANDIT_THEFT_COINS + " coins, " + WorldEvents.BANDIT_THEFT_GOODS + " wood and "
                + WorldEvents.BANDIT_THEFT_GOODS + " stone";
            if (isStandingCamp(instance)) {
                WorldEffect camp = campOf(context, instance);
                if (camp == null) {
                    return "The bandits outside " + where + " have already broken camp and moved on.";
                }
                int minute = context.ticks(60);
                int minutes = System.Math.Max(1, (camp.getEndTick() - context.getTick() + minute - 1) / minute);
                return "Bandits are still camped in the woods outside " + where + ", robbing it of " + theft
                    + " every minute. Left alone, they move on in about " + minutes + (minutes == 1 ? " minute." : " minutes.");
            }
            return "Bandits have made camp in the woods outside " + where + ". Until they are dealt with they will rob it of "
                + theft + " every minute.";
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            int members = context.getNation(instance.getNationId()) == null ? 0 : context.getNation(instance.getNationId()).getNumberOfMembers();
            bool standing = isStandingCamp(instance);
            WorldEventChoice militia = WorldEventChoice.requiring(members >= MILITIA_MEMBERS, "Raise a militia and drive them off",
                "The camp is gone at once, but every member who turns out comes back worn out.", "Needs " + MILITIA_MEMBERS + " members");
            WorldEventChoice bribe = WorldEventChoice.requiring(context.availableCoins(instance) >= BRIBE_COINS, "Pay them to leave (" + BRIBE_COINS + " coins)",
                "They take the money and go.", "Needs " + BRIBE_COINS + " coins");
            if (standing && campOf(context, instance) == null) {
                // they moved on while the choice was open: nobody is left to drive off or pay
                militia = new WorldEventChoice(militia.getLabel(), militia.getConsequence(), AlreadyGoneReason);
                bribe = new WorldEventChoice(bribe.getLabel(), bribe.getConsequence(), AlreadyGoneReason);
            }
            WorldEventChoice wait = standing
                ? new WorldEventChoice("Keep the gates barred", "Free now, but they go on robbing the settlement every minute until they move on.")
                : new WorldEventChoice("Bar the gates and wait",
                    "Free now, but they rob the settlement every minute for a day and a half, unless they are driven off or paid later.");
            return new List<WorldEventChoice> { militia, bribe, wait };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            List<WorldEventChoice> choices = getChoices(context, instance);
            Nation nation = context.getNation(instance.getNationId());
            if (choices[0].isAvailable() && nation.getNumberOfMembers() >= MILITIA_MEMBERS + 1) {
                return 0;
            }
            return choices[1].isAvailable() ? 1 : 2;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            Nation nation = context.getNation(instance.getNationId());
            if (settlement == null || nation == null) {
                return null;
            }
            string where = "the " + settlement.getDisplayName();
            string who = context.chooser(instance);
            bool standing = isStandingCamp(instance);
            WorldEffect camp = campOf(context, instance);
            if (standing && camp == null) {
                // they moved on while the choice was open (and said so): nothing is left to do
                return null;
            }
            if (choice == 0) {
                foreach (Pawn member in context.membersOf(nation)) {
                    member.setEnergy(System.Math.Max(1f, member.getEnergy() - MILITIA_ENERGY));
                }
                if (instance.isForPlayer()) {
                    Player player = context.getPlayer();
                    player.setEnergy(System.Math.Max(1f, player.getEnergy() - MILITIA_ENERGY));
                }
                if (camp != null) {
                    context.getWorldEvents().removeEffect(camp);
                }
                return standing
                    ? who + " raised a militia of " + nation.getName() + " and drove off the bandits camped outside " + where + "."
                    : "bandits camped outside " + where + "; " + who + " raised a militia of " + nation.getName() + " and drove them off.";
            }
            if (choice == 1) {
                context.payCoins(instance, BRIBE_COINS);
                if (camp != null) {
                    context.getWorldEvents().removeEffect(camp);
                }
                return standing
                    ? who + " paid the bandits camped outside " + where + " " + BRIBE_COINS + " coins to go away."
                    : "bandits camped outside " + where + "; " + who + " paid them " + BRIBE_COINS + " coins to go away.";
            }
            if (standing) {
                // the camp runs on, on its own clock
                return who + " kept the gates of " + where + " barred against the bandits outside.";
            }
            if (camp == null) {
                context.getWorldEvents().addEffect(new WorldEffect(WorldEffectKind.BANDITS, nation.getId(), settlement.getId(), context.getTick(),
                    context.getTick() + context.ticks(STAY_SECONDS), context.getTick() + context.ticks(WorldEvents.BANDIT_THEFT_SECONDS)));
            }
            return "bandits made camp outside " + where + ", and " + who + " barred the gates to wait them out.";
        }
    }
}
