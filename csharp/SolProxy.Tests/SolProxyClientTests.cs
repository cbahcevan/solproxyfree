using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SolProxy;
using Xunit;

public class SolProxyClientTests
{
    [Fact]
    public void FreeDefault() =>
        Assert.Equal("http://ws.solproxy.net:8501", SolProxyClient.ProxyUrl());

    [Fact]
    public void FreeFlags() =>
        Assert.Equal("http://residential-de-sticky10m:x@ws.solproxy.net:8501",
            SolProxyClient.ProxyUrl(new SolProxyOptions { Country = "DE", Sticky = "10m", Type = "residential" }));

    [Fact]
    public void FreeOnAccountPort()
    {
        Assert.Equal("http://free:x@ws.solproxy.net:8500",
            SolProxyClient.ProxyUrl(new SolProxyOptions { Port = 8500 }));
        Assert.Equal("http://free-de-sticky10m:x@ws.solproxy.net:8500",
            SolProxyClient.ProxyUrl(new SolProxyOptions { Country = "de", Sticky = "10m", Port = 8500 }));
    }

    [Fact]
    public void FreeDefaultHasNoCredentials()
    {
        var p = SolProxyClient.CreateProxy();
        Assert.Null(p.Credentials);
        Assert.Equal(new Uri("http://ws.solproxy.net:8501"), p.Address);
    }

    [Fact]
    public void Paid() =>
        Assert.Equal("http://cenk-tr-b2:T@ws.solproxy.net:8500",
            SolProxyClient.ProxyUrl(new SolProxyOptions { Username = "cenk", Token = "T", Country = "tr", Session = "b2" }));

    [Fact]
    public void ProxyCarriesCredentials()
    {
        var p = SolProxyClient.CreateProxy(new SolProxyOptions { Country = "de" });
        var c = p.Credentials.GetCredential(p.Address, "Basic");
        Assert.Equal("de", c.UserName);
        Assert.Equal(new Uri("http://ws.solproxy.net:8501"), p.Address);
    }

    [Theory]
    [InlineData("u", null, null, null, null, null)]
    [InlineData(null, null, null, "3m", null, null)]
    [InlineData(null, null, "usa", null, null, null)]
    [InlineData("u", "t", null, null, null, "mobile")]
    [InlineData(null, null, null, null, "a-b", null)]
    public void Rejects(string user, string token, string country, string sticky, string session, string type) =>
        Assert.Throws<ArgumentException>(() => SolProxyClient.ProxyUrl(new SolProxyOptions
        {
            Username = user,
            Token = token,
            Country = country,
            Sticky = sticky,
            Session = session,
            Type = type,
        }));

    [Fact]
    public void ClientSignature() =>
        Assert.StartsWith("solproxy-cs/0.1.0", SolProxyClient.Create().DefaultRequestHeaders.UserAgent.ToString());

    [Fact]
    public async Task FreeProxies()
    {
        var handler = new FakeHandler("1.2.3.4:8080\n5.6.7.8:3128\n");
        var list = await SolProxyClient.GetFreeProxiesAsync("de", "residential", new HttpClient(handler));
        Assert.Equal(new[] { new Uri("http://1.2.3.4:8080"), new Uri("http://5.6.7.8:3128") }, list);
        Assert.Equal("https://solproxy.net/free-proxy-list/api.txt?country=de&type=residential", handler.Url);
        Assert.StartsWith("solproxy-cs/", handler.UserAgent);
    }

    [Fact]
    public Task FreeProxiesRejectsBadType() =>
        Assert.ThrowsAsync<ArgumentException>(() => SolProxyClient.GetFreeProxiesAsync(type: "residental"));

    sealed class FakeHandler : HttpMessageHandler
    {
        readonly string _body;
        public string Url, UserAgent;
        public FakeHandler(string body) => _body = body;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Url = request.RequestUri.ToString();
            UserAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body) });
        }
    }
}
