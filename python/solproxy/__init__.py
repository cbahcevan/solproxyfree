"""Free proxies for Python, collected and re-tested every minute by SolProxy.

The code is in client.py.
"""

__version__ = "0.1.0"

from .client import FREE_PORT, HOST, PORT, Session, free_proxies, proxies, proxy_url

__all__ = ["free_proxies", "proxy_url", "proxies", "Session", "HOST", "PORT", "FREE_PORT"]
