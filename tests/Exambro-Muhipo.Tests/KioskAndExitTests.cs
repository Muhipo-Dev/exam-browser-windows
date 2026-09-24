using System;
using System.Threading.Tasks;
using System.Windows;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using ExambroMuhipo.ViewModels;
using Xunit;

namespace ExambroMuhipo.Tests;

public class MockKioskService : IKioskService
{
    public bool IsKioskActive { get; set; }
    public event Action? AltF4Pressed;

    public void EnableKiosk(Window window, ExamProfile profile) => IsKioskActive = true;
    public void DisableKiosk(Window window) => IsKioskActive = false;

    public void SimulateAltF4()
    {
        AltF4Pressed?.Invoke();
    }
}

public class KioskAndExitTests
{
    [Fact]
    public void KioskService_ShouldImplementIKioskServiceAndExposeAltF4Event()
    {
        var logger = new LoggingService();
        var kiosk = new KioskService(logger);

        Assert.IsAssignableFrom<IKioskService>(kiosk);
        Assert.False(kiosk.IsKioskActive);

        bool eventFired = false;
        kiosk.AltF4Pressed += () => eventFired = true;

        // Verify event registration works without throwing
        Assert.False(eventFired);
    }

    [Fact]
    public async Task AltF4_WhenAuthorized_ShouldTriggerExitFlow()
    {
        var logger = new LoggingService();
        var configService = new ConfigurationService(logger);
        var mockKiosk = new MockKioskService();
        var sessionService = new ExamSessionService(configService, logger);
        var navPolicy = new NavigationPolicy();
        var securityService = new SecurityPolicyService(navPolicy, logger);
        var browserService = new WebView2BrowserService(securityService, logger);
        var stateManager = new ApplicationStateManager(logger);

        bool exitDialogShown = false;

        var examVm = new ExamViewModel(
            configService,
            browserService,
            sessionService,
            mockKiosk,
            stateManager,
            () =>
            {
                exitDialogShown = true;
                return true; // authorized with MUHIPO23
            });

        // Trigger Alt+F4 simulation
        mockKiosk.SimulateAltF4();

        // Give async task a brief moment to execute
        await Task.Delay(50);

        Assert.True(exitDialogShown);
    }

    [Fact]
    public async Task AltF4_WhenDenied_ShouldSetWarningMessage()
    {
        var logger = new LoggingService();
        var configService = new ConfigurationService(logger);
        var mockKiosk = new MockKioskService();
        var sessionService = new ExamSessionService(configService, logger);
        var navPolicy = new NavigationPolicy();
        var securityService = new SecurityPolicyService(navPolicy, logger);
        var browserService = new WebView2BrowserService(securityService, logger);
        var stateManager = new ApplicationStateManager(logger);

        var examVm = new ExamViewModel(
            configService,
            browserService,
            sessionService,
            mockKiosk,
            stateManager,
            () => false); // Denied

        mockKiosk.SimulateAltF4();
        await Task.Delay(50);

        Assert.True(examVm.IsBlockedBannerVisible);
    }
}
