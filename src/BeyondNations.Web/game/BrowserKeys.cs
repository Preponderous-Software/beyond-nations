using Silk.NET.Input;

namespace beyondnations.web {

    /**
    * KeyboardEvent.code -> the Silk.NET Key the shared input layer and
    * KeyBindings are written against.
    *
    * `code` names the physical key, not the character it types, which is
    * what a binding table wants: W is "KeyW" on an AZERTY keyboard too, just
    * as GLFW's key codes are positional on the desktop.
    */
    public static class BrowserKeys {

        public static Key fromCode(string code) {
            if (string.IsNullOrEmpty(code)) {
                return Key.Unknown;
            }

            // "KeyA".."KeyZ"
            if (code.Length == 4 && code.StartsWith("Key") && code[3] >= 'A' && code[3] <= 'Z') {
                return (Key) ((int) Key.A + (code[3] - 'A'));
            }
            // "Digit0".."Digit9"
            if (code.Length == 6 && code.StartsWith("Digit") && code[5] >= '0' && code[5] <= '9') {
                return (Key) ((int) Key.Number0 + (code[5] - '0'));
            }
            // "Numpad0".."Numpad9"
            if (code.Length == 7 && code.StartsWith("Numpad") && code[6] >= '0' && code[6] <= '9') {
                return (Key) ((int) Key.Keypad0 + (code[6] - '0'));
            }
            // "F1".."F24"
            if (code.Length >= 2 && code.Length <= 3 && code[0] == 'F' && int.TryParse(code.Substring(1), out int function)
                    && function >= 1 && function <= 24) {
                return (Key) ((int) Key.F1 + (function - 1));
            }

            switch (code) {
                case "Space": return Key.Space;
                case "Quote": return Key.Apostrophe;
                case "Comma": return Key.Comma;
                case "Minus": return Key.Minus;
                case "Period": return Key.Period;
                case "Slash": return Key.Slash;
                case "Semicolon": return Key.Semicolon;
                case "Equal": return Key.Equal;
                case "BracketLeft": return Key.LeftBracket;
                case "Backslash": return Key.BackSlash;
                case "BracketRight": return Key.RightBracket;
                case "Backquote": return Key.GraveAccent;
                case "Escape": return Key.Escape;
                case "Enter": return Key.Enter;
                case "Tab": return Key.Tab;
                case "Backspace": return Key.Backspace;
                case "Insert": return Key.Insert;
                case "Delete": return Key.Delete;
                case "ArrowRight": return Key.Right;
                case "ArrowLeft": return Key.Left;
                case "ArrowDown": return Key.Down;
                case "ArrowUp": return Key.Up;
                case "PageUp": return Key.PageUp;
                case "PageDown": return Key.PageDown;
                case "Home": return Key.Home;
                case "End": return Key.End;
                case "CapsLock": return Key.CapsLock;
                case "ScrollLock": return Key.ScrollLock;
                case "NumLock": return Key.NumLock;
                case "PrintScreen": return Key.PrintScreen;
                case "Pause": return Key.Pause;
                case "NumpadDecimal": return Key.KeypadDecimal;
                case "NumpadDivide": return Key.KeypadDivide;
                case "NumpadMultiply": return Key.KeypadMultiply;
                case "NumpadSubtract": return Key.KeypadSubtract;
                case "NumpadAdd": return Key.KeypadAdd;
                case "NumpadEnter": return Key.KeypadEnter;
                case "NumpadEqual": return Key.KeypadEqual;
                case "ShiftLeft": return Key.ShiftLeft;
                case "ControlLeft": return Key.ControlLeft;
                case "AltLeft": return Key.AltLeft;
                case "MetaLeft": return Key.SuperLeft;
                case "ShiftRight": return Key.ShiftRight;
                case "ControlRight": return Key.ControlRight;
                case "AltRight": return Key.AltRight;
                case "MetaRight": return Key.SuperRight;
                case "ContextMenu": return Key.Menu;
                default: return Key.Unknown;
            }
        }
    }
}
