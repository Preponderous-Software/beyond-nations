using System;
using beyondnations;
using beyondnations.desktop.telemetry;
using StephensonSoftware.Trace;

namespace beyondnations.desktop {

    public static class Program {

        public static int Main(string[] args) {
            // Core logs through a sink so that it needs no engine and no console
            // of its own. The host decides where the output goes.
            Log.setSink((level, message) => {
                if (level == LogLevel.Error) {
                    Console.Error.WriteLine("[error] " + message);
                } else if (level == LogLevel.Warning) {
                    Console.Error.WriteLine("[warn ] " + message);
                } else {
                    Console.WriteLine("[info ] " + message);
                }
            });

            GameOptions options = GameOptions.parse(args);

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
                try {
                    using (Game game = new Game(options)) {
                        game.run();
                    }
                } catch (Exception e) {
                    Console.Error.WriteLine("[error] the host failed to start: " + e.Message);
                    Console.Error.WriteLine(e);
                    return 1;
                }
            }

            return 0;
        }
    }
}
