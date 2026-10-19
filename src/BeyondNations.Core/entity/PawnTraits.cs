using System.Collections.Generic;

namespace beyondnations {

    /**
    * Something about a pawn's character that changes what it does. Every
    * trait has a rule behind it (see PawnTraits); none is flavour only.
    */
    public enum PawnTrait {
        INDUSTRIOUS,
        GREEDY,
        GREGARIOUS,
        GLUTTON,
        AMBITIOUS,
        LOYAL,
    }

    /**
    * Rolling, inheriting, naming and applying pawn traits.
    *
    * Each pawn has one trait, and sometimes a second. A world-generated pawn
    * rolls them from a RandomSource of their own (seeded from the world seed),
    * so the same seed always gives the same characters and adding traits never
    * shifted anything else a seeded world does. A pawn born in a settlement
    * takes one trait from each parent (PopulationGrowth).
    *
    * The effects, all in one place so the numbers can be tuned together:
    *   Industrious  half as much again wood or stone from every tree or rock,
    *                and less inclined to idle in town
    *   Greedy       sells a quarter dearer at the market
    *   Gregarious   warms half as fast again to the player, twice as fast to
    *                neighbours, and lingers in town
    *   Glutton      burns energy half as fast again
    *   Ambitious    will not join an existing nation unless it is practically
    *                inside the settlement, so it goes off to found its own
    *   Loyal        needs far more than Loyal standing to defect
    */
    public static class PawnTraits {
        public static readonly PawnTrait[] All = (PawnTrait[]) System.Enum.GetValues(typeof(PawnTrait));

        /** Percent chance a world-generated pawn has a second trait. */
        public const int SECOND_TRAIT_CHANCE_PERCENT = 40;

        /** Percent chance a newborn's trait from one parent is replaced by a fresh one. */
        public const int MUTATION_CHANCE_PERCENT = 20;

        public const int INDUSTRIOUS_YIELD_BONUS_PERCENT = 50;
        public const int GREEDY_PRICE_BONUS_PERCENT = 25;
        public const int GREGARIOUS_RELATIONSHIP_BONUS_PERCENT = 50;
        public const float GLUTTON_METABOLISM_MULTIPLIER = 1.5f;
        /** How close an ambitious pawn has to be to a settlement before it joins that nation rather than founding one. */
        public const float AMBITIOUS_JOIN_RANGE = 60f;
        /** How far beyond the Loyal tier a loyal pawn's regard for the player must go before it defects. */
        public const int LOYAL_EXTRA_RELATIONSHIP_TO_DEFECT = 20;

        public static List<PawnTrait> roll(RandomSource random) {
            List<PawnTrait> traits = new List<PawnTrait>();
            PawnTrait first = All[random.range(0, All.Length)];
            traits.Add(first);
            if (random.range(0, 100) < SECOND_TRAIT_CHANCE_PERCENT) {
                // one of the other traits, never the same one twice
                int offset = random.range(1, All.Length);
                traits.Add(All[((int) first + offset) % All.Length]);
            }
            return traits;
        }

        /**
        * A newborn's traits: the first trait of each parent (one trait if they
        * share it), each now and then replaced by a fresh roll.
        */
        public static List<PawnTrait> inherit(Pawn mother, Pawn father, RandomSource random) {
            List<PawnTrait> traits = new List<PawnTrait>();
            addInherited(traits, mother, random);
            addInherited(traits, father, random);
            if (traits.Count == 0) {
                traits.Add(All[random.range(0, All.Length)]);
            }
            return traits;
        }

        private static void addInherited(List<PawnTrait> traits, Pawn parent, RandomSource random) {
            IReadOnlyList<PawnTrait> parentTraits = parent.getTraits();
            bool mutate = random.range(0, 100) < MUTATION_CHANCE_PERCENT;
            PawnTrait? trait = null;
            if (mutate || parentTraits.Count == 0) {
                trait = All[random.range(0, All.Length)];
            }
            else {
                trait = parentTraits[0];
            }
            if (!traits.Contains(trait.Value)) {
                traits.Add(trait.Value);
            }
        }

