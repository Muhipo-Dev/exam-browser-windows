using System.Collections.Generic;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using Xunit;

namespace ExambroMuhipo.Tests;

public class WhitelistAndNavigationTests
{
    private readonly NavigationPolicy _policy = new();

    private ExamProfile CreateTestProfile()
    {
        return new ExamProfile
        {
            ExamName = "Profil Uji Standar",
            StartUrl = "https://example.com/cbt",
            AllowedDomains = new List<string>
            {
                "example.com",
                "*.example.com",
                "muhipo.sch.id"
            },
            AllowedUrls = new List<string>
            {
                "https://trusted-cdn.org/assets/*"
            },
            EnforceHttps = true
        };
    }

    [Theory]
    [InlineData("https://example.com/cbt", true)]
    [InlineData("https://sub.example.com/cbt/login", true)]
    [InlineData("https://exam.sub.example.com/cbt", true)]
    [InlineData("https://muhipo.sch.id/ujian", true)]
    [InlineData("https://trusted-cdn.org/assets/style.css", true)]
    [InlineData("about:blank", true)]
    public void EvaluateNavigation_ShouldAllow_WhenUrlOrDomainIsInWhitelist(string url, bool expectedAllowed)
    {
        var profile = CreateTestProfile();
        var result = _policy.EvaluateNavigation(url, profile);

        Assert.Equal(expectedAllowed, result.IsAllowed);
        if (expectedAllowed)
        {
            Assert.Equal(NavigationDecisionType.Allowed, result.Decision);
        }
    }

    [Theory]
    [InlineData("https://google.com")]
    [InlineData("https://wikipedia.org")]
    [InlineData("https://other-school.sch.id")]
    [InlineData("https://malicious.com")]
    public void EvaluateNavigation_ShouldBlock_WhenDomainIsNotInWhitelist(string url)
    {
        var profile = CreateTestProfile();
        var result = _policy.EvaluateNavigation(url, profile);

        Assert.False(result.IsAllowed);
        Assert.Equal(NavigationDecisionType.BlockedDomain, result.Decision);
    }

    [Fact]
    public void EvaluateNavigation_ShouldBlock_WhenHttpIsUsedAndHttpsEnforced()
    {
        var profile = CreateTestProfile();
        profile.EnforceHttps = true;

        var result = _policy.EvaluateNavigation("http://example.com/cbt", profile);

        Assert.False(result.IsAllowed);
        Assert.Equal(NavigationDecisionType.BlockedNonHttps, result.Decision);
    }

    [Theory]
    [InlineData("http://192.168.1.200:8080/cbt")]
    [InlineData("http://10.0.0.15/ujian")]
    [InlineData("http://172.16.0.5:8000/")]
    [InlineData("http://localhost:8888/login")]
    [InlineData("http://127.0.0.1:8080/exam")]
    public void EvaluateNavigation_ShouldAllowLocalIpHttp_EvenWhenHttpsEnforced(string localUrl)
    {
        var profile = new ExamProfile
        {
            ExamName = "Ujian CBT Server Lokal",
            StartUrl = localUrl,
            AllowedDomains = new List<string> { "192.168.1.200", "10.0.0.15", "172.16.0.5", "localhost", "127.0.0.1" },
            EnforceHttps = true,
            AllowLocalIpHttp = true
        };

        var result = _policy.EvaluateNavigation(localUrl, profile);
        Assert.True(result.IsAllowed, $"Harus mengizinkan IP lokal HTTP: {localUrl}. Alasan: {result.Reason}");
        Assert.Equal(NavigationDecisionType.Allowed, result.Decision);
    }

    [Theory]
    [InlineData("not a valid url")]
    [InlineData("http:///bad")]
    [InlineData("")]
    public void EvaluateNavigation_ShouldBlock_WhenUrlIsMalformed(string url)
    {
        var profile = CreateTestProfile();
        var result = _policy.EvaluateNavigation(url, profile);

        Assert.False(result.IsAllowed);
        Assert.Equal(NavigationDecisionType.BlockedMalformedUri, result.Decision);
    }

    [Theory]
    [InlineData("data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=")]
    [InlineData("blob:http://192.168.1.200:8080/guid-12345")]
    [InlineData("about:blank")]
    [InlineData("about:srcdoc")]
    public void EvaluateNavigation_ShouldAllow_DataBlobAndAboutSchemes(string url)
    {
        var profile = CreateTestProfile();
        var result = _policy.EvaluateNavigation(url, profile);

        Assert.True(result.IsAllowed, $"Protokol web sah {url} harus diizinkan.");
        Assert.Equal(NavigationDecisionType.Allowed, result.Decision);
    }

    [Fact]
    public void EvaluateNavigation_ShouldAutoAllowLocalIp_WithoutExplicitDomainWhitelist()
    {
        var profile = new ExamProfile
        {
            ExamName = "Server CBT Lab",
            StartUrl = "http://192.168.1.200:8080/cbt",
            AllowedDomains = new List<string>(), // Whitelist kosong
            AllowLocalIpHttp = true,
            EnforceHttps = true
        };

        var result = _policy.EvaluateNavigation("http://192.168.1.200:8080/cbt/login.php", profile);
        Assert.True(result.IsAllowed);
        Assert.Equal(NavigationDecisionType.Allowed, result.Decision);
    }

    [Fact]
    public void EvaluateNavigation_ShouldNormalizeAndAllow_LocalIpWithoutScheme()
    {
        var profile = new ExamProfile
        {
            ExamName = "Server CBT Lab",
            StartUrl = "http://192.168.1.150:8080/cbt",
            AllowedDomains = new List<string> { "192.168.1.150" },
            AllowLocalIpHttp = true,
            EnforceHttps = true
        };

        // User or web redirect tanpa http://
        var result = _policy.EvaluateNavigation("192.168.1.150:8080/cbt/test", profile);
        Assert.True(result.IsAllowed);
        Assert.Equal(NavigationDecisionType.Allowed, result.Decision);
    }

    [Theory]
    [InlineData("example.com", "http://example.com")]
    [InlineData("https://cbt.muhipo.sch.id", "https://cbt.muhipo.sch.id")]
    [InlineData("about:blank", "about:blank")]
    [InlineData("data:text/html,<h1>Test</h1>", "data:text/html,<h1>Test</h1>")]
    public void NormalizeUrl_ShouldFormatCorrectly(string input, string expected)
    {
        string actual = WebView2BrowserService.NormalizeUrl(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BrowserService_ShouldExposeActiveEngineName()
    {
        var logger = new LoggingService();
        var navPolicy = new NavigationPolicy();
        var securityService = new SecurityPolicyService(navPolicy, logger);
        var browserService = new WebView2BrowserService(securityService, logger);

        string engineName = browserService.GetActiveEngineName();
        Assert.False(string.IsNullOrWhiteSpace(engineName));
    }
}
