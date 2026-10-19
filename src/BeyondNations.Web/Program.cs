using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using beyondnations;
using beyondnations.desktop;
using beyondnations.web.host;

[assembly: SupportedOSPlatform("browser")]

namespace beyondnations.web {

    /**
    * Entry point for the browser build. The page (wwwroot/main.js) has
    * already registered the webhost module and created the WebGL2 context;
    * Main reads the options from the URL, builds the game and hands it to the
    * host, whose frame clock then drives it.
    *
    * Usage reporting: the browser build sends nothing. The desktop build's
    * one startup event (telemetry/UsageReporting.cs) is not compiled in --
    * the arcade already counts page loads without identifying anyone, and a
    * trace write key in a public page would be anyone's to use. See
    * docs/WEB.md.
    */
    public static class Program {

        public static int Main(string[] args) {
            Log.setSink((level, message) => {
                WebPage.log(level == LogLevel.Error ? "error" : level == LogLevel.Warning ? "warn" : "info", message);
            });

            List<string> arguments = WebOptions.toArguments(WebPage.query(), warning => Log.warning(warning));
            GameOptions options = GameOptions.parse(arguments.ToArray());
            if (arguments.Count > 0) {
                Log.info("options from the URL: " + string.Join(" ", arguments));
            }

            try {
                WebHostExports.register(new WebGame(options, WebPage.devicePixelRatio()));
            } catch (Exception e) {
                WebPage.fatal("The game failed to start: " + e.Message);
                WebPage.log("error", e.ToString());
                return 1;
            }
            return 0;
        }
    }
}
