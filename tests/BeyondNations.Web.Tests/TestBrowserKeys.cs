using beyondnations.desktop.input;
using beyondnations.web;
using Silk.NET.Input;
using Xunit;

namespace beyondnationstests.web {

    public class TestBrowserKeys {

        [Theory]
        [InlineData("KeyA", Key.A)]
        [InlineData("KeyW", Key.W)]
        [InlineData("KeyZ", Key.Z)]
        [InlineData("Digit0", Key.Number0)]
        [InlineData("Digit9", Key.Number9)]
        [InlineData("Numpad5", Key.Keypad5)]
        [InlineData("F1", Key.F1)]
        [InlineData("F12", Key.F12)]
        [InlineData("ArrowUp", Key.Up)]
        [InlineData("Space", Key.Space)]
        [InlineData("ShiftLeft", Key.ShiftLeft)]
        [InlineData("Escape", Key.Escape)]
        [InlineData("PageUp", Key.PageUp)]
        [InlineData("Insert", Key.Insert)]
        public void codesMapToTheSameKeysGlfwReports(string code, Key expected) {
            Assert.Equal(expected, BrowserKeys.fromCode(code));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("Keyа")]
        [InlineData("F0")]
        [InlineData("F99")]
        [InlineData("IntlBackslash")]
        public void unknownCodesAreUnknown(string code) {
            Assert.Equal(Key.Unknown, BrowserKeys.fromCode(code));
        }

        /** Every key in the binding table can be pressed from a browser. */
        [Fact]
        public void everyBindingHasABrowserCode() {
            foreach (System.Reflection.FieldInfo field in typeof(KeyBindings).GetFields()) {
                Key bound = (Key) field.GetValue(null);
                bool found = false;
                foreach (string code in Codes) {
                    if (BrowserKeys.fromCode(code) == bound) {
                        found = true;
                        break;
                    }
                }
                Assert.True(found, field.Name + " (" + bound + ") has no KeyboardEvent.code");
            }
        }

        private static readonly string[] Codes = BuildCodes();

        private static string[] BuildCodes() {
            System.Collections.Generic.List<string> codes = new System.Collections.Generic.List<string>();
            for (char c = 'A'; c <= 'Z'; c++) codes.Add("Key" + c);
            for (int i = 1; i <= 24; i++) codes.Add("F" + i);
            codes.AddRange(new[] { "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Space", "ShiftLeft", "Escape",
                "Insert", "PageUp", "PageDown", "Enter", "Tab", "Slash" });
            return codes.ToArray();
        }
    }
}
