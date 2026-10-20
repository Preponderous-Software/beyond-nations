namespace beyondnations {

    /**
    * Sets one of the nation's laws (realm/Laws). Only the leader can; the
    * change is proclaimed and comes in later (Realm.LAW_DELAY_TICKS).
    */
    public class SetLawCommand {
        private readonly NationRepository nationRepository;
        private readonly Realm realm;

        public SetLawCommand(NationRepository nationRepository, Realm realm) {
            this.nationRepository = nationRepository;
            this.realm = realm;
        }

        public void execute(Player player, LawType law, int level) {
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null) {
                player.getStatus().update("You need a nation before you can make its laws.");
                return;
            }
            if (!own.getLeaderId().Equals(player.getId())) {
                player.getStatus().update("Only the leader of " + own.getName() + " can make its laws.");
                return;
            }
            player.getStatus().update(realm.proclaim(own, law, level));
        }
    }

    /**
    * Declares war on another nation. The answer says how the enemy takes it,
    * by the strength each side can field.
    */
    public class DeclareWarCommand {
        private readonly NationRepository nationRepository;
        private readonly Realm realm;

        public DeclareWarCommand(NationRepository nationRepository, Realm realm) {
            this.nationRepository = nationRepository;
            this.realm = realm;
        }

        public void execute(Player player, Nation target) {
            Nation own = NationDiplomacy.getLedNation(player, nationRepository, target);
            if (own == null) {
                return;
            }
            if (realm.isAtWar(own, target)) {
                player.getStatus().update("You are already at war with " + target.getName() + ".");
                return;
            }
            // weighed after the war breaks any alliance between the two
            realm.declareWar(own, target);
            Diplomacy diplomacy = realm.getDiplomacy();
            int ours = diplomacy.getStrength(own);
            int theirs = diplomacy.getDefence(target);
            string odds = theirs + " to your " + ours;
            string answer;
            if (theirs >= 2 * ours) {
                answer = target.getName() + " laughs at your declaration of war: they are " + odds + ".";
            }
            else if (ours >= 2 * theirs) {
                answer = target.getName() + " falls back behind its walls: war, and they are only " + odds + ".";
            }
            else {
                answer = target.getName() + " takes up arms: war, " + odds + ".";
            }
            if (!diplomacy.areNeighbours(own, target)) {
                answer += " They are far off; war bites only where your lands meet.";
            }
            player.getStatus().update(answer);
        }
    }

    /**
    * Offers peace to a nation the player is at war or at odds with. It is
    * refused when too much blood lies between them, or when the enemy is
    * twice as strong and thinks it is winning -- unless a tribute has bought
    * a truce. Accepted, the war ends, relations come up to Neutral and a
    * truce holds for a minute.
    */
    public class OfferPeaceCommand {
        /** Below this, no peace is accepted whatever the odds. */
        public const int TOO_MUCH_BLOOD = -90;

        private readonly NationRepository nationRepository;
        private readonly Realm realm;

        public OfferPeaceCommand(NationRepository nationRepository, Realm realm) {
            this.nationRepository = nationRepository;
            this.realm = realm;
        }

        /** Whether peace is worth offering at all: at war, or Rival or worse. */
        public static bool isApplicable(NationRepository nationRepository, Realm realm, Nation own, Nation target) {
            return own != null && target != null
                && (realm.isAtWar(own, target) || nationRepository.getRelations().getState(own, target) <= NationRelationState.RIVAL);
        }

        public void execute(Player player, Nation target) {
            Nation own = NationDiplomacy.getLedNation(player, nationRepository, target);
            if (own == null) {
                return;
            }
            NationRelations relations = nationRepository.getRelations();
            if (!isApplicable(nationRepository, realm, own, target)) {
                player.getStatus().update("You are not at odds with " + target.getName() + ": relations are " + relations.describe(own, target) + ".");
                return;
            }
            Diplomacy diplomacy = realm.getDiplomacy();
            int ours = diplomacy.getDefence(own);
            int theirs = diplomacy.getStrength(target);
            int score = relations.getScore(own, target);
            bool truce = realm.hasTruce(own, target);
            if (score <= TOO_MUCH_BLOOD) {
                player.getStatus().update(target.getName() + " refuses peace: too much blood lies between you (" + relations.describe(own, target)
                    + "). Tribute may soften them.");
                return;
            }
            if (theirs >= 2 * ours && !truce) {
                player.getStatus().update(target.getName() + " refuses peace: they are " + theirs + " to your " + ours
                    + " and think they are winning. Tribute may buy a truce.");
                return;
            }
            string why = theirs < ours ? "they are the weaker, " + theirs + " to your " + ours
                : (truce ? "your tribute has softened them" : "the fighting has cost them enough");
            realm.makePeace(own, target);
            player.getStatus().update(target.getName() + " accepts peace (" + why + "). A truce holds for "
                + (Realm.TRUCE_TICKS / TickCounter.DefaultTicksPerSecond) + " seconds; relations are " + relations.describe(own, target) + ".");
        }
    }
}
