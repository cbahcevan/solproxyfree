# SolProxy SDKs

Free rotating proxy pool with a one-line upgrade to residential proxies.

| Language | Package | Folder |
|----------|---------|--------|
| Python   | `pip install solproxy` | [python/](python) |
| C# / .NET | `dotnet add package SolProxy` | [csharp/](csharp) |

Both are thin clients for the gateway at `ws.solproxy.net:8500`. No credentials means the free pool; a username and token from [solproxy.net](https://solproxy.net/pricing) means residential.

## Releasing

- Python: bump `__version__` in `python/src/solproxy/__init__.py`, then push tag `py-vX.Y.Z`.
- C#: bump `<Version>` in `csharp/src/SolProxy/SolProxy.csproj`, then push tag `cs-vX.Y.Z`.

The release workflow fails if the tag and the version in the code differ.
