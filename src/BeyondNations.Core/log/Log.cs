using System;

namespace beyondnations {

    public enum LogLevel {
        Info,
        Warning,
        Error
    }

    /**
    * The simulation's logging seam.
    *
    * UnityEngine.Debug is the most heavily referenced engine API in this
    * codebase, and unlike Vector3 it has no equivalent in the base class
    * library. Core therefore logs through here and the host decides where the
    * output goes, which keeps the simulation free of any engine dependency
    * without losing the messages.
    *
    * Unlike RandomSource this is deliberately static, because logging is
    * cross-cutting and its call sites are static today. Randomness is injected
    * because it has to be seedable per world; logging does not.
    *
    * The default sink discards everything, so tests stay quiet unless they opt
    * in. The host installs a real sink at startup.
    */
    public static class Log {
        private static Action<LogLevel, string> sink = delegate { };

        public static void setSink(Action<LogLevel, string> newSink) {
            sink = newSink ?? delegate { };
        }

        public static void info(object message) {
            sink(LogLevel.Info, describe(message));
        }

        public static void warning(object message) {
            sink(LogLevel.Warning, describe(message));
        }

        public static void error(object message) {
            sink(LogLevel.Error, describe(message));
        }

        private static string describe(object message) {
            return message == null ? "null" : message.ToString();
        }
    }
}
