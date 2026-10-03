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
