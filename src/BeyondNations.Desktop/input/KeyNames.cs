using System.Text;
using Silk.NET.Input;

namespace beyondnations.desktop.input {

    /**
    * The name a player would use for a key. Silk.NET's enum names read as
    * code ("ShiftLeft", "PageUp", "Slash"), and printing them as they are
    * makes the player translate.
    */
    public static class KeyNames {

        public static string describe(Key key) {
            switch (key) {
                case Key.ShiftLeft: return "Left Shift";
                case Key.ShiftRight: return "Right Shift";
                case Key.ControlLeft: return "Left Ctrl";
                case Key.ControlRight: return "Right Ctrl";
                case Key.AltLeft: return "Left Alt";
                case Key.AltRight: return "Right Alt";
                case Key.Up: return "Up Arrow";
                case Key.Down: return "Down Arrow";
                case Key.Left: return "Left Arrow";
                case Key.Right: return "Right Arrow";
                case Key.Slash: return "?";
                case Key.Escape: return "Esc";
                default: return splitWords(key.ToString());
            }
        }

        // "PageUp" -> "Page Up". Single letters, digits and F-keys are left alone.
        private static string splitWords(string name) {
            StringBuilder builder = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++) {
                char c = name[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1])) {
                    builder.Append(' ');
                }
                builder.Append(c);
            }
            return builder.ToString();
        }
    }
}
