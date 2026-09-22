using System.Collections.Generic;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using Xunit;

namespace ExambroMuhipo.Tests;

public class SecurityPolicyTests
{
    private readonly NavigationPolicy _navPolicy;
    private readonly SecurityPolicyService _securityService;
    private readonly LoggingService _logger;

    public SecurityPolicyTests()
    {
        _navPolicy = new NavigationPolicy();
        _logger = new LoggingService();
        _securityService = new SecurityPolicyService(_navPolicy, _logger);
    }

    [Theory]
    [InlineData("mailto:proctor@example.com")]
    [InlineData("tel:08123456789")]
    [InlineData("file:///C:/Windows/System32/cmd.exe")]
    [InlineData("ms-settings:privacy")]
    [InlineData("powershell:Start-Process calc")]
    [InlineData("cmd:/c dir")]
    [InlineData("javascript:alert('cheat')")]
    public void CheckNavigation_ShouldBlockDangerousExternalProtocols(string externalUrl)
    {
        var profile = new ExamProfile
        {
            AllowedDomains = new List<string> { "example.com" }
        };

        var result = _securityService.CheckNavigation(externalUrl, profile);

        Assert.False(result.IsAllowed);
        Assert.Equal(NavigationDecisionType.BlockedProtocol, result.Decision);
    }

    [Fact]
    public void CanOpenNewWindow_ShouldReturnFalse_WhenDisabledInProfile()
    {
        var profile = new ExamProfile
        {
            DisableNewWindows = true,
            AllowedDomains = new List<string> { "example.com" }
        };

        bool allowed = _securityService.CanOpenNewWindow(profile, "https://example.com/popup");
        Assert.False(allowed);
    }

    [Fact]
    public void CanShowPopup_ShouldReturnFalse_WhenDisabledInProfile()
    {
        var profile = new ExamProfile
        {
            DisablePopups = true,
            AllowedDomains = new List<string> { "example.com" }
        };

        bool allowed = _securityService.CanShowPopup(profile, "https://example.com/dialog");
        Assert.False(allowed);
    }

    [Fact]
    public void CanDownload_ShouldFollowProfileConfiguration()
    {
        var profileNoDownload = new ExamProfile { AllowDownloads = false };
        var profileWithDownload = new ExamProfile { AllowDownloads = true };

        Assert.False(_securityService.CanDownload(profileNoDownload, "https://example.com/file.pdf"));
        Assert.True(_securityService.CanDownload(profileWithDownload, "https://example.com/file.pdf"));
    }

    [Fact]
    public void CanGrantPermission_ShouldOnlyAllowConfiguredHardware()
    {
        var profile = new ExamProfile
        {
            AllowCamera = true,
            AllowMicrophone = false
        };

        Assert.True(_securityService.CanGrantPermission(profile, "camera"));
        Assert.False(_securityService.CanGrantPermission(profile, "microphone"));
        Assert.False(_securityService.CanGrantPermission(profile, "geolocation"));
    }
}
