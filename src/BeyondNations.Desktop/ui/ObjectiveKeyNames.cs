using beyondnations.desktop.input;

namespace beyondnations.desktop.ui {

    /**
    * Turns the HintKey tokens in an objective hint into the keys they are
    * bound to. The simulation names actions; only the host knows the binding
    * table, the same split Game.createWorld keeps for the opening hint. A
    * switch rather than reflection, so the trimmed browser build keeps it.
    */
    public static class ObjectiveKeyNames {

        public static string of(HintKey key) {
            switch (key) {
                case HintKey.Interact: return KeyBindings.Interact.ToString();
                case HintKey.CreateNewNation: return KeyBindings.CreateNewNation.ToString();
                case HintKey.JoinNation: return KeyBindings.JoinNation.ToString();
                case HintKey.LeaveNation: return KeyBindings.LeaveNation.ToString();
                case HintKey.FoundSettlement: return KeyBindings.FoundSettlement.ToString();
                case HintKey.BuildStall: return KeyBindings.BuildStall.ToString();
                case HintKey.TeleportToHomeSettlement: return KeyBindings.TeleportToHomeSettlement.ToString();
                case HintKey.PlantSapling: return KeyBindings.PlantSapling.ToString();
                case HintKey.ToggleInventory: return KeyBindings.ToggleInventory.ToString();
                default: return key.ToString();
            }
        }

        /** A hint with every key token replaced by its bound key. */
        public static string resolve(string hint) {
            return ObjectiveHint.resolve(hint, of);
        }
    }
}
