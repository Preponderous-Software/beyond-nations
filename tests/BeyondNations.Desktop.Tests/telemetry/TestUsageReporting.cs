using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Xunit;

using beyondnations.desktop.telemetry;
using beyondnations.desktop.ui;
using StephensonSoftware.Trace;

namespace beyondnationstests.desktop.telemetry {

    /**
    * Usage reporting, end to end but never against the real server: every
    * test that could send points the endpoint at a loopback HttpListener, and
    * the rest have no key or are switched off. The two opt-out environment
    * variables are cleared for each test and restored afterwards, so a
    * DO_NOT_TRACK on the machine running the suite cannot skew it.
    */
    [Collection("environment")]
    public class TestUsageReporting : IDisposable {
        private readonly string directory;
        private readonly string defaultsPath;
        private readonly string settingsPath;
        private readonly string savedTraceUsageReporting;
        private readonly string savedDoNotTrack;
        private readonly List<string> infos = new List<string>();
        private readonly List<string> warnings = new List<string>();

        public TestUsageReporting() {
            directory = Path.Combine(Path.GetTempPath(), "bn-usage-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            defaultsPath = Path.Combine(directory, UsageReporting.DefaultsFileName);
            settingsPath = Path.Combine(directory, "data", UsageReporting.SettingsFileName);
            savedTraceUsageReporting = Environment.GetEnvironmentVariable("TRACE_USAGE_REPORTING");
            savedDoNotTrack = Environment.GetEnvironmentVariable("DO_NOT_TRACK");
            Environment.SetEnvironmentVariable("TRACE_USAGE_REPORTING", null);
            Environment.SetEnvironmentVariable("DO_NOT_TRACK", null);
        }

        public void Dispose() {
            Environment.SetEnvironmentVariable("TRACE_USAGE_REPORTING", savedTraceUsageReporting);
            Environment.SetEnvironmentVariable("DO_NOT_TRACK", savedDoNotTrack);
            try {
                Directory.Delete(directory, true);
            } catch (IOException) {
                // a temp directory left behind is harmless
            }
        }

        private void writeDefaults(string endpoint, string key) {
            File.WriteAllText(defaultsPath,
                "// comment, as the shipped file has\n{ \"enabled\": true, \"endpoint\": \"" + endpoint + "\", \"key\": \"" + key + "\" }");
        }

        private void writeSettings(string json) {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            File.WriteAllText(settingsPath, json);
        }

        private TraceClient start(bool switchedOff = false) {
            return UsageReporting.start(switchedOff, defaultsPath, settingsPath, infos.Add, warnings.Add);
        }

        [Fact]
        public void testTheShippedDefaultsNameTheRealServerAndCarryAKey() {
            // The file that ships beside the executable, as the build copies it.
            string shipped = UsageReporting.defaultDefaultsPath();

            UsageReporting.Settings settings = UsageReporting.load(shipped, settingsPath, warnings.Add);

            Assert.Empty(warnings);
            Assert.True(settings.Enabled);
            Assert.Equal("https://trace.danielstephenson.dev", settings.Endpoint);
            Assert.False(string.IsNullOrWhiteSpace(settings.Key));
        }

        [Fact]
        public void testFirstRunCreatesSettingsShowingReportingOnAndPrintsTheNotice() {
            using (Stub stub = new Stub()) {
                writeDefaults(stub.BaseUrl, "test-key");

                TraceClient client = start();
                client.Dispose();

                Assert.True(File.Exists(settingsPath));
                Assert.Contains("\"enabled\": true", File.ReadAllText(settingsPath));
                string notice = Assert.Single(infos);
                Assert.StartsWith("Usage reporting is on: Beyond Nations sends its name and version", notice);
                Assert.Contains(settingsPath, notice);
                Assert.Contains("--no-usage-reporting", notice);
                Assert.Contains("TRACE_USAGE_REPORTING=off", notice);
                Assert.Contains("DO_NOT_TRACK=1", notice);
                Assert.EndsWith("Details: https://github.com/Stephenson-Software/trace#usage-reporting", notice);
            }
        }

        [Fact]
        public void testStartupIsReportedWithTheVersionAndTheKey() {
            using (Stub stub = new Stub()) {
                writeDefaults("https://must-not-be-used.invalid", "bundled-key");
                writeSettings("{ \"usageReporting\": { \"enabled\": true, \"endpoint\": \"" + stub.BaseUrl + "\", \"key\": \"test-key\" } }");

                TraceClient client = start();
                client.Dispose(); // drains the queued event

                Assert.Equal(
                    "{\"application\":\"beyond-nations\",\"name\":\"startup\",\"tags\":{\"version\":\"" + GameVersion.Version + "\"}}",
                    stub.Body);
                Assert.Equal("Bearer test-key", stub.Authorization);
                Assert.Empty(infos); // not the first run, reporting on: nothing to say
            }
        }

        [Fact]
        public void testSettingsEnabledFalseTurnsReportingOff() {
            using (Stub stub = new Stub()) {
                writeDefaults(stub.BaseUrl, "test-key");
                writeSettings("{ \"usageReporting\": { \"enabled\": false } }");

                TraceClient client = start();
                client.Dispose();

                Assert.False(client.IsEnabled);
                Assert.Equal("config", client.DisabledReason);
                Assert.Equal("Usage reporting is off (config).", Assert.Single(infos));
                Assert.Null(stub.Body);
            }
        }

        [Fact]
        public void testTheCommandLineSwitchTurnsReportingOff() {
            using (Stub stub = new Stub()) {
                writeDefaults(stub.BaseUrl, "test-key");

                TraceClient client = start(switchedOff: true);
                client.Dispose();

                Assert.Equal("config", client.DisabledReason);
                Assert.Null(stub.Body);
            }
        }

        [Theory]
        [InlineData("TRACE_USAGE_REPORTING", "off")]
        [InlineData("DO_NOT_TRACK", "1")]
        public void testTheEnvironmentTurnsReportingOffOverEverything(string variable, string value) {
            using (Stub stub = new Stub()) {
                writeDefaults(stub.BaseUrl, "test-key");
                writeSettings("{ \"usageReporting\": { \"enabled\": true } }");
                Environment.SetEnvironmentVariable(variable, value);

                TraceClient client = start();
                client.Dispose();

                Assert.Equal("environment", client.DisabledReason);
                Assert.Equal("Usage reporting is off (environment).", Assert.Single(infos));
                Assert.Null(stub.Body);
            }
        }

        [Fact]
        public void testAnUnreadableSettingsFileCountsAsOffAndIsLeftAlone() {
            using (Stub stub = new Stub()) {
                writeDefaults(stub.BaseUrl, "test-key");
                writeSettings("{ \"usageReporting\": { \"enabled\": fals");

                TraceClient client = start();
                client.Dispose();

                Assert.Equal("config", client.DisabledReason);
                Assert.Single(warnings);
                Assert.Equal("{ \"usageReporting\": { \"enabled\": fals", File.ReadAllText(settingsPath));
                Assert.Null(stub.Body);
            }
        }

        [Fact]
        public void testMissingDefaultsMeanNoKeyAndNothingSent() {
            TraceClient client = start();
            client.Dispose();

            Assert.Equal("no key", client.DisabledReason);
            Assert.Equal("Usage reporting is off (no key).", Assert.Single(infos));
        }

        /** A trace server stand-in that records the one request it expects. */
        private sealed class Stub : IDisposable {
            private readonly HttpListener listener = new HttpListener();
            private readonly Thread thread;
            public readonly string BaseUrl;
            public volatile string Body;
            public volatile string Authorization;

            public Stub() {
                TcpListener probe = new TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                int port = ((IPEndPoint) probe.LocalEndpoint).Port;
                probe.Stop();
                BaseUrl = "http://127.0.0.1:" + port;
                listener.Prefixes.Add(BaseUrl + "/");
                listener.Start();
                thread = new Thread(serve) { IsBackground = true };
                thread.Start();
            }

            private void serve() {
                while (listener.IsListening) {
                    try {
                        HttpListenerContext context = listener.GetContext();
                        using (StreamReader reader = new StreamReader(context.Request.InputStream)) {
                            Authorization = context.Request.Headers["Authorization"];
                            Body = reader.ReadToEnd();
                        }
                        context.Response.StatusCode = 201;
                        context.Response.Close();
                    } catch (Exception) {
                        return;
                    }
                }
            }

            public void Dispose() {
                try {
                    listener.Stop();
                    listener.Close();
                } catch (Exception) {
                    // already stopped
                }
            }
        }
    }
}
