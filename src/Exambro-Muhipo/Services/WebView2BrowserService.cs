using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using ExambroMuhipo.Models;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi layanan engine browser dengan dukungan arsitektur Dual-Engine:
/// 1. Microsoft Edge WebView2 (Modern Chromium) saat runtime terpasang di Windows 10/11 atau Win 7 dengan Edge.
/// 2. Windows Native WebBrowser Control (MSHTML IE11 Emulation) sebagai fallback otomatis 0-dependency untuk Windows 7/8/10 tanpa WebView2.
/// </summary>
public class WebView2BrowserService : IBrowserService
{
    private readonly ISecurityPolicyService _securityPolicy;
    private readonly ILoggingService _logger;
    private WebView2? _webView;
    private WebBrowser? _nativeBrowser;
    private ExamProfile? _currentProfile;
    private readonly string _userDataFolder;
    private string? _pendingNavigationUrl;
    private bool _isUsingNativeFallback;
    private double _fallbackZoomFactor = 1.0;

    public event Action<string, bool, string?>? NavigationStatusChanged;
    public event Action<NavigationCheckResult>? NavigationBlocked;
    public event Action<string>? BrowserProcessFailed;
    public event Action<bool>? IsLoadingChanged;

    public bool IsUsingNativeFallback => _isUsingNativeFallback;

    public WebView2BrowserService(ISecurityPolicyService securityPolicy, ILoggingService logger)
    {
        _securityPolicy = securityPolicy;
        _logger = logger;

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _userDataFolder = Path.Combine(localAppData, "Exambro-Muhipo", "WebView2Data");
    }

    /// <summary>
    /// Mendeteksi folder runtime browser Chromium yang tersedia di sistem:
    /// Prioritas 1: Microsoft Edge WebView2 Runtime / Microsoft Edge
    /// Prioritas 2: Google Chrome (Fallback Modern untuk Windows 7/10/11 jika Edge tidak ada)
    /// </summary>
    public static (string? folder, string browserType) FindAvailableChromiumBrowser()
    {
        // 1. Prioritas 1: Microsoft Edge & WebView2 Runtime
        string[] edgeBases = [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "EdgeWebView", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "EdgeWebView", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "EdgeWebView", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge Beta", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge Dev", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge SxS", "Application")
        ];

        foreach (var basePath in edgeBases)
        {
            var match = ProbeBrowserDirectory(basePath);
            if (match != null)
            {
                return (match, "Microsoft Edge / WebView2");
            }
        }

        // 2. Prioritas 2: Google Chrome (Jika Edge/WebView2 tidak terpasang di Windows 7 / 10 / 11)
        string[] chromeBases = [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome Beta", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome Beta", "Application"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome SxS", "Application")
        ];

        foreach (var basePath in chromeBases)
        {
            var match = ProbeBrowserDirectory(basePath);
            if (match != null)
            {
                return (match, "Google Chrome");
            }
        }

        // 3. Cek Default System Registry WebView2 Environment
        try
        {
            string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            if (!string.IsNullOrEmpty(version))
            {
                return (null, "Microsoft Edge (Sistem)");
            }
        }
        catch
        {
            // Tidak ada di registry sistem
        }

        return (null, "None");
    }

    private static string? ProbeBrowserDirectory(string basePath)
    {
        if (!Directory.Exists(basePath))
            return null;

        try
        {
            var subDirs = Directory.GetDirectories(basePath);
            foreach (var dir in subDirs)
            {
                string folderName = Path.GetFileName(dir);
                if (Version.TryParse(folderName, out _))
                {
                    try
                    {
                        string version = CoreWebView2Environment.GetAvailableBrowserVersionString(dir);
                        if (!string.IsNullOrEmpty(version))
                        {
                            return dir;
                        }
                    }
                    catch
                    {
                        // Lanjutkan
                    }
                }
            }

            try
            {
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString(basePath);
                if (!string.IsNullOrEmpty(version))
                {
                    return basePath;
                }
            }
            catch
            {
                // Lanjutkan
            }
        }
        catch
        {
            // Abaikan kesalahan IO
        }

        return null;
    }

