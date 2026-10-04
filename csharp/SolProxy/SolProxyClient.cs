using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SolProxy
{
    /// <summary>Connection options. Leave Username/Token empty for the free pool.</summary>
    public sealed class SolProxyOptions
    {
        public string Username { get; set; }
        public string Token { get; set; }
        /// <summary>2-letter country code, a preference: another country is used if none is alive.</summary>
        public string Country { get; set; }
        /// <summary>Keep the same exit IP: 1m, 2m, 5m, 10m, 30m or 1h.</summary>
        public string Sticky { get; set; }
        /// <summary>Name for an independent sticky session.</summary>
        public string Session { get; set; }
        /// <summary>Free pool only, strict: residential, datacenter or mobile.</summary>
        public string Type { get; set; }
        public string Host { get; set; } = SolProxyClient.DefaultHost;
        /// <summary>
        /// Leave null for the default: 8501 for the free pool, 8500 for the paid one.
        /// Set 8500 to reach the free pool through the account endpoint.
        /// </summary>
        public int? Port { get; set; }
    }

    /// <summary>
    /// Free proxies collected and re-tested every minute by SolProxy, with a
    /// one-line upgrade to residential. Both pools go
    /// through the same gateway: the free pool on 8501 with no credentials,
    /// the paid pool on 8500. Flags travel in the proxy username.
    /// </summary>
    public static class SolProxyClient
    {
        public const string DefaultHost = "ws.solproxy.net";
        /// <summary>Account endpoint; the free pool is the username "free" here.</summary>
        public const int DefaultPort = 8500;
        /// <summary>Free pool, no credentials.</summary>
        public const int DefaultFreePort = 8501;
        public const string FreeListUrl = "https://solproxy.net/free-proxy-list/api.txt";
        public static readonly string UserAgent =
            "solproxy-cs/" + typeof(SolProxyClient).Assembly.GetName().Version.ToString(3);

        static readonly string[] StickyValues = { "1m", "2m", "5m", "10m", "30m", "1h" };
        static readonly string[] Types = { "residential", "datacenter", "mobile" };

        /// <summary>
        /// The free proxies that passed the gateway's latest probe, as http://ip:port.
        /// They die within minutes; Create() picks a live one per request instead.
        /// </summary>
        public static async Task<IReadOnlyList<Uri>> GetFreeProxiesAsync(
            string country = null, string type = null, HttpClient http = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(type) && !Types.Contains(type))
                throw new ArgumentException("Type must be one of " + string.Join(", ", Types));
            if (!string.IsNullOrEmpty(country) && (country.Length != 2 || !country.All(char.IsLetter)))
                throw new ArgumentException("Country is a 2-letter code, e.g. \"de\"");

            var query = new List<string>();
            if (!string.IsNullOrEmpty(country)) query.Add("country=" + Uri.EscapeDataString(country));
            if (!string.IsNullOrEmpty(type)) query.Add("type=" + Uri.EscapeDataString(type));
            var url = FreeListUrl + (query.Count > 0 ? "?" + string.Join("&", query) : "");

            var owned = http == null;
            http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                    using (var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return body.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(line => new Uri("http://" + line.Trim()))
                            .ToList();
                    }
                }
            }
            finally
            {
                if (owned) http.Dispose();
            }
        }

        /// <summary>Port the options resolve to.</summary>
        public static int ResolvePort(SolProxyOptions o) =>
            o?.Port ?? (string.IsNullOrEmpty(o?.Username) ? DefaultFreePort : DefaultPort);

        /// <summary>
        /// Proxy username carrying the pool and flags, e.g. free-de-sticky10m on 8500
        /// or de-sticky10m on 8501. Empty when the free pool is used on 8501 without flags.
        /// </summary>
        public static string ProxyUsername(SolProxyOptions o)
        {
            o = o ?? new SolProxyOptions();
            bool paid = !string.IsNullOrEmpty(o.Username);
            if (paid != !string.IsNullOrEmpty(o.Token))
                throw new ArgumentException("Username and Token go together");
            if (!string.IsNullOrEmpty(o.Sticky) && !StickyValues.Contains(o.Sticky))
                throw new ArgumentException("Sticky must be one of " + string.Join(", ", StickyValues));
            if (!string.IsNullOrEmpty(o.Type))
            {
                if (paid) throw new ArgumentException("Type only applies to the free pool");
                if (!Types.Contains(o.Type))
                    throw new ArgumentException("Type must be one of " + string.Join(", ", Types));
            }
            if (!string.IsNullOrEmpty(o.Country) && (o.Country.Length != 2 || !o.Country.All(char.IsLetter)))
                throw new ArgumentException("Country is a 2-letter code, e.g. \"de\"");
            if (!string.IsNullOrEmpty(o.Session) && o.Session.IndexOfAny(new[] { '-', ':', '@' }) >= 0)
                throw new ArgumentException("Session may not contain '-', ':' or '@'");

            // 8501 has no account, so every part of the username is a flag there;
            // a leading "free" would be read as a session key.
            bool anonymous = !paid && ResolvePort(o) == DefaultFreePort;
            var parts = new[]
            {
                paid ? o.Username : anonymous ? null : "free",
                o.Type,
                o.Country?.ToLowerInvariant(),
                string.IsNullOrEmpty(o.Sticky) ? null : "sticky" + o.Sticky,
                o.Session,
            };
            return string.Join("-", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        /// <summary>Proxy URL with credentials, for clients that take one string.</summary>
        public static string ProxyUrl(SolProxyOptions options = null)
        {
            var o = options ?? new SolProxyOptions();
            var user = ProxyUsername(o);
            var auth = user.Length == 0 ? "" : user + ":" + (string.IsNullOrEmpty(o.Token) ? "x" : o.Token) + "@";
            return "http://" + auth + o.Host + ":" + ResolvePort(o);
        }

        /// <summary>IWebProxy for any .NET client.</summary>
        public static WebProxy CreateProxy(SolProxyOptions options = null)
        {
            var o = options ?? new SolProxyOptions();
            var user = ProxyUsername(o);
            var proxy = new WebProxy(new Uri("http://" + o.Host + ":" + ResolvePort(o)));
            if (user.Length > 0)
                proxy.Credentials = new NetworkCredential(user, string.IsNullOrEmpty(o.Token) ? "x" : o.Token);
            return proxy;
        }

        public static HttpClientHandler CreateHandler(SolProxyOptions options = null) =>
            new HttpClientHandler { Proxy = CreateProxy(options), UseProxy = true };

        /// <summary>HttpClient routed through SolProxy.</summary>
        public static HttpClient Create(SolProxyOptions options = null)
        {
            var client = new HttpClient(CreateHandler(options), disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
            return client;
        }
    }
}
