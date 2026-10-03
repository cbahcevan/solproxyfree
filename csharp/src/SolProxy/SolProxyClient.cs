using System;
using System.Linq;
using System.Net;
using System.Net.Http;

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
        public int Port { get; set; } = SolProxyClient.DefaultPort;
    }

    /// <summary>
    /// Free proxy pool with a one-line upgrade to residential. Both pools go
    /// through the same gateway; flags travel in the proxy username.
    /// </summary>
    public static class SolProxyClient
    {
        public const string DefaultHost = "ws.solproxy.net";
        public const int DefaultPort = 8500;
        public static readonly string UserAgent =
            "solproxy-cs/" + typeof(SolProxyClient).Assembly.GetName().Version.ToString(3);

        static readonly string[] StickyValues = { "1m", "2m", "5m", "10m", "30m", "1h" };
        static readonly string[] Types = { "residential", "datacenter", "mobile" };

        /// <summary>Proxy username carrying the pool and flags, e.g. free-de-sticky10m.</summary>
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

            var parts = new[]
            {
                paid ? o.Username : "free",
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
            return "http://" + ProxyUsername(o) + ":" + (string.IsNullOrEmpty(o.Token) ? "x" : o.Token)
                   + "@" + o.Host + ":" + o.Port;
        }

        /// <summary>IWebProxy for any .NET client.</summary>
        public static WebProxy CreateProxy(SolProxyOptions options = null)
        {
            var o = options ?? new SolProxyOptions();
            return new WebProxy(new Uri("http://" + o.Host + ":" + o.Port))
            {
                Credentials = new NetworkCredential(ProxyUsername(o), string.IsNullOrEmpty(o.Token) ? "x" : o.Token),
            };
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
