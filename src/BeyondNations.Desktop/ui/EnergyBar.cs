using System.Numerics;
using beyondnations.desktop.ui.screens;

namespace beyondnations.desktop.ui {

    /**
    * What the energy bar in the player box shows: how full it is, what colour
    * and what it says.
    *
    * Energy used to be a bare number among the relationship count and the
    * nation name, so a falling value read the same as a rising one until the
    * hunger warning appeared at 30. A bar is read at a glance, and its colour
    * turns from green through yellow and amber to red on the same thresholds
    * the hunger warning uses (WorldHud.HungryEnergy and StarvingEnergy), so
    * the bar and the warning always agree.
    */
    public static class EnergyBar {
        /**
        * Energy a new player starts with, which is a full bar. Eating can take
        * the player past it; the bar then stays full.
        */
        public const float FullEnergy = 100f;

        public enum Tier { Fed, Peckish, Hungry, Starving }

        public static float getFraction(float energy) {
            if (float.IsNaN(energy)) {
                return 0f;
            }
            return System.Math.Clamp(energy / FullEnergy, 0f, 1f);
        }

        public static Tier getTier(float energy) {
            if (energy < WorldHud.StarvingEnergy) {
                return Tier.Starving;
            }
            if (energy < WorldHud.HungryEnergy) {
                return Tier.Hungry;
            }
            if (energy < WorldHud.HungryEnergy * 2f) {
                return Tier.Peckish;
            }
            return Tier.Fed;
        }

        public static Vector4 getColour(float energy) {
            switch (getTier(energy)) {
                case Tier.Starving: return new Vector4(0.85f, 0.20f, 0.20f, 1f);
                case Tier.Hungry: return new Vector4(0.90f, 0.55f, 0.15f, 1f);
                case Tier.Peckish: return new Vector4(0.80f, 0.75f, 0.20f, 1f);
                default: return new Vector4(0.25f, 0.70f, 0.30f, 1f);
            }
        }

        /** The text drawn over the bar, such as "Energy 73" or "Hungry 24". */
        public static string getLabel(float energy) {
            int whole = (int) energy;
            switch (getTier(energy)) {
                case Tier.Starving: return "Starving " + whole;
                case Tier.Hungry: return "Hungry " + whole;
                default: return "Energy " + whole;
            }
        }
    }
}