        public static string getName(PawnTrait trait) {
            switch (trait) {
                case PawnTrait.INDUSTRIOUS: return "Industrious";
                case PawnTrait.GREEDY: return "Greedy";
                case PawnTrait.GREGARIOUS: return "Gregarious";
                case PawnTrait.GLUTTON: return "Glutton";
                case PawnTrait.AMBITIOUS: return "Ambitious";
                case PawnTrait.LOYAL: return "Loyal";
                default: return trait.ToString();
            }
        }

        /** "Industrious, Greedy", or "" for a pawn with no traits. */
        public static string describe(Pawn pawn) {
            IReadOnlyList<PawnTrait> traits = pawn.getTraits();
            List<string> names = new List<string>();
            foreach (PawnTrait trait in traits) {
                names.Add(getName(trait));
            }
            return string.Join(", ", names);
        }

        /** What a pawn says about itself, by its first trait, or null for a pawn with none. */
        public static string aside(Pawn pawn) {
            IReadOnlyList<PawnTrait> traits = pawn.getTraits();
            if (traits.Count == 0) {
                return null;
            }
            switch (traits[0]) {
                case PawnTrait.INDUSTRIOUS: return "Idle hands make me itch.";
                case PawnTrait.GREEDY: return "Mind you, nothing's free.";
                case PawnTrait.GREGARIOUS: return "It's always nice to have someone to talk to!";
                case PawnTrait.GLUTTON: return "Is it supper time yet?";
                case PawnTrait.AMBITIOUS: return "One day they'll tell stories about me.";
                case PawnTrait.LOYAL: return "I stand by my own.";
                default: return null;
            }
        }

        // --- effects --------------------------------------------------------

        /** Extra wood or stone an industrious pawn takes from a tree or rock that held this many. */
        public static int gatherBonus(Pawn pawn, int amount) {
            if (amount <= 0 || !pawn.hasTrait(PawnTrait.INDUSTRIOUS)) {
                return 0;
            }
            return (amount * INDUSTRIOUS_YIELD_BONUS_PERCENT + 99) / 100;
        }

        /** The percent a pawn adds to market prices when it sells. */
        public static int sellPriceBonusPercent(Pawn pawn) {
            return pawn.hasTrait(PawnTrait.GREEDY) ? GREEDY_PRICE_BONUS_PERCENT : 0;
        }

        /** A relationship gain toward the player, after the pawn's temperament. */
        public static int adjustPlayerRelationshipGain(Pawn pawn, int amount) {
            if (amount <= 0 || !pawn.hasTrait(PawnTrait.GREGARIOUS)) {
                return amount;
            }
            return amount + (amount * GREGARIOUS_RELATIONSHIP_BONUS_PERCENT + 99) / 100;
        }

        /** How much a pawn warms to a neighbour each time they share a settlement. */
        public static int neighbourGain(Pawn pawn) {
            return pawn.hasTrait(PawnTrait.GREGARIOUS) ? 2 : 1;
        }

        public static float metabolismMultiplier(Pawn pawn) {
            return pawn.hasTrait(PawnTrait.GLUTTON) ? GLUTTON_METABOLISM_MULTIPLIER : 1f;
        }

        /** How close a nationless pawn must be to a settlement to join its nation. */
        public static float joinRange(Pawn pawn, float configuredRange) {
            if (pawn.hasTrait(PawnTrait.AMBITIOUS)) {
                return System.Math.Min(configuredRange, AMBITIOUS_JOIN_RANGE);
            }
            return configuredRange;
        }

        /** The relationship with the player a pawn in another nation needs before it defects. */
        public static int relationshipToDefect(Pawn pawn) {
            return RelationshipTiers.LOYAL_THRESHOLD + (pawn.hasTrait(PawnTrait.LOYAL) ? LOYAL_EXTRA_RELATIONSHIP_TO_DEFECT : 0);
        }

        /**
        * Percent chance per decision that an idle pawn walks out of a
        * settlement. A pawn lingers at home (#304 made towns visible, and the
        * old flat 10% emptied a staged town in seconds); an industrious pawn
        * gets back to work sooner and a gregarious one stays for the company.
        */
        public static int idleExitChancePercent(Pawn pawn, bool atHome) {
            if (!atHome) {
                return 10;
            }
            if (pawn.hasTrait(PawnTrait.INDUSTRIOUS)) {
                return 4;
            }
            if (pawn.hasTrait(PawnTrait.GREGARIOUS)) {
                return 1;
            }
            return 2;
        }
    }
}
