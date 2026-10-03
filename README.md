# SolProxy free proxies: Python and .NET

Free proxies that actually work. SolProxy collects free proxies from public sources, re-tests each one every minute (real download, checksum and TLS certificate check) and serves only the live ones through a single rotating endpoint.

| Language | Install | Folder |
|----------|---------|--------|
| Python   | `pip install solproxy` | [python/](python) |
| C# / .NET | `dotnet add package SolProxy` | [csharp/](csharp) |

```python
import solproxy
solproxy.Session().get("https://example.com")       # through a live free proxy
solproxy.free_proxies(country="de")                 # or just the list
```

Both packages are thin clients for the gateway at `ws.solproxy.net:8500`. When free isn't enough, a username and token from [solproxy.net](https://solproxy.net/pricing) switch the same code to residential proxies.

## Releasing

- Python: bump `__version__` in `python/solproxy/__init__.py`, then push tag `py-vX.Y.Z`.
- C#: bump `<Version>` in `csharp/SolProxy/SolProxy.csproj`, then push tag `cs-vX.Y.Z`.

The release workflow fails if the tag and the version in the code differ.
