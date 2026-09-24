/*
 * trace-client 0.1.0 -- https://github.com/Stephenson-Software/trace-client-csharp
 *
 * One call to report that a program was used. Copy this file into a project as
 * is, or reference the project; either way there is nothing else to add.
 * System.Net.Http only; netstandard2.0 and C# 7.3, so Unity and .NET Framework
 * 4.6.1+ can vendor it as well as modern .NET.
 *
 * MIT licensed. Keep this header when vendoring so the file can be found again.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace StephensonSoftware.Trace
{
    /// <summary>
    /// Reports usage events to a trace server, and never gets in the way of
    /// the program doing the reporting.
    /// </summary>
    /// <remarks>
    /// <para>Three properties hold for every call to <see cref="Report"/>:</para>
    /// <list type="bullet">
    ///   <item><b>It returns immediately.</b> The HTTP call happens on a single
    ///   background thread owned by this client. A game loop can report from its
    ///   main thread without a frame ever waiting on the network.</item>
    ///   <item><b>It never throws.</b> A server that is down, slow, or rejecting
    ///   the key is a dropped report, not an exception in the host program.
    ///   Failures go to the optional <c>log</c> callback and otherwise nowhere.</item>
    ///   <item><b>It is bounded.</b> At most <see cref="QueueCapacity"/> reports
    ///   wait to be sent; beyond that, new reports are dropped rather than
    ///   accumulated.</item>
    /// </list>
    /// <para>Reporting is opt-out, and the person running the program has the
    /// last word. The constructor checks, in this order, and the first match is
    /// what <see cref="DisabledReason"/> says: the environment
    /// (<c>TRACE_USAGE_REPORTING=off</c> or <c>DO_NOT_TRACK=1</c>) -- reason
    /// <c>environment</c>; the program's own setting, <c>enabled: false</c> --
    /// reason <c>config</c>; no key -- reason <c>no key</c>.</para>
    /// <code>
    /// var trace = new TraceClient("https://trace.example.org", "my-game",
    ///                             key: settings.UsageReportingKey,
    ///                             enabled: settings.UsageReportingEnabled);
    /// trace.Report("startup", tags: new Dictionary&lt;string, string&gt; { { "version", Version } });
    /// ...
    /// trace.Dispose(); // on shutdown: sends what is queued, bounded by the timeout
    /// </code>
    /// </remarks>
    public sealed class TraceClient : IDisposable
    {
        /// <summary>The client version, as sent in the User-Agent header.</summary>
        public const string Version = "0.1.0";

        /// <summary>How many reports may wait to be sent before new ones are dropped.</summary>
        public const int QueueCapacity = 256;

        /// <summary>Per-request timeout, and the total time <see cref="Close"/> waits for queued reports.</summary>
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        /// <summary>The most tags the server accepts on one report.</summary>
        public const int MaxTags = 32;

        /// <summary>The longest application, name, tag key or tag value the server accepts, in UTF-16 chars.</summary>
        public const int MaxLength = 255;

        /// <summary>Environment variable that turns reporting off: <c>off</c>, <c>false</c>, <c>0</c>, <c>no</c>.</summary>
        public const string EnvUsageReporting = "TRACE_USAGE_REPORTING";

        /// <summary>Environment variable that turns reporting off: <c>1</c>, <c>true</c>, <c>yes</c>. See https://consoledonottrack.com.</summary>
        public const string EnvDoNotTrack = "DO_NOT_TRACK";

        /// <summary>Reason given when an environment variable turned reporting off.</summary>
        public const string ReasonEnvironment = "environment";

        /// <summary>Reason given when the program's own setting turned reporting off.</summary>
        public const string ReasonConfig = "config";

        /// <summary>Reason given when no key was supplied.</summary>
        public const string ReasonNoKey = "no key";

        // Where environment variables come from. A seam rather than
        // System.Environment directly, so tests can point it at a dictionary;
        // nothing else should touch it.
        internal static Func<string, string> EnvironmentSource = System.Environment.GetEnvironmentVariable;

        private readonly Uri _endpoint;
        private readonly string _application;
        private readonly string _key;
        private readonly Action<string> _log;
        private readonly BlockingCollection<string> _queue;   // null when disabled
        private readonly Thread _thread;                       // null when disabled
        private readonly HttpClient _http;                     // null when disabled
        private readonly CancellationTokenSource _stop;        // null when disabled
        private int _closed;

        /// <summary>
        /// A client for the program named <paramref name="application"/>, reporting
        /// to the trace server at <paramref name="baseUrl"/>. Throws only for a
        /// missing or malformed <paramref name="baseUrl"/> or a missing
        /// <paramref name="application"/> -- programming errors, not runtime ones.
        /// </summary>
        /// <param name="baseUrl">The trace server, e.g. <c>https://trace.danielstephenson.dev</c>.</param>
        /// <param name="application">The program's name, exactly as its key was issued for.</param>
        /// <param name="key">The program's write key. Without one the client is a no-op.</param>
        /// <param name="enabled">The program's own opt-out. <c>false</c> yields a client that reports nothing.</param>
        /// <param name="log">Where dropped reports are mentioned. Optional; treat as debug-level.</param>
        public TraceClient(string baseUrl, string application, string key = null, bool enabled = true,
                           Action<string> log = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("baseUrl is required", "baseUrl");
            }
            if (string.IsNullOrWhiteSpace(application))
            {
                throw new ArgumentException("application is required", "application");
            }
            _endpoint = new Uri(baseUrl.Trim().TrimEnd('/') + "/api/metrics");
            _application = application.Trim();
            _key = key == null ? "" : key.Trim();
            _log = log;

            if (EnvironmentDisables())
            {
                DisabledReason = ReasonEnvironment;
            }
            else if (!enabled)
            {
                DisabledReason = ReasonConfig;
            }
            else if (_key.Length == 0)
            {
                DisabledReason = ReasonNoKey;
            }

            if (DisabledReason != null)
            {
                return;
            }
            _queue = new BlockingCollection<string>(new ConcurrentQueue<string>(), QueueCapacity);
            _stop = new CancellationTokenSource();
            _http = new HttpClient { Timeout = Timeout };
            _thread = new Thread(Drain) { IsBackground = true, Name = "trace-client/" + _application };
            _thread.Start();
        }

        /// <summary>A client that reports nothing. Useful as a default before settings are read.</summary>
        public static TraceClient Disabled()
        {
            return new TraceClient("http://disabled.invalid", "disabled", enabled: false);
        }

        /// <summary>
        /// Why <see cref="Report"/> sends nothing: <c>null</c> when enabled, otherwise
        /// <see cref="ReasonEnvironment"/>, <see cref="ReasonConfig"/> or
        /// <see cref="ReasonNoKey"/>, verbatim, so a program can print
        /// <c>"Usage reporting is off (" + reason + ")."</c>. Decided once, in the constructor.
        /// </summary>
        public string DisabledReason { get; private set; }

        /// <summary>Whether <see cref="Report"/> will actually send anything. <c>false</c> after <see cref="Close"/> too.</summary>
        public bool IsEnabled
        {
            get { return _queue != null && Volatile.Read(ref _closed) == 0; }
        }

        /// <summary>
        /// Whether the environment asks for usage reporting to be off, via
        /// <c>TRACE_USAGE_REPORTING=off</c> or <c>DO_NOT_TRACK=1</c>. Only the listed
        /// values count; anything else (including empty) leaves the program's
        /// own setting in charge.
        /// </summary>
        public static bool EnvironmentDisables()
        {
            try
            {
                return IsOff(EnvironmentSource(EnvUsageReporting)) || IsYes(EnvironmentSource(EnvDoNotTrack));
            }
            catch (Exception)
            {
                return false; // e.g. a SecurityException in a sandbox; the program's setting stays in charge
            }
        }

        private static bool IsOff(string value)
        {
            if (value == null)
            {
                return false;
            }
            string v = value.Trim().ToLowerInvariant();
            return v == "off" || v == "false" || v == "0" || v == "no";
        }

        private static bool IsYes(string value)
        {
            if (value == null)
            {
                return false;
            }
            string v = value.Trim().ToLowerInvariant();
            return v == "1" || v == "true" || v == "yes";
        }

        /// <summary>
        /// Reports that <paramref name="name"/> happened, with an optional numeric
        /// value and optional string tags. Returns immediately and never throws.
        /// A report the server would reject for its size -- more than
        /// <see cref="MaxTags"/> tags, or a string longer than <see cref="MaxLength"/> --
        /// is dropped here instead of being sent.
        /// </summary>
        public void Report(string name, double? value = null, IEnumerable<KeyValuePair<string, string>> tags = null)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            try
            {
                string body;
                string problem = Json(_application, name, value, tags, out body);
                if (problem != null)
                {
                    Log("dropped " + name + ": " + problem);
                    return;
                }
                if (!_queue.TryAdd(body))
                {
                    Log("queue full, dropped " + name);
                }
            }
            catch (Exception failure)
            {
                // InvalidOperationException when Close() raced us, or anything
                // else: a usage report must never be the reason a program stops.
                Log("could not queue " + name + ": " + failure.Message);
            }
        }

        /// <summary>
        /// Stops the sending thread, giving reports already queued up to
        /// <see cref="Timeout"/> in total to be sent first. A program that reports
        /// and exits within milliseconds -- a CLI -- would otherwise lose its one
        /// event. The bound still holds: an unreachable server delays exit by at
        /// most the timeout, never a hang; whatever has not been sent by then is
        /// dropped. Safe to call more than once, and on a disabled client.
        /// </summary>
        public void Close()
        {
            if (_queue == null || Interlocked.Exchange(ref _closed, 1) == 1)
            {
                return;
            }
            try
            {
                _queue.CompleteAdding(); // no new work; what is queued still runs
                if (!_thread.Join(Timeout))
                {
                    _stop.Cancel(); // aborts the in-flight request; the rest is dropped
                    _thread.Join(TimeSpan.FromMilliseconds(500));
                }
            }
            catch (Exception failure)
            {
                Log("could not close cleanly: " + failure.Message);
            }
        }

        /// <summary>Same as <see cref="Close"/>.</summary>
        public void Dispose()
        {
            Close();
        }

        private void Drain()
        {
            try
            {
                foreach (string body in _queue.GetConsumingEnumerable(_stop.Token))
                {
                    Send(body);
                }
            }
            catch (Exception)
            {
                // OperationCanceledException from Close() giving up; nothing to report.
            }
            finally
            {
                try
                {
                    _http.Dispose();
                }
                catch (Exception)
                {
                    // nothing useful to do
                }
            }
        }

        private void Send(string body)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, _endpoint))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _key);
                    request.Headers.TryAddWithoutValidation("User-Agent", "trace-client/" + Version + " (" + _application + ")");
                    using (HttpResponseMessage response = _http.SendAsync(request, _stop.Token).GetAwaiter().GetResult())
                    {
                        int status = (int)response.StatusCode;
                        if (status != 201)
                        {
                            Log("trace server answered " + status + " for " + body);
                        }
                    }
                }
            }
            catch (Exception failure)
            {
                // Timeouts, refused connections, TLS failures, a disposed client:
                // all a dropped report, never an exception in the host.
                Log("could not deliver " + body + ": " + failure.GetType().Name + ": " + failure.Message);
            }
        }

        private void Log(string message)
        {
            if (_log == null)
            {
                return;
            }
            try
            {
                _log("[trace] " + message);
            }
            catch (Exception)
            {
                // a throwing logger must not break the promise either
            }
        }

        // JSON is written by hand so this file has no dependencies. The shape is
        // fixed and small -- three scalars and a flat string map. Returns null
        // and sets body when the report is sendable, otherwise the reason it is not.
        internal static string Json(string application, string name, double? value,
                                    IEnumerable<KeyValuePair<string, string>> tags, out string body)
        {
            body = null;
            if (application.Length > MaxLength)
            {
                return "application longer than " + MaxLength + " characters";
            }
            if (name.Length > MaxLength)
            {
                return "name longer than " + MaxLength + " characters";
            }
            var out_ = new StringBuilder(128);
            out_.Append("{\"application\":").Append(Quote(application));
            out_.Append(",\"name\":").Append(Quote(name));
            if (value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value))
            {
                out_.Append(",\"value\":").Append(value.Value.ToString("R", CultureInfo.InvariantCulture));
            }
            if (tags != null)
            {
                int count = 0;
                var tagJson = new StringBuilder();
                foreach (KeyValuePair<string, string> tag in tags)
                {
                    // The server rejects blank keys and null values outright; skip
                    // them so one bad tag does not cost the whole report.
                    if (string.IsNullOrWhiteSpace(tag.Key) || tag.Value == null)
                    {
                        continue;
                    }
                    if (tag.Key.Length > MaxLength || tag.Value.Length > MaxLength)
                    {
                        return "tag " + tag.Key.Substring(0, Math.Min(tag.Key.Length, 32)) + " longer than " + MaxLength + " characters";
                    }
                    if (++count > MaxTags)
                    {
                        return "more than " + MaxTags + " tags";
                    }
                    if (count > 1)
                    {
                        tagJson.Append(',');
                    }
                    tagJson.Append(Quote(tag.Key)).Append(':').Append(Quote(tag.Value));
                }
                if (count > 0)
                {
                    out_.Append(",\"tags\":{").Append(tagJson).Append('}');
                }
            }
            body = out_.Append('}').ToString();
            return null;
        }

        internal static string Quote(string text)
        {
            var out_ = new StringBuilder(text.Length + 2).Append('"');
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"': out_.Append("\\\""); break;
                    case '\\': out_.Append("\\\\"); break;
                    case '\n': out_.Append("\\n"); break;
                    case '\r': out_.Append("\\r"); break;
                    case '\t': out_.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            out_.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            out_.Append(c);
                        }
                        break;
                }
            }
            return out_.Append('"').ToString();
        }
    }
}
