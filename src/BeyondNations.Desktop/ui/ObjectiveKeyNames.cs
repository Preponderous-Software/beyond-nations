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
            if (TouchLayout.isActive()) {
                return ofTouch(key);
            }
            switch (key) {
                case HintKey.Interact: return KeyBindings.Interact.ToString();
                case HintKey.GiftToPawn: return KeyBindings.GiftToPawn.ToString();
                case HintKey.InviteToNation: return KeyBindings.InviteToNation.ToString();
                case HintKey.CreateNewNation: return KeyBindings.CreateNewNation.ToString();
                case HintKey.JoinNation: return KeyBindings.JoinNation.ToString();
                case HintKey.LeaveNation: return KeyBindings.LeaveNation.ToString();
                case HintKey.FoundSettlement: return KeyBindings.FoundSettlement.ToString();
                case HintKey.BuildStall: return KeyBindings.BuildStall.ToString();
                case HintKey.BuildBuilding: return KeyBindings.BuildBuilding.ToString();
                case HintKey.TeleportToHomeSettlement: return KeyBindings.TeleportToHomeSettlement.ToString();
                case HintKey.PlantSapling: return KeyBindings.PlantSapling.ToString();
                case HintKey.ToggleInventory: return KeyBindings.ToggleInventory.ToString();
                default: return key.ToString();
            }
        }

        /**
        * The on-screen button that does the same on a touch screen
        * (TouchLayout): the Use and Bag buttons, or the command bar's button
        * of that name, which the bar offers whenever the action is possible.
        */
        public static string ofTouch(HintKey key) {
            switch (key) {
                case HintKey.Interact: return "Use";
                case HintKey.GiftToPawn: return "Gift";
                case HintKey.InviteToNation: return "Invite";
                case HintKey.CreateNewNation: return "Create Nation";
                case HintKey.JoinNation: return "Join Nation";
                case HintKey.LeaveNation: return "Leave Nation";
                case HintKey.FoundSettlement: return "Found Settlement";
                case HintKey.BuildStall: return "Build Stall";
                // the bar offers one "Build House", "Build Farm", ... per building it can afford
                case HintKey.BuildBuilding: return "a Build button";
                case HintKey.TeleportToHomeSettlement: return "Teleport Home";
                case HintKey.PlantSapling: return "Plant Sapling";
                case HintKey.ToggleInventory: return "Bag";
                default: return key.ToString();
            }
        }

        /** A hint with every key token replaced by its bound key. */
        public static string resolve(string hint) {
            return ObjectiveHint.resolve(hint, of);
        }
    }
}