    public static string? GetWindowsBuiltInBrowserFolder()
    {
        var (folder, _) = FindAvailableChromiumBrowser();
        return folder;
    }

    public bool IsRuntimeAvailable()
    {
        try
        {
            var (folder, type) = FindAvailableChromiumBrowser();
            if (type != "None")
            {
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString(folder);
                return !string.IsNullOrEmpty(version);
            }
            return false;
        }
        catch
        {
            try
            {
                string fallbackVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return !string.IsNullOrEmpty(fallbackVersion);
            }
            catch
            {
                return false;
            }
        }
    }

    public string GetRuntimeVersion()
    {
        try
        {
            var (folder, _) = FindAvailableChromiumBrowser();
            return CoreWebView2Environment.GetAvailableBrowserVersionString(folder);
        }
        catch
        {
            try
            {
                return CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch
            {
                return "Tidak Terdeteksi (Mode Native Fallback Aktif)";
            }
        }
    }

    public string GetActiveEngineName()
    {
        if (_isUsingNativeFallback)
        {
            return "Web Bawaan Windows (IE11 Emulation Mode)";
        }

        var (_, browserType) = FindAvailableChromiumBrowser();
        return browserType switch
        {
            "Google Chrome" => "Google Chrome (Chromium Engine)",
            _ => "Microsoft Edge (WebView2 Chromium)"
        };
    }

    public async Task InitializeAsync(WebView2? webView, WebBrowser? nativeBrowser, ExamProfile profile)
    {
        _webView = webView;
        _nativeBrowser = nativeBrowser;
        _currentProfile = profile ?? throw new ArgumentNullException(nameof(profile));

        bool canUseWebView2 = _webView != null && IsRuntimeAvailable();

        if (canUseWebView2)
        {
            try
            {
                await InitializeWebView2Async(profile);
                _isUsingNativeFallback = false;
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(AuditEventType.ApplicationError, "Inisialisasi WebView2 gagal, beralih ke Fallback Windows Native WebBrowser.", ex.Message);
            }
        }

        // Fallback ke Windows Native WebBrowser (0-dependency bawaan Windows 7/8/10/11)
        InitializeNativeWebBrowser(profile);
    }

    private async Task InitializeWebView2Async(ExamProfile profile)
    {
        if (_webView == null) throw new InvalidOperationException("WebView2 control is null.");

        if (!Directory.Exists(_userDataFolder))
        {
            Directory.CreateDirectory(_userDataFolder);
        }

        string? browserExecutable = GetWindowsBuiltInBrowserFolder();

        string browserArgs = string.Join(" ", new[]
        {
            "--allow-running-insecure-content",
            "--ignore-certificate-errors",
            "--disable-features=BlockInsecurePrivateNetworkRequests",
            "--enable-gpu-rasterization",
            "--enable-zero-copy",
            "--disable-gpu-watchdog",
            "--disable-features=Translate,OptimizationHints,MediaRouter,DialMediaRouteProvider,CalculateNativeWinOcclusion,EdgeSidebar,EdgeCollections,EdgeSearch",
            "--disable-background-networking",
            "--disable-component-update",
            "--disable-sync",
            "--disable-default-apps",
            "--disable-extensions",
            "--no-first-run",
            "--no-default-browser-check",
            "--autoplay-policy=no-user-gesture-required",
            "--disable-blink-features=AutomationControlled"
        });

        var envOptions = new CoreWebView2EnvironmentOptions
        {
            Language = "id-ID",
            AdditionalBrowserArguments = browserArgs
        };

        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: browserExecutable,
            userDataFolder: _userDataFolder,
            options: envOptions);

        await _webView.EnsureCoreWebView2Async(environment);
        _webView.DefaultBackgroundColor = System.Drawing.Color.White;

        ConfigureBrowserSettings();
        AttachEventHandlers();

        _logger.LogInfo(AuditEventType.ApplicationStarted, "Browser Microsoft Edge WebView2 berhasil diinisialisasi.", $"Versi: {GetRuntimeVersion()}");

        string targetUrl = !string.IsNullOrWhiteSpace(_pendingNavigationUrl)
            ? _pendingNavigationUrl
            : profile.StartUrl;

        _pendingNavigationUrl = null;

        if (!string.IsNullOrWhiteSpace(targetUrl))
        {
            Navigate(targetUrl);
        }
    }

