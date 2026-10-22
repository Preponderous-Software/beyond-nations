using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace beyondnationstests {

    /**
    * The browser build compiles a hand-picked list of the desktop's source
    * files (the Compile items in BeyondNations.Web.csproj). A new desktop
    * file that shared code uses but that is not on the list builds and tests
    * fine on the desktop and breaks only the web build in CI. This reads the
    * list, and fails when a shared file names a type declared in a desktop
    * file that is not on it.
    */
    public class TestWebSharedSources {
        private static readonly Regex CompileItem = new Regex("<Compile\\s+Include=\"\\$\\(DesktopDir\\)([^\"]+)\"(?:\\s+Exclude=\"\\$\\(DesktopDir\\)([^\"]+)\")?");
        private static readonly Regex TypeDeclaration = new Regex(@"\b(?:class|struct|enum|interface)\s+([A-Z]\w*)");

        [Fact]
        public void testEveryDesktopTypeTheWebBuildUsesIsCompiledIntoIt() {
            string root = findRepositoryRoot();
            string desktop = Path.Combine(root, "src/BeyondNations.Desktop");
            string csproj = File.ReadAllText(Path.Combine(root, "src/BeyondNations.Web/BeyondNations.Web.csproj"));

            List<string> all = Directory.GetFiles(desktop, "*.cs", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(desktop, file).Replace('\\', '/'))
                .Where(file => !file.StartsWith("bin/") && !file.StartsWith("obj/"))
                .ToList();
            HashSet<string> shared = new HashSet<string>();
            foreach (Match item in CompileItem.Matches(csproj)) {
                string include = item.Groups[1].Value;
                string exclude = item.Groups[2].Success ? item.Groups[2].Value : null;
                foreach (string file in all) {
                    if (matches(include, file) && (exclude == null || !matches(exclude, file))) {
                        shared.Add(file);
                    }
                }
            }
            Assert.Contains("input/PlayerInputController.cs", shared);

            // the types declared only in files the web build leaves out
            Dictionary<string, string> leftOut = new Dictionary<string, string>();
            foreach (string file in all.Where(file => !shared.Contains(file))) {
                foreach (Match declaration in TypeDeclaration.Matches(stripComments(File.ReadAllText(Path.Combine(desktop, file))))) {
                    leftOut[declaration.Groups[1].Value] = file;
                }
            }
            foreach (string file in shared) {
                foreach (Match declaration in TypeDeclaration.Matches(stripComments(File.ReadAllText(Path.Combine(desktop, file))))) {
                    leftOut.Remove(declaration.Groups[1].Value);
                }
            }

            List<string> missing = new List<string>();
            foreach (string file in shared) {
                string code = stripComments(File.ReadAllText(Path.Combine(desktop, file)));
                foreach (KeyValuePair<string, string> type in leftOut) {
                    if (Regex.IsMatch(code, @"\b" + type.Key + @"\b")) {
                        missing.Add(file + " uses " + type.Key + " from " + type.Value);
                    }
                }
            }
            Assert.True(missing.Count == 0,
                "add these to the Compile items in src/BeyondNations.Web/BeyondNations.Web.csproj:\n" + string.Join("\n", missing));
        }

        /** A Compile glob ("ui/**\/*.cs", "input/KeyBindings.cs") against a path under the desktop project. */
        private static bool matches(string glob, string file) {
            string pattern = "^" + Regex.Escape(glob)
                .Replace(@"\*\*/", "(.*/)?")
                .Replace(@"\*", "[^/]*") + "$";
            return Regex.IsMatch(file, pattern);
        }

        private static string stripComments(string code) {
            code = Regex.Replace(code, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            code = Regex.Replace(code, @"//[^\n]*", " ");
            return Regex.Replace(code, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
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
