using System;
using System.Text;

namespace beyondnations {

    /**
    * The player actions an objective hint can name. Key bindings belong to the
    * host's input layer, not to the simulation, so a hint carries a token such
    * as "{Interact}" and the host turns it into whatever key that action is
    * bound to, or the on-screen button that does it on a touch screen. Each
    * name here matches a field on the desktop KeyBindings table. Any
    * player-facing text, not only an objective hint, can carry these tokens
    * (a status line from the simulation, a panel's help line), so no text
    * names a keyboard key to a player on a phone.
    */
    public enum HintKey {
        Interact,
        GiftToPawn,
        InviteToNation,
        CreateNewNation,
        JoinNation,
        LeaveNation,
        FoundSettlement,
        BuildStall,
        BuildBuilding,
        TeleportToHomeSettlement,
        PlantSapling,
        ToggleInventory,
        ToggleCrafting,
        ToggleNations,
        ToggleChronicle,
        ShowControls,
        Pause,
    }

    public static class ObjectiveHint {

        /**
        * Replaces every "{HintKey}" token in a hint with the name the host
        * gives that key. Unknown tokens are left as they are.
        */
        public static string resolve(string hint, Func<HintKey, string> keyName) {
            if (string.IsNullOrEmpty(hint) || hint.IndexOf('{') < 0) {
                return hint;
            }
            StringBuilder result = new StringBuilder(hint);
            foreach (HintKey key in (HintKey[]) Enum.GetValues(typeof(HintKey))) {
                result.Replace(token(key), keyName(key));
            }
            return result.ToString();
        }

        /**
        * The token for a key, for building hints.
        */
        public static string token(HintKey key) {
            return "{" + key + "}";
        }
    }
}