    private void InitializeNativeWebBrowser(ExamProfile profile)
    {
        _isUsingNativeFallback = true;
        if (_nativeBrowser == null) return;

        EnsureBrowserEmulation();
        SetWebBrowserSilent(_nativeBrowser, true);

        _nativeBrowser.Navigating += OnNativeNavigating;
        _nativeBrowser.Navigated += OnNativeNavigated;
        _nativeBrowser.LoadCompleted += OnNativeLoadCompleted;

        _logger.LogInfo(AuditEventType.ApplicationStarted, "Engine Windows Native WebBrowser aktif sebagai fallback bebas-dependensi (Zero-dependency untuk Windows 7/8/10).");

        string targetUrl = !string.IsNullOrWhiteSpace(_pendingNavigationUrl)
            ? _pendingNavigationUrl
            : profile.StartUrl;

        _pendingNavigationUrl = null;

        if (!string.IsNullOrWhiteSpace(targetUrl))
        {
            Navigate(targetUrl);
        }
    }

    public static void EnsureBrowserEmulation()
    {
        try
        {
            string processName = Path.GetFileName(Environment.ProcessPath ?? "Exambro-Muhipo.exe");
            string[] features = [
                "FEATURE_BROWSER_EMULATION",
                "FEATURE_GPU_RENDERING",
                "FEATURE_NATIVE_XMLHTTP",
                "FEATURE_AJAX_CONNECTIONPOLICY",
                "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION",
                "FEATURE_MANAGE_SCRIPT_CIRCULAR_REFS"
            ];

            foreach (var feature in features)
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                        $@"Software\Microsoft\Internet Explorer\Main\FeatureControl\{feature}");
                    int val = feature == "FEATURE_BROWSER_EMULATION" ? 11001 : 1;
                    key?.SetValue(processName, val, Microsoft.Win32.RegistryValueKind.DWord);
                }
                catch
                {
                    // Ignored on limited key
                }
            }
        }
        catch
        {
            // Ignored on limited privilege fallback
        }
    }

    private static void SetWebBrowserSilent(WebBrowser browser, bool silent)
    {
        if (browser == null) return;
        try
        {
            FieldInfo? field = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                dynamic? ax = field.GetValue(browser);
                if (ax != null)
                {
                    ax.Silent = silent;
                }
            }
        }
        catch
        {
            // Fallback
        }
    }

    private void OnNativeNavigating(object? sender, NavigatingCancelEventArgs e)
    {
        if (e.Uri == null) return;
        string url = e.Uri.ToString();

        if (_currentProfile != null)
        {
            var check = _securityPolicy.CheckNavigation(url, _currentProfile);
            if (!check.IsAllowed)
            {
                e.Cancel = true;
                _logger.LogWarning(AuditEventType.NavigationBlocked, $"Navigasi diblokir (Native Browser): {check.Reason}", url);
                NavigationBlocked?.Invoke(check);
                return;
            }
        }

        NavigationStatusChanged?.Invoke(url, false, null);
        IsLoadingChanged?.Invoke(true);
    }

    private void OnNativeNavigated(object? sender, NavigationEventArgs e)
    {
        if (_nativeBrowser != null)
        {
            SetWebBrowserSilent(_nativeBrowser, true);
        }
        NavigationStatusChanged?.Invoke(e.Uri?.ToString() ?? "", true, null);
    }

    private void OnNativeLoadCompleted(object? sender, NavigationEventArgs e)
    {
        IsLoadingChanged?.Invoke(false);
        if (_nativeBrowser != null)
        {
            SetWebBrowserSilent(_nativeBrowser, true);
            ApplyNativeBrowserZoom();
        }
    }

    private void ConfigureBrowserSettings()
    {
        if (_webView?.CoreWebView2 == null || _currentProfile == null)
            return;

        var settings = _webView.CoreWebView2.Settings;

        settings.AreDevToolsEnabled = !_currentProfile.DisableDevTools;
        settings.AreDefaultContextMenusEnabled = !_currentProfile.DisableContextMenu;
        settings.IsZoomControlEnabled = _currentProfile.AllowZoom;
        settings.IsPinchZoomEnabled = _currentProfile.AllowZoom;
        settings.IsStatusBarEnabled = false;
        settings.IsSwipeNavigationEnabled = false;
        settings.AreHostObjectsAllowed = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsBuiltInErrorPageEnabled = true;
    }

    private void AttachEventHandlers()
    {
        if (_webView?.CoreWebView2 == null)
            return;

        _webView.NavigationStarting += OnNavigationStarting;
        _webView.NavigationCompleted += OnNavigationCompleted;
        _webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
        _webView.CoreWebView2.DownloadStarting += OnDownloadStarting;
        _webView.CoreWebView2.PermissionRequested += OnPermissionRequested;
        _webView.CoreWebView2.ProcessFailed += OnProcessFailed;
        _webView.CoreWebView2.ServerCertificateErrorDetected += OnServerCertificateErrorDetected;
    }

    private void OnServerCertificateErrorDetected(object? sender, CoreWebView2ServerCertificateErrorDetectedEventArgs e)
    {
        _logger.LogInfo(AuditEventType.NavigationAllowed, $"Mendeteksi sertifikat server SSL pada: {e.RequestUri}", $"Status: {e.ErrorStatus}");
        // Selalu izinkan sertifikat SSL self-signed pada koneksi server lokal lab
        e.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        string targetUrl = e.Uri;

        if (_currentProfile != null)
        {
            var check = _securityPolicy.CheckNavigation(targetUrl, _currentProfile);
            if (!check.IsAllowed)
            {
                e.Cancel = true;
                _logger.LogWarning(AuditEventType.NavigationBlocked, $"Navigasi diblokir: {check.Reason}", targetUrl);
                NavigationBlocked?.Invoke(check);
                return;
            }
        }

        NavigationStatusChanged?.Invoke(targetUrl, false, null);
        IsLoadingChanged?.Invoke(true);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        IsLoadingChanged?.Invoke(false);
        string currentUrl = _webView?.Source?.ToString() ?? string.Empty;

        if (e.IsSuccess)
        {
            NavigationStatusChanged?.Invoke(currentUrl, true, null);
        }
        else
        {
            string errorDetail = $"WebErrorStatus: {e.WebErrorStatus}, HttpStatusCode: {e.HttpStatusCode}";
            _logger.LogError(AuditEventType.ApplicationError, $"Gagal memuat URL: {currentUrl}", errorDetail);
            NavigationStatusChanged?.Invoke(currentUrl, false, errorDetail);
        }
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        string targetUrl = e.Uri;

        if (_currentProfile == null || !_securityPolicy.CanOpenNewWindow(_currentProfile, targetUrl))
        {
            e.Handled = true;
            if (!string.IsNullOrWhiteSpace(targetUrl))
            {
                var check = _securityPolicy.CheckNavigation(targetUrl, _currentProfile ?? new ExamProfile());
                if (check.IsAllowed)
                {
                    Navigate(targetUrl);
                }
                else
                {
                    NavigationBlocked?.Invoke(check);
                }
            }
        }
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        if (_currentProfile == null)
        {
            e.Cancel = true;
            return;
        }

        if (!_securityPolicy.CanDownload(_currentProfile, e.DownloadOperation.Uri))
        {
            e.Cancel = true;
        }
    }

    private void OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        if (_currentProfile == null)
        {
            e.State = CoreWebView2PermissionState.Deny;
            return;
        }

        bool allowed = _securityPolicy.CanGrantPermission(_currentProfile, e.PermissionKind.ToString());
        e.State = allowed ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        string detail = $"ProcessFailedKind: {e.ProcessFailedKind}, Reason: {e.Reason}, ExitCode: {e.ExitCode}";
        _logger.LogError(AuditEventType.WebViewCrashed, "WebView2 mengalami process failure atau crash.", detail);
        BrowserProcessFailed?.Invoke(detail);
    }

    public void Navigate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        string normalizedUrl = NormalizeUrl(url);

        if (!_isUsingNativeFallback && _webView?.CoreWebView2 != null)
        {
            _webView.CoreWebView2.Navigate(normalizedUrl);
        }
        else if (_isUsingNativeFallback && _nativeBrowser != null)
        {
            try
            {
                _nativeBrowser.Navigate(new Uri(normalizedUrl));
            }
            catch (Exception ex)
            {
                _logger.LogError(AuditEventType.ApplicationError, $"Gagal navigasi Native WebBrowser ke {normalizedUrl}", exception: ex);
            }
        }
        else
        {
            _pendingNavigationUrl = normalizedUrl;
        }
    }

    public static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        string trimmed = url.Trim();
        if (trimmed.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (!trimmed.Contains("://"))
        {
            return "http://" + trimmed;
        }

        return trimmed;
    }

    public void Reload()
    {
        if (!_isUsingNativeFallback && _webView?.CoreWebView2 != null)
        {
            _webView.CoreWebView2.Reload();
        }
        else if (_isUsingNativeFallback && _nativeBrowser != null)
        {
            try
            {
                _nativeBrowser.Refresh();
            }
            catch
            {
                if (_currentProfile != null && !string.IsNullOrWhiteSpace(_currentProfile.StartUrl))
                {
                    Navigate(_currentProfile.StartUrl);
                }
            }
        }
        else if (_currentProfile != null && !string.IsNullOrWhiteSpace(_currentProfile.StartUrl))
        {
            Navigate(_currentProfile.StartUrl);
        }
    }

    public void SetZoomFactor(double zoomFactor)
    {
        if (zoomFactor < 0.5 || zoomFactor > 3.0) return;

        if (!_isUsingNativeFallback && _webView != null)
        {
            _webView.ZoomFactor = zoomFactor;
        }
        else if (_isUsingNativeFallback)
        {
            _fallbackZoomFactor = zoomFactor;
            ApplyNativeBrowserZoom();
        }
    }

    private void ApplyNativeBrowserZoom()
    {
        if (_nativeBrowser?.Document != null)
        {
            try
            {
                dynamic doc = _nativeBrowser.Document;
                if (doc?.body != null)
                {
                    doc.body.style.zoom = $"{_fallbackZoomFactor:0.##}";
                }
            }
            catch
            {
                // Ignored on zoom fallback
            }
        }
    }

    public double GetZoomFactor()
    {
        return !_isUsingNativeFallback && _webView != null
            ? _webView.ZoomFactor
            : _fallbackZoomFactor;
    }

    public async Task ClearBrowsingDataAsync()
    {
        if (!_isUsingNativeFallback && _webView?.CoreWebView2?.Profile != null)
        {
            try
            {
                var kinds = CoreWebView2BrowsingDataKinds.CacheStorage |
                            CoreWebView2BrowsingDataKinds.DiskCache;

                if (_currentProfile?.ClearCookiesOnFinish ?? false)
                {
                    kinds |= CoreWebView2BrowsingDataKinds.Cookies;
                }

                await _webView.CoreWebView2.Profile.ClearBrowsingDataAsync(kinds);
                _logger.LogInfo(AuditEventType.ExamCompleted, "Data cache/browsing sesi ujian WebView2 dibersihkan.");
            }
            catch (Exception ex)
            {
                _logger.LogError(AuditEventType.ApplicationError, "Gagal membersihkan data browsing WebView2.", exception: ex);
            }
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    public void Shutdown()
    {
        if (_webView?.CoreWebView2 != null)
        {
            _webView.NavigationStarting -= OnNavigationStarting;
            _webView.NavigationCompleted -= OnNavigationCompleted;
            _webView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
            _webView.CoreWebView2.DownloadStarting -= OnDownloadStarting;
            _webView.CoreWebView2.PermissionRequested -= OnPermissionRequested;
            _webView.CoreWebView2.ProcessFailed -= OnProcessFailed;
            _webView.CoreWebView2.ServerCertificateErrorDetected -= OnServerCertificateErrorDetected;
        }

        if (_nativeBrowser != null)
        {
            _nativeBrowser.Navigating -= OnNativeNavigating;
            _nativeBrowser.Navigated -= OnNativeNavigated;
            _nativeBrowser.LoadCompleted -= OnNativeLoadCompleted;
        }

        _webView = null;
        _nativeBrowser = null;
        _currentProfile = null;
    }
}
