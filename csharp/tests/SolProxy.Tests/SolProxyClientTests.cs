using System;
using System.Net;
using SolProxy;
using Xunit;

public class SolProxyClientTests
{
    [Fact]
    public void FreeDefault() =>
        Assert.Equal("http://free:x@ws.solproxy.net:8500", SolProxyClient.ProxyUrl());

    [Fact]
    public void FreeFlags() =>
        Assert.Equal("http://free-residential-de-sticky10m:x@ws.solproxy.net:8500",
            SolProxyClient.ProxyUrl(new SolProxyOptions { Country = "DE", Sticky = "10m", Type = "residential" }));

    [Fact]
    public void Paid() =>
        Assert.Equal("http://cenk-tr-b2:T@ws.solproxy.net:8500",
            SolProxyClient.ProxyUrl(new SolProxyOptions { Username = "cenk", Token = "T", Country = "tr", Session = "b2" }));

    [Fact]
    public void ProxyCarriesCredentials()
    {
        var p = SolProxyClient.CreateProxy(new SolProxyOptions { Country = "de" });
        var c = p.Credentials.GetCredential(p.Address, "Basic");
        Assert.Equal("free-de", c.UserName);
        Assert.Equal(new Uri("http://ws.solproxy.net:8500"), p.Address);
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
}
