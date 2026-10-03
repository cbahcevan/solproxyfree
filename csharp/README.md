# SolProxy: free rotating proxies for .NET

```bash
dotnet add package SolProxy
```

```csharp
using SolProxy;

var http = SolProxyClient.Create();          // free proxy pool, no signup
Console.WriteLine(await http.GetStringAsync("https://api.ipify.org"));
```

Upgrade to residential proxies by adding your credentials:

```csharp
var http = SolProxyClient.Create(new SolProxyOptions { Username = "you", Token = "YOUR_TOKEN" });
```

Residential proxies cost **$1.00–1.20/GB, pay as you go**. Get a token at [solproxy.net/pricing](https://solproxy.net/pricing).

## Free proxy pool

The free pool is built from public proxy lists. It is not an unchecked list: every proxy is re-tested once a minute from the gateway itself, by downloading a fixed file and verifying its checksum and its TLS certificate. Only proxies that pass are served. If a proxy dies mid-session, the gateway swaps in another one.

Free proxies are still free proxies. They are slow, they disappear, and many sites block them. Use them to try things out, and switch to residential when you need it to work.

The live list is at [solproxy.net/free-proxy-list](https://solproxy.net/free-proxy-list).

## Country, sticky IP, proxy type

```csharp
SolProxyClient.Create(new SolProxyOptions { Country = "de" });                  // prefer a German exit
SolProxyClient.Create(new SolProxyOptions { Sticky = "10m" });                  // same exit IP for 10 minutes
SolProxyClient.Create(new SolProxyOptions { Sticky = "10m", Session = "a1" });  // independent sticky sessions
SolProxyClient.Create(new SolProxyOptions { Type = "residential" });            // free pool only
```

`Sticky` accepts `1m`, `2m`, `5m`, `10m`, `30m`, `1h`. Country is a preference: if no exit in that country is alive, another country is used. Type is strict: if no proxy of that type is alive, the request fails with 503.

## Your own HttpClient, Playwright, Selenium

```csharp
var handler = SolProxyClient.CreateHandler(options);   // HttpClientHandler
IWebProxy proxy = SolProxyClient.CreateProxy(options);  // any IWebProxy consumer
string url = SolProxyClient.ProxyUrl(options);          // http://user:token@host:port
```
