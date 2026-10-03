import pytest

import solproxy


def test_free_default():
    assert solproxy.proxy_url() == "http://free:x@ws.solproxy.net:8500"


def test_free_flags():
    url = solproxy.proxy_url(country="DE", sticky="10m", type="residential")
    assert url == "http://free-residential-de-sticky10m:x@ws.solproxy.net:8500"


def test_paid():
    url = solproxy.proxy_url(username="cenk", token="T", country="tr", session="b2")
    assert url == "http://cenk-tr-b2:T@ws.solproxy.net:8500"


@pytest.mark.parametrize(
    "kw",
    [
        {"username": "u"},
        {"sticky": "3m"},
        {"country": "usa"},
        {"username": "u", "token": "t", "type": "mobile"},
        {"session": "a-b"},
    ],
)
def test_rejects(kw):
    with pytest.raises(ValueError):
        solproxy.proxy_url(**kw)


def test_session_wires_proxy_and_signature():
    s = solproxy.Session(country="de")
    assert s.proxies["https"] == "http://free-de:x@ws.solproxy.net:8500"
    adapter = s.get_adapter("https://example.com")
    assert adapter.proxy_headers(s.proxies["https"])["User-Agent"].startswith("solproxy-py/")
    assert "Proxy-Authorization" in adapter.proxy_headers(s.proxies["https"])


def test_free_proxies(monkeypatch):
    seen = {}

    class Resp:
        def raise_for_status(self):
            pass

        def json(self):
            return {"proxies": [{"ip": "1.2.3.4", "port": 8080, "country": "DE"}]}

    def fake_get(url, params, timeout, headers):
        seen.update(url=url, params=params, ua=headers["User-Agent"])
        return Resp()

    monkeypatch.setattr(solproxy.client.requests, "get", fake_get)
    rows = solproxy.free_proxies(country="de", type="residential")
    assert rows[0]["url"] == "http://1.2.3.4:8080"
    assert seen["url"] == "https://solproxy.net/free-proxy-list/api.json"
    assert seen["params"] == {"country": "de", "type": "residential"}
    assert seen["ua"].startswith("solproxy-py/")


def test_free_proxies_rejects_bad_type():
    with pytest.raises(ValueError):
        solproxy.free_proxies(type="residental")
