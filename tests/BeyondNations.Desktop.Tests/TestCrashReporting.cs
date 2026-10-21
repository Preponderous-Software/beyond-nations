using System;
using System.Collections.Generic;
using Xunit;

using beyondnations.desktop;

namespace beyondnationstests {

    /**
    * A crash inside the window loop used to be replaced on the console by
    * the exception Silk.NET throws when the window is then disposed, so the
    * real cause was never shown.
    */
    public class TestCrashReporting {

        [Fact]
        public void testCrashMidGame_ReportsTheRealException_NotTheDisposeOne() {
            List<string> output = new List<string>();

            int exitCode = Program.runAndReport(
                () => throw new NullReferenceException("the real cause"),
                () => throw new InvalidOperationException("You cannot call `Reset` inside of the render loop!"),
                () => true,
                output.Add);

            Assert.Equal(1, exitCode);
            Assert.Equal("[error] the game crashed: the real cause", output[0]);
            Assert.Contains("NullReferenceException", output[1]);
            Assert.Contains("cleanup after the error above also failed", output[2]);
        }

        [Fact]
        public void testFailureBeforeTheFirstFrame_ReportsFailedToStart() {
            List<string> output = new List<string>();

            int exitCode = Program.runAndReport(
                () => throw new InvalidOperationException("no GL"),
                () => { },
                () => false,
                output.Add);

            Assert.Equal(1, exitCode);
            Assert.Equal("[error] the host failed to start: no GL", output[0]);
            Assert.Equal(2, output.Count);
        }

        [Fact]
        public void testCleanRun_ExitsZero_AndPrintsNothing() {
            List<string> output = new List<string>();
            bool disposed = false;

            int exitCode = Program.runAndReport(() => { }, () => disposed = true, () => true, output.Add);

            Assert.Equal(0, exitCode);
            Assert.True(disposed);
            Assert.Empty(output);
        }

        [Fact]
        public void testCrashReport_IsWrittenWithTheErrorLines() {
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bn-crash-test-" + Guid.NewGuid());
            try {
                string path = Program.writeCrashReport(
                    directory,
                    new List<string> { "[error] the game crashed: the real cause" },
                    new DateTime(2026, 10, 18, 20, 30, 0));

                Assert.Equal(System.IO.Path.Combine(directory, "crash_20261018_203000.txt"), path);
                string text = System.IO.File.ReadAllText(path);
                Assert.StartsWith("Beyond Nations v", text);
                Assert.Contains("[error] the game crashed: the real cause", text);
            } finally {
                if (System.IO.Directory.Exists(directory)) {
                    System.IO.Directory.Delete(directory, true);
                }
            }
        }
    }
}
