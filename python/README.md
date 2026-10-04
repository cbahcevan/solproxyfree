# solproxy: free proxies for Python that actually work

```bash
pip install solproxy
```

```python
import solproxy

s = solproxy.Session()
print(s.get("https://api.ipify.org?format=json").json())   # a free proxy's IP, not yours
```

No signup, no API key, no proxy list to maintain.

## Why most free proxy lists don't work, and what this does instead

Public free proxy lists are mostly dead. The ones that answer are often too slow to load a page, and some tamper with HTTPS.

SolProxy collects free proxies from public sources and re-tests every one of them **once a minute**:

- it downloads a fixed file through the proxy and checks its checksum, so proxies that are too slow or that corrupt data are dropped;
- it verifies the target's TLS certificate, so proxies that intercept HTTPS are dropped;
- only proxies that passed the latest test are served.

`solproxy.Session()` sends each request through one of those live proxies. If one dies mid-session, the gateway swaps in another one, and the session retries on connection errors.

## Pick country, type, or keep the same IP

```python
s = solproxy.Session(country="de")                 # prefer a German exit
s = solproxy.Session(type="residential")           # residential, datacenter or mobile
s = solproxy.Session(sticky="10m")                 # same exit IP for 10 minutes
s = solproxy.Session(sticky="10m", session="a1")   # several independent sticky sessions
```

`sticky` accepts `1m`, `2m`, `5m`, `10m`, `30m`, `1h`. Country is a preference: if no live proxy is in that country, another country is used. Type is strict: if no proxy of that type is alive, the request fails with 503.

## Get the list itself

```python
for p in solproxy.free_proxies(country="us", type="residential"):
    print(p["url"], p["country"], p["latency_ms"], p["uptime"])
```

Each item has `ip`, `port`, `url`, `country`, `type`, `isp`, `latency_ms`, `uptime` (%) and `last_checked`. The same list is on [solproxy.net/free-proxy-list](https://solproxy.net/free-proxy-list). A list goes stale within minutes, so `Session()` is the better choice for anything that runs for a while.

## Use it with httpx, aiohttp, curl or anything else

```python
import httpx, solproxy

httpx.get("https://example.com", proxy=solproxy.proxy_url(country="us"))
requests.get("https://example.com", proxies=solproxy.proxies(country="us"))
```

```bash
curl -x "$(python -c 'import solproxy; print(solproxy.proxy_url())')" https://api.ipify.org
```

## Ports

The free endpoint is `ws.solproxy.net:8501` and takes no credentials, so the plain URL works in any tool:

```bash
curl -x http://ws.solproxy.net:8501 https://api.ipify.org
```

If only port 8500 is allowed out of your network, the same free proxies are there under the username `free`:

```python
s = solproxy.Session(port=8500)              # http://free:x@ws.solproxy.net:8500
```

Residential always uses 8500.

## When free isn't enough

Free proxies are still free proxies: slow, short-lived, and blocked by many sites. If you need every request to work, the same code runs on SolProxy residential proxies with one change:

```python
s = solproxy.Session(username="you", token="YOUR_TOKEN")
```

Residential is $1.00–1.20/GB, pay as you go. See [solproxy.net/pricing](https://solproxy.net/pricing).
