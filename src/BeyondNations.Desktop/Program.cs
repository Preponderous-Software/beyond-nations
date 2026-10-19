using System;
using System.Collections.Generic;
using System.IO;
using beyondnations;
using beyondnations.desktop.telemetry;
using StephensonSoftware.Trace;

namespace beyondnations.desktop {

    public static class Program {

        public static int Main(string[] args) {
            GameOptions options = GameOptions.parse(args);

            // Core logs through a sink so that it needs no engine and no console
            // of its own. The host decides where the output goes. Debug lines --
            // one per simulation event, thousands in a long session -- filled
            // the console window a Windows launcher leaves open, so they are
            // shown only with --debug-mode.
            Log.setSink((level, message) => {
                if (level == LogLevel.Debug) {
                    if (options.DebugMode) {
                        Console.WriteLine("[debug] " + message);
                    }
                } else if (level == LogLevel.Error) {
                    Console.Error.WriteLine("[error] " + message);
                } else if (level == LogLevel.Warning) {
                    Console.Error.WriteLine("[warn ] " + message);
                } else {
                    Console.WriteLine("[info ] " + message);
                }
            });

            // One `startup` event to trace, on a background thread; see
            // UsageReporting for what is sent and every way to turn it off.
            // Disposing the client on the way out gives the event up to five
            // seconds to be delivered, so a short run is still counted.
            using (TraceClient usageReporting = UsageReporting.start(
                       options.NoUsageReporting,
                       UsageReporting.defaultDefaultsPath(),
                       UsageReporting.defaultSettingsPath(),
                       message => Log.info(message),
                       message => Log.warning(message))) {
                Game game = null;
                List<string> report = new List<string>();
                int exitCode = runAndReport(
                    () => { game = new Game(options); game.run(); },
                    () => game?.Dispose(),
                    () => game != null && game.getFramesRendered() > 0,
                    line => { Console.Error.WriteLine(line); report.Add(line); });
                if (exitCode != 0) {
                    reportCrashFile(report);
                }
                return exitCode;
            }
        }

        /**
        * Saves what went wrong to a file and says where. On Windows the game is
        * started from a launcher whose console closes when the game does, so
        * the error printed above was gone before anyone could read it, and a
        * bug report had nothing to attach.
        */
        private static void reportCrashFile(List<string> report) {
            try {
                string path = writeCrashReport(AppDataPaths.getCrashReportsDirectory(), report, DateTime.Now);
                Console.Error.WriteLine("[error] a crash report was saved to " + path);
            } catch (Exception e) {
                Console.Error.WriteLine("[warn ] the crash report could not be saved: " + e.Message);
            }
        }

        /**
        * Writes the report lines, with the game version and the time, to a new
        * file in directory and returns its path. Public so a test can write
        * one into a temporary directory.
        */
        public static string writeCrashReport(string directory, IEnumerable<string> lines, DateTime now) {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "crash_" + now.ToString("yyyyMMdd_HHmmss") + ".txt");
            List<string> contents = new List<string> {
                "Beyond Nations " + beyondnations.desktop.ui.GameVersion.getLabel(),
                "Time: " + now.ToString("yyyy-MM-dd HH:mm:ss"),
                "OS: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ""
            };
            contents.AddRange(lines);
            File.WriteAllLines(path, contents);
            return path;
        }

        /**
        * Runs the game and reports what went wrong if it did. An exception
        * thrown from inside the window's loop leaves Silk.NET believing the
        * loop is still running, so disposing the window afterwards throws
        * "You cannot call Reset inside of the render loop!" -- which, inside a
        * using block, replaced the real exception and was all a player saw.
        * The real one is reported first now, and a failure to dispose after
        * it is only a warning.
        */
        public static int runAndReport(Action run, Action dispose, Func<bool> started, Action<string> error) {
            int exitCode = 0;
            try {
                run();
            } catch (Exception e) {
                error(started()
                    ? "[error] the game crashed: " + e.Message
                    : "[error] the host failed to start: " + e.Message);
                error(e.ToString());
                exitCode = 1;
            }
            try {
                dispose();
            } catch (Exception e) {
                if (exitCode == 0) {
                    throw;
                }
                error("[warn ] cleanup after the error above also failed: " + e.Message);
            }
            return exitCode;
        }
    }
}
