"""SolProxy client: free proxy pool with a one-line upgrade to residential.

    import solproxy

    s = solproxy.Session()                                   # free pool
    s = solproxy.Session(country="de", sticky="10m")         # free, flags
    s = solproxy.Session(username="me", token="TOKEN")       # paid residential

Both pools go through the same gateway (ws.solproxy.net:8500); only the
credentials differ. Flags travel in the proxy username, exactly as the
gateway parses them (backend/proxyserver.py `_parse_username_flags`).
"""

from typing import Dict, Optional

import requests
from requests.adapters import HTTPAdapter

__version__ = "0.1.0"
__all__ = ["proxy_url", "proxies", "Session", "HOST", "PORT"]

HOST = "ws.solproxy.net"
PORT = 8500
USER_AGENT = f"solproxy-py/{__version__}"

STICKY = ("1m", "2m", "5m", "10m", "30m", "1h")
TYPES = ("residential", "datacenter", "mobile")  # free pool only


def proxy_url(
    username: Optional[str] = None,
    token: Optional[str] = None,
    country: Optional[str] = None,
    sticky: Optional[str] = None,
    session: Optional[str] = None,
    type: Optional[str] = None,
    host: str = HOST,
    port: int = PORT,
) -> str:
    """Build the proxy URL. No username/token means the free pool."""
    if bool(username) != bool(token):
        raise ValueError("username and token go together")
    if sticky and sticky not in STICKY:
        raise ValueError(f"sticky must be one of {STICKY}")
    if type:
        if username:
            raise ValueError("type only applies to the free pool")
        if type not in TYPES:
            raise ValueError(f"type must be one of {TYPES}")
    if country and (len(country) != 2 or not country.isalpha()):
        raise ValueError("country is a 2-letter code, e.g. 'de'")
    if session and ("-" in session or ":" in session or "@" in session):
        raise ValueError("session may not contain '-', ':' or '@'")

    parts = [
        username or "free",
        type,
        country.lower() if country else None,
        f"sticky{sticky}" if sticky else None,
        session,
    ]
    user = "-".join(p for p in parts if p)
    return f"http://{user}:{token or 'x'}@{host}:{port}"


def proxies(**kwargs) -> Dict[str, str]:  # type: ignore[no-untyped-def]
    """`proxies=` dict for requests / httpx."""
    url = proxy_url(**kwargs)
    return {"http": url, "https": url}


class _Adapter(HTTPAdapter):
    # HTTPS goes through CONNECT; the gateway only sees CONNECT headers,
    # so the client signature has to ride there to show up in its logs.
    def proxy_headers(self, proxy: str) -> Dict[str, str]:
        headers = super().proxy_headers(proxy)
        headers["User-Agent"] = USER_AGENT
        return headers


class Session(requests.Session):
    """requests.Session routed through SolProxy.

    Free pool proxies die mid-session; the gateway swaps in another one,
    but a request can still fail. `retries` re-sends on connection
    errors. The paid pool rarely needs it.
    """

    def __init__(
        self,
        username: Optional[str] = None,
        token: Optional[str] = None,
        country: Optional[str] = None,
        sticky: Optional[str] = None,
        session: Optional[str] = None,
        type: Optional[str] = None,
        retries: int = 2,
        timeout: float = 30,
    ) -> None:
        super().__init__()
        self.proxies.update(
            proxies(
                username=username,
                token=token,
                country=country,
                sticky=sticky,
                session=session,
                type=type,
            )
        )
        self.headers["User-Agent"] = USER_AGENT
        self.timeout = timeout
        adapter = _Adapter(max_retries=retries)
        self.mount("http://", adapter)
        self.mount("https://", adapter)

    def request(self, method, url, *args, **kwargs):  # type: ignore[no-untyped-def]
        kwargs.setdefault("timeout", self.timeout)
        # requests lets HTTP(S)_PROXY from the environment override
        # session.proxies; passing them per request keeps ours in front.
        kwargs.setdefault("proxies", self.proxies)
        return super().request(method, url, *args, **kwargs)
