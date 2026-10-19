using System;
using System.Text;

namespace beyondnations {

    /**
    * The player actions an objective hint can name. Key bindings belong to the
    * host's input layer, not to the simulation, so a hint carries a token such
    * as "{Interact}" and the host turns it into whatever key that action is
    * bound to. Each name here matches a field on the desktop KeyBindings table.
    */
    public enum HintKey {
        Interact,
        CreateNewNation,
        JoinNation,
        LeaveNation,
        FoundSettlement,
        BuildStall,
        TeleportToHomeSettlement,
        PlantSapling,
        ToggleInventory,
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
