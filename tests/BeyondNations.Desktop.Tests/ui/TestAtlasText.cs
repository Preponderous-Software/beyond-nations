using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

using beyondnations.desktop.ui;

namespace beyondnationstests {

    /**
    * Text the UI's bitmap font can draw. The font (ImGui's default,
    * ProggyClean) holds only U+0020..U+00FF, and drawn below the size its
    * atlas was rasterised at it loses the one-pixel rows hyphens and full
    * stops sit on. "Nothing now -- but" on the claimant sheet read
    * "Nothing now    but" for the second reason, not the first: the text is
    * two ASCII hyphens.
    */
    public class TestAtlasText {

        [Fact]
        public void testAFontSizeIsNeverBelowTheAtlasSize() {
            float atlas = Overlay.BaseFontSize;
            Assert.Equal(atlas, Overlay.atLeastAtlasFontSize(atlas * 0.75f));
            Assert.Equal(atlas * 1.5f, Overlay.atLeastAtlasFontSize(atlas * 1.5f));
            Assert.Equal(1f, Overlay.fontScaleFor(Overlay.atLeastAtlasFontSize(atlas * 0.5f)), 3);
        }

        [Fact]
        public void testTheSourceDrawsNoGlyphTheAtlasLacks() {
            // every string the game can show comes from these projects; a
            // glyph past U+00FF (an em dash, curly quotes, an arrow) has no
            // pixels in the atlas and is drawn as a gap or a '?'
            string root = findRepositoryRoot();
            List<string> offenders = new List<string>();
            foreach (string dir in new[] { "src/BeyondNations.Core", "src/BeyondNations.Desktop", "src/BeyondNations.Web" }) {
                foreach (string file in Directory.GetFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories)) {
                    string normalised = file.Replace('\\', '/');
                    if (normalised.Contains("/obj/") || normalised.Contains("/bin/")) {
                        continue;
                    }
                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++) {
                        string code = lines[i].TrimStart();
                        if (code.StartsWith("//") || code.StartsWith("*") || code.StartsWith("/*")) {
                            continue;
                        }
                        foreach (char c in code) {
                            if (c > 'ÿ') {
                                offenders.Add(Path.GetRelativePath(root, file) + ":" + (i + 1) + ": U+" + ((int) c).ToString("X4") + " in " + code);
                                break;
                            }
                        }
                    }
                }
            }
            Assert.True(offenders.Count == 0,
                "The UI font has no glyph past U+00FF; use ASCII (\"--\" for a dash, \"->\" for an arrow):\n"
                + string.Join("\n", offenders));
        }

        private static string findRepositoryRoot() {
            string dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "BeyondNations.sln"))) {
                dir = Path.GetDirectoryName(dir);
            }
            Assert.NotNull(dir);
            return dir;
        }
    }
}
