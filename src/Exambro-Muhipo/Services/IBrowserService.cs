using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using ExambroMuhipo.Models;
using Microsoft.Web.WebView2.Wpf;

namespace ExambroMuhipo.Services;

/// <summary>
/// Kontrak layanan engine browser dengan arsitektur Dual-Engine:
/// 1. Microsoft Edge WebView2 (Modern Chromium) jika terpasang.
/// 2. Windows Native WebBrowser Control (Zero-dependency fallback untuk Windows 7/8/10 tanpa WebView2).
/// </summary>
public interface IBrowserService
{
    event Action<string, bool, string?>? NavigationStatusChanged;
    event Action<NavigationCheckResult>? NavigationBlocked;
    event Action<string>? BrowserProcessFailed;
    event Action<bool>? IsLoadingChanged;

    bool IsRuntimeAvailable();
    string GetRuntimeVersion();
    string GetActiveEngineName();
    bool IsUsingNativeFallback { get; }

    Task InitializeAsync(WebView2? webView, WebBrowser? nativeBrowser, ExamProfile profile);
    void Navigate(string url);
    void Reload();
    void SetZoomFactor(double zoomFactor);
    double GetZoomFactor();
    Task ClearBrowsingDataAsync();
    void Shutdown();
}

