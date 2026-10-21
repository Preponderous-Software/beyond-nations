using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace beyondnationstests {

    /**
    * The browser build compiles the desktop's ImGui screens as they are, and
    * ImGui.NET's wrappers for Dear ImGui's printf-style functions call
    * variadic C functions with a fixed argument list. On WebAssembly that
    * traps ("null function or function signature mismatch") the first frame
    * the call runs, so none of them may appear in the shared source; use
    * Overlay.textColoured, or ImGui.TextUnformatted (with PushTextWrapPos for
    * wrapping). See docs/WEB.md, "Variadic ImGui functions".
    */
    public class TestNoVariadicImGuiCalls {

        // The ImGui.NET methods whose native function is variadic in cimgui
        // 1.90.8 (cimgui.h: igText, igTextColored, igTextDisabled,
        // igTextWrapped, igLabelText, igBulletText, igSetTooltip,
        // igSetItemTooltip, igLogText, and the format overloads of TreeNode
        // and TreeNodeEx, which this guard does not try to tell apart).
        private static readonly Regex VariadicCall = new Regex(
            @"\bImGui\.(Text|TextColored|TextDisabled|TextWrapped|LabelText|BulletText|SetTooltip|SetItemTooltip|LogText)\s*\(");

        [Fact]
        public void testSharedUiSourceMakesNoVariadicImGuiCalls() {
            string root = findRepositoryRoot();
            List<string> offenders = new List<string>();
            foreach (string dir in new[] { "src/BeyondNations.Desktop", "src/BeyondNations.Web" }) {
                foreach (string file in Directory.GetFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories)) {
                    string normalised = file.Replace('\\', '/');
                    if (normalised.Contains("/obj/") || normalised.Contains("/bin/")) {
                        continue;
                    }
                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++) {
                        string code = lines[i].TrimStart();
                        if (code.StartsWith("//") || code.StartsWith("*")) {
                            continue;
                        }
                        if (VariadicCall.IsMatch(code)) {
                            offenders.Add(Path.GetRelativePath(root, file) + ":" + (i + 1) + ": " + code);
                        }
                    }
                }
            }
            Assert.True(offenders.Count == 0,
                "Variadic ImGui calls crash the browser build; use Overlay.textColoured or ImGui.TextUnformatted:\n"
                + string.Join("\n", offenders));
        }

        [Fact]
        public void testTheGuardRecognisesAVariadicCall() {
            Assert.Matches(VariadicCall, "ImGui.TextColored(colour, text);");
            Assert.Matches(VariadicCall, "ImGui.Text (\"x\");");
            Assert.DoesNotMatch(VariadicCall, "ImGui.TextUnformatted(text);");
            Assert.DoesNotMatch(VariadicCall, "Overlay.textColoured(colour, text);");
        }

        private static string findRepositoryRoot() {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BeyondNations.sln"))) {
                dir = dir.Parent;
            }
            Assert.NotNull(dir);
            return dir.FullName;
        }
    }
}
