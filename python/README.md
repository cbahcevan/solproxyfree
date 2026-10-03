# solproxy: free rotating proxies for Python

```bash
pip install solproxy
```

```python
import solproxy

s = solproxy.Session()                       # free proxy pool, no signup
print(s.get("https://api.ipify.org?format=json").json())
```

Upgrade to residential proxies by adding one line:

```python
s = solproxy.Session(username="you", token="YOUR_TOKEN")
```

Residential proxies cost **$1.00–1.20/GB, pay as you go**. Get a token at [solproxy.net/pricing](https://solproxy.net/pricing).

## Free proxy pool

The free pool is built from public proxy lists. It is not an unchecked list: every proxy is re-tested once a minute from the gateway itself, by downloading a fixed file and verifying its checksum and its TLS certificate. Only proxies that pass are served. If a proxy dies mid-session, the gateway swaps in another one.

Free proxies are still free proxies. They are slow, they disappear, and many sites block them. Use them to try things out, and switch to residential when you need it to work.

The live list is at [solproxy.net/free-proxy-list](https://solproxy.net/free-proxy-list).

## Country, sticky IP, proxy type

```python
s = solproxy.Session(country="de")                 # prefer a German exit
s = solproxy.Session(sticky="10m")                 # same exit IP for 10 minutes
s = solproxy.Session(sticky="10m", session="a1")   # several independent sticky sessions
s = solproxy.Session(type="residential")           # free pool only: residential, datacenter, mobile
```

`sticky` accepts `1m`, `2m`, `5m`, `10m`, `30m`, `1h`. Country is a preference: if no exit in that country is alive, another country is used. Type is strict: if no proxy of that type is alive, the request fails with 503.

## Rotating proxies with requests, httpx or anything else

`solproxy.Session` is a normal `requests.Session`. To use another client, take the URL:

```python
import httpx, solproxy

url = solproxy.proxy_url(country="us")
httpx.get("https://example.com", proxy=url)
```

```python
requests.get("https://example.com", proxies=solproxy.proxies(country="us"))
```

```bash
curl -x "$(python -c 'import solproxy; print(solproxy.proxy_url())')" https://api.ipify.org
```

## Free vs residential

|                 | Free pool             | SolProxy residential     |
|-----------------|-----------------------|--------------------------|
| Price           | $0                    | $1.00–1.20/GB            |
| Signup          | No                    | Yes                      |
| Exit IPs        | Public open proxies   | Real residential IPs     |
| Uptime of an IP | Minutes               | Stable, sticky up to 1h  |
| Country, sticky | Yes                   | Yes                      |
