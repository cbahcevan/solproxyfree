# SolProxy: free proxies for .NET that actually work

```bash
dotnet add package SolProxy
```

```csharp
using SolProxy;

var http = SolProxyClient.Create();
Console.WriteLine(await http.GetStringAsync("https://api.ipify.org"));   // a free proxy's IP, not yours
```

No signup, no API key, no proxy list to maintain.

## Why most free proxy lists don't work, and what this does instead

Public free proxy lists are mostly dead. The ones that answer are often too slow to load a page, and some tamper with HTTPS.

SolProxy collects free proxies from public sources and re-tests every one of them **once a minute**:

- it downloads a fixed file through the proxy and checks its checksum, so proxies that are too slow or that corrupt data are dropped;
- it verifies the target's TLS certificate, so proxies that intercept HTTPS are dropped;
- only proxies that passed the latest test are served.

`SolProxyClient.Create()` sends each request through one of those live proxies. If one dies mid-session, the gateway swaps in another one.

## Pick country, type, or keep the same IP

```csharp
SolProxyClient.Create(new SolProxyOptions { Country = "de" });                  // prefer a German exit
SolProxyClient.Create(new SolProxyOptions { Type = "residential" });            // residential, datacenter or mobile
SolProxyClient.Create(new SolProxyOptions { Sticky = "10m" });                  // same exit IP for 10 minutes
SolProxyClient.Create(new SolProxyOptions { Sticky = "10m", Session = "a1" });  // independent sticky sessions
```

`Sticky` accepts `1m`, `2m`, `5m`, `10m`, `30m`, `1h`. Country is a preference: if no live proxy is in that country, another country is used. Type is strict: if no proxy of that type is alive, the request fails with 503.

## Get the list itself

```csharp
foreach (var proxy in await SolProxyClient.GetFreeProxiesAsync(country: "us", type: "residential"))
    Console.WriteLine(proxy);   // http://ip:port
```

The same list is on [solproxy.net/free-proxy-list](https://solproxy.net/free-proxy-list). A list goes stale within minutes, so `Create()` is the better choice for anything that runs for a while.

## Your own HttpClient, Playwright, Selenium

```csharp
var handler = SolProxyClient.CreateHandler(options);   // HttpClientHandler
IWebProxy proxy = SolProxyClient.CreateProxy(options);  // any IWebProxy consumer
string url = SolProxyClient.ProxyUrl(options);          // http://user:pass@host:port
```

## When free isn't enough

Free proxies are still free proxies: slow, short-lived, and blocked by many sites. If you need every request to work, the same code runs on SolProxy residential proxies with one change:

```csharp
var http = SolProxyClient.Create(new SolProxyOptions { Username = "you", Token = "YOUR_TOKEN" });
```

Residential is $1.00–1.20/GB, pay as you go. See [solproxy.net/pricing](https://solproxy.net/pricing).
