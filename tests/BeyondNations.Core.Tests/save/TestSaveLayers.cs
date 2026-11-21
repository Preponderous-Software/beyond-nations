using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace beyondnationstests {

    /**
    * The shape of src/BeyondNations.Core/save (its README): model, core and
    * ports know nothing of the adapters and touch no file, clock or
    * environment themselves; each port and each adapter is one file holding
    * one type. Everything is in one namespace, so this reads the source
    * rather than the compiled references.
    */
    public class TestSaveLayers {
        private static readonly string[] InnerLayers = { "model", "core", "ports" };

        // the outside world, reached only through a port
        private static readonly string[] OutsideWorld = {
            @"\bFile\.", @"\bDirectory\.", @"\bFileStream\b", @"\bDateTime\.(UtcNow|Now|Today)\b",
            @"\bEnvironment\.", @"\bConsole\.", @"\bThread\.Sleep\b"
        };

        // seams the approved port map leaves inline, each with its reason
        private static readonly Dictionary<string, string> Allowed = new Dictionary<string, string> {
            // SaveGame.encode(simulation, version): the clockless overload the tests and the
            // pacing tool call; SaveSession stamps saves through IClock instead
            { "core/SaveGame.cs", @"\bDateTime\.(UtcNow|Now|Today)\b" },
        };

        [Fact]
        public void testInnerLayers_NeitherNameAnAdapter_NorReachOutside() {
            string save = saveDirectory();
            List<string> adapters = Directory.GetFiles(Path.Combine(save, "adapters"), "*.cs")
                .Select(Path.GetFileNameWithoutExtension).ToList();
            adapters.Add("LocalStorageSaveStorage");
            Assert.Contains("FileSaveStorage", adapters);

            List<string> problems = new List<string>();
            foreach (string layer in InnerLayers) {
                foreach (string path in Directory.GetFiles(Path.Combine(save, layer), "*.cs")) {
                    string relative = layer + "/" + Path.GetFileName(path);
                    string code = codeOnly(File.ReadAllText(path));
                    foreach (string adapter in adapters) {
                        if (Regex.IsMatch(code, @"\b" + adapter + @"\b")) {
                            problems.Add(relative + " names the adapter " + adapter);
                        }
                    }
                    foreach (string pattern in OutsideWorld) {
                        string allowed;
                        if (Allowed.TryGetValue(relative, out allowed) && allowed == pattern) {
                            continue;
                        }
                        Match match = Regex.Match(code, pattern);
                        if (match.Success) {
                            problems.Add(relative + " reaches outside: " + match.Value);
                        }
                    }
                }
            }
            Assert.Empty(problems);
        }

        [Fact]
        public void testEachPortAndAdapter_IsOneTypeInAFileOfItsName() {
            string save = saveDirectory();
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(Path.Combine(save, "ports"), "*.cs"));
            files.AddRange(Directory.GetFiles(Path.Combine(save, "adapters"), "*.cs"));
            files.Add(Path.Combine(repositoryRoot(), "src", "BeyondNations.Web", "game", "LocalStorageSaveStorage.cs"));
            Assert.True(files.Count >= 4);

            List<string> problems = new List<string>();
            foreach (string path in files) {
                List<string> types = Regex.Matches(codeOnly(File.ReadAllText(path)),
                        @"^    (?:public |internal )?(?:static |sealed |abstract |partial )*(?:class|interface|struct|enum|record) (\w+)",
                        RegexOptions.Multiline)
                    .Select(m => m.Groups[1].Value).ToList();
                string name = Path.GetFileNameWithoutExtension(path);
                if (types.Count != 1 || types[0] != name) {
                    problems.Add(name + ".cs declares [" + string.Join(", ", types) + "]");
                }
            }
            Assert.Empty(problems);
        }

        private static string codeOnly(string code) {
            code = Regex.Replace(code, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            code = Regex.Replace(code, @"//[^\n]*", " ");
            return Regex.Replace(code, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
        }

        private static string saveDirectory() {
            return Path.Combine(repositoryRoot(), "src", "BeyondNations.Core", "save");
        }

        private static string repositoryRoot() {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BeyondNations.sln"))) {
                dir = dir.Parent;
            }
            Assert.NotNull(dir);
            return dir.FullName;
        }
    }
}
