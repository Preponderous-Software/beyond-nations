using beyondnations.desktop.input;

namespace beyondnations.desktop.ui {

    /**
    * The one place a player-facing line learns what to call a control: the
    * key it is bound to, or, on a touch screen (TouchLayout), the on-screen
    * button that does the same. Text carries "{HintKey}" tokens (objective
    * hints, status lines, panel help) and resolve() fills them in, so no
    * screen keeps a key name of its own and a phone never reads "press O".
    *
    * The simulation names actions; only the host knows the binding table,
    * the same split Game.createWorld keeps for the opening hint. A switch
    * rather than reflection, so the trimmed browser build keeps it.
    */
    public static class ObjectiveKeyNames {

        /** What a panel's footer says on a touch screen, where it has a Close button rather than a key. */
        public const string TouchCloseHint = "(Tap Close, top right, to go back)";

        public static string of(HintKey key) {
            if (TouchLayout.isActive()) {
                return ofTouch(key);
            }
            return ofKeyboard(key);
        }

        /** The key an action is bound to, as a player would name it ("Esc", "?", "E"). */
        public static string ofKeyboard(HintKey key) {
            switch (key) {
                case HintKey.Interact: return KeyNames.describe(KeyBindings.Interact);
                case HintKey.GiftToPawn: return KeyNames.describe(KeyBindings.GiftToPawn);
                case HintKey.InviteToNation: return KeyNames.describe(KeyBindings.InviteToNation);
                case HintKey.CreateNewNation: return KeyNames.describe(KeyBindings.CreateNewNation);
                case HintKey.JoinNation: return KeyNames.describe(KeyBindings.JoinNation);
                case HintKey.LeaveNation: return KeyNames.describe(KeyBindings.LeaveNation);
                case HintKey.FoundSettlement: return KeyNames.describe(KeyBindings.FoundSettlement);
                case HintKey.BuildStall: return KeyNames.describe(KeyBindings.BuildStall);
                case HintKey.BuildBuilding: return KeyNames.describe(KeyBindings.BuildBuilding);
                case HintKey.TeleportToHomeSettlement: return KeyNames.describe(KeyBindings.TeleportToHomeSettlement);
                case HintKey.PlantSapling: return KeyNames.describe(KeyBindings.PlantSapling);
                case HintKey.ToggleInventory: return KeyNames.describe(KeyBindings.ToggleInventory);
                case HintKey.ToggleCrafting: return KeyNames.describe(KeyBindings.ToggleCrafting);
                case HintKey.ToggleNations: return KeyNames.describe(KeyBindings.ToggleNations);
                case HintKey.ToggleChronicle: return KeyNames.describe(KeyBindings.ToggleChronicle);
                case HintKey.ShowControls: return KeyNames.describe(KeyBindings.ShowControls);
                case HintKey.Pause: return KeyNames.describe(KeyBindings.Pause);
                default: return key.ToString();
            }
        }

        /**
        * The on-screen button that does the same on a touch screen
        * (TouchLayout, docs/WEB.md): the Use button, one of the button row's
        * Bag, Craft, Realm, News, ? and Menu, or the command bar's button of
        * that name, which the bar offers whenever the action is possible.
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
                case HintKey.ToggleCrafting: return "Craft";
                case HintKey.ToggleNations: return "Realm";
                case HintKey.ToggleChronicle: return "News";
                case HintKey.ShowControls: return "?";
                case HintKey.Pause: return "Menu";
                default: return key.ToString();
            }
        }

        /** A line with every key token replaced by its bound key, or its button on a touch screen. */
        public static string resolve(string hint) {
            return ObjectiveHint.resolve(hint, of);
        }

        /**
        * A panel's footer: the keyboard hint (itself free to carry tokens),
        * or, on a touch screen, where the panel's Close button is, since a
        * finger has no Escape.
        */
        public static string closeHint(string keyboardHint) {
            return closeHint(keyboardHint, TouchCloseHint);
        }

        /** closeHint, for a panel whose Close button is somewhere else. */
        public static string closeHint(string keyboardHint, string touchHint) {
            return TouchLayout.isActive() ? touchHint : resolve(keyboardHint);
        }
    }
}
