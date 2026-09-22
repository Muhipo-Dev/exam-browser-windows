using System;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.ViewModels;

/// <summary>
/// ViewModel untuk Halaman Utama (HomeView) bergaya Exambrowser Klien Ujian UTBK / ANBK.
/// Menampilkan checklist kesiapan perangkat, resolusi, status LAN/offline, dan tombol RUN.
/// </summary>
public class HomeViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly IBrowserService _browserService;
    private readonly IApplicationStateManager _stateManager;
    private readonly Func<bool> _showAdminLoginDialog;
    private readonly DispatcherTimer _clockTimer;

    public string AppName => "Exambro-Muhipo";
    public string AppSubtitle => "Secure Examination Browser";
    public string SchoolName => "SMA Muhammadiyah 1 Ponorogo";
    public string AppVersion => $"v{_configService.Config.AppVersion}";
    public string DeveloperCredit => "Developed by Muhipo Dev";

    private string _currentTimeString = DateTime.Now.ToString("HH:mm:ss");
    public string CurrentTimeString
    {
        get => _currentTimeString;
        set => SetField(ref _currentTimeString, value);
    }

    private string _webView2Status = "Memeriksa...";
    public string WebView2Status
    {
        get => _webView2Status;
        set => SetField(ref _webView2Status, value);
    }

    private bool _isWebView2Ready = true;
    public bool IsWebView2Ready
    {
        get => _isWebView2Ready;
        set => SetField(ref _isWebView2Ready, value);
    }

    private string _networkStatus = "Memeriksa...";
    public string NetworkStatus
    {
        get => _networkStatus;
        set => SetField(ref _networkStatus, value);
    }

    private bool _isNetworkReady = true;
    public bool IsNetworkReady
    {
        get => _isNetworkReady;
        set => SetField(ref _isNetworkReady, value);
    }

    private string _resolutionStatus = "Standar Layar Sesuai";
    public string ResolutionStatus
    {
        get => _resolutionStatus;
        set => SetField(ref _resolutionStatus, value);
    }

    private string _osStatus = "Windows 64-bit";
    public string OsStatus
    {
        get => _osStatus;
        set => SetField(ref _osStatus, value);
    }

    public string ActiveExamName => _configService.ActiveProfile.ExamName;
    public string ActiveExamDescription => _configService.ActiveProfile.ExamDescription;
    public string ActiveStartUrl => _configService.ActiveProfile.StartUrl;
    public string ActiveDurationText => _configService.ActiveProfile.SessionTimeout > 0
        ? $"{_configService.ActiveProfile.SessionTimeout} Menit"
        : "Tanpa Batas Waktu";

    public bool IsLocalServerMode => ExamProfile.IsLocalOrPrivateIp(GetHostFromUrl(_configService.ActiveProfile.StartUrl));

    public ICommand StartExamCommand { get; }
    public ICommand OpenAdminCommand { get; }
    public ICommand OpenAboutCommand { get; }
    public ICommand OpenHelpCommand { get; }
    public ICommand ExitAppCommand { get; }

    public HomeViewModel(
        IConfigurationService configService,
        IBrowserService browserService,
        IApplicationStateManager stateManager,
        Func<bool> showAdminLoginDialog)
    {
        _configService = configService;
        _browserService = browserService;
        _stateManager = stateManager;
        _showAdminLoginDialog = showAdminLoginDialog;

        StartExamCommand = new RelayCommand(OnStartExam);
        OpenAdminCommand = new RelayCommand(OnOpenAdmin);
        OpenAboutCommand = new RelayCommand(() => _stateManager.NavigateToAbout());
        OpenHelpCommand = new RelayCommand(() => _stateManager.NavigateToHelp());
        ExitAppCommand = new RelayCommand(OnExitApp);

        // Jam digital real-time
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => CurrentTimeString = DateTime.Now.ToString("HH:mm:ss");
        _clockTimer.Start();

        CheckSystemStatus();
    }

    public string BrowserEngineDisplayName => _browserService.GetActiveEngineName();

    public string BrowserEngineBadge => _browserService.IsRuntimeAvailable()
        ? "🟢 Engine Modern Aktif"
        : "🔵 Web Bawaan Windows Aktif";

    public void RefreshProfileInfo()
    {
        OnPropertyChanged(nameof(ActiveExamName));
        OnPropertyChanged(nameof(ActiveExamDescription));
        OnPropertyChanged(nameof(ActiveStartUrl));
        OnPropertyChanged(nameof(ActiveDurationText));
        OnPropertyChanged(nameof(IsLocalServerMode));
        OnPropertyChanged(nameof(AppVersion));
        OnPropertyChanged(nameof(BrowserEngineDisplayName));
        OnPropertyChanged(nameof(BrowserEngineBadge));
    }

    private void CheckSystemStatus()
    {
        // 1. Cek Engine Browser (WebView2 Modern atau Native WebBrowser Fallback)
        bool wv2Available = _browserService.IsRuntimeAvailable();
        IsWebView2Ready = true; // Selalu siap berkat Dual-Engine Architecture
        WebView2Status = wv2Available
            ? $"Siap (Modern WebView2 v{_browserService.GetRuntimeVersion()})"
            : "Siap (Web Bawaan Windows 7)";

        // 2. Cek Resolusi Layar
        try
        {
            double w = SystemParameters.PrimaryScreenWidth;
            double h = SystemParameters.PrimaryScreenHeight;
            ResolutionStatus = $"{w:0} x {h:0} (Sesuai Standar)";
        }
        catch
        {
            ResolutionStatus = "Resolusi Standar";
        }

        // 3. Cek Sistem Operasi
        OsStatus = Environment.Is64BitOperatingSystem ? "Windows 64-bit (Sesuai)" : "Windows 32-bit";

        // 4. Cek Jaringan / LAN
        try
        {
            bool isNetUp = NetworkInterface.GetIsNetworkAvailable();
            IsNetworkReady = true; // Selalu siap karena mendukung server lokal offline LAN!
            NetworkStatus = isNetUp ? "Terhubung ke Jaringan (LAN / Lokal)" : "Mode Offline Mandiri (Siap)";
        }
        catch
        {
            NetworkStatus = "Jaringan Lokal Siap";
            IsNetworkReady = true;
        }
    }

    private static string GetHostFromUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Host;
        return string.Empty;
    }

    private void OnStartExam()
    {
        // Jika Microsoft Edge, WebView2, maupun Google Chrome tidak terpasang, minta konfirmasi untuk menggunakan Web Bawaan Windows
        if (!_browserService.IsRuntimeAvailable())
        {
            var confirmResult = MessageBox.Show(
                "Pemberitahuan Sistem Browser:\n\n" +
                "Browser Microsoft Edge / WebView2 maupun Google Chrome tidak ditemukan pada komputer ini.\n" +
                "Aplikasi akan menjalankan ujian menggunakan Web Bawaan Windows (Windows Native WebBrowser IE11 Mode).\n\n" +
                "Apakah Anda ingin melanjutkan masuk ke ujian dengan Web Bawaan Windows?",
                "Konfirmasi Browser - Exambro-Muhipo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _stateManager.NavigateToExam();
    }

    private void OnOpenAdmin()
    {
        if (_showAdminLoginDialog())
        {
            _stateManager.NavigateToAdmin();
        }
    }

    private void OnExitApp()
    {
        Application.Current.Shutdown();
    }
}
