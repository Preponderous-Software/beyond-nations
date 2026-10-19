using System;
using System.Collections.Generic;

namespace beyondnations.desktop.ui {

    /**
    * Breaks text into lines no wider than a limit, at spaces, given a way to
    * measure a string. Done here rather than with ImGui's own wrapping so the
    * browser build only makes the ImGui calls the shared UI already used
    * (measuring and plain text), and so the breaking can be tested without a
    * frame.
    */
    public static class TextWrap {

        public static List<string> wrap(string text, float maxWidth, Func<string, float> measure) {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) {
                return lines;
            }
            string current = "";
            foreach (string word in text.Split(' ')) {
                string candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length > 0 && measure(candidate) > maxWidth) {
                    lines.Add(current);
                    current = word;
                } else {
                    current = candidate;
                }
            }
            if (current.Length > 0) {
                lines.Add(current);
            }
            return lines;
        }
    }
}
