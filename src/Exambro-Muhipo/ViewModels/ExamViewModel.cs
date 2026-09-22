using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.ViewModels;

/// <summary>
/// ViewModel untuk layar pelaksanaan ujian aktif (ExamView).
/// Mengelola lockdown browser, penghitung waktu mundur, banner peringatan navigasi diblokir,
/// dan alur autentikasi keluar sesi (Exit Mechanism).
/// </summary>
public class ExamViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly IBrowserService _browserService;
    private readonly IExamSessionService _sessionService;
    private readonly IKioskService _kioskService;
    private readonly IApplicationStateManager _stateManager;
    private readonly Func<bool> _showAdminExitDialog;

    public string SchoolName => "SMA Muhammadiyah 1 Ponorogo";
    public string ExamTitle => _sessionService.CurrentProfile?.ExamName ?? _configService.ActiveProfile.ExamName;

    private string _formattedTimeRemaining = "00:00:00";
    public string FormattedTimeRemaining
    {
        get => _formattedTimeRemaining;
        set => SetField(ref _formattedTimeRemaining, value);
    }

    private bool _hasTimeLimit;
    public bool HasTimeLimit
    {
        get => _hasTimeLimit;
        set => SetField(ref _hasTimeLimit, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }

    private bool _isBlockedBannerVisible;
    public bool IsBlockedBannerVisible
    {
        get => _isBlockedBannerVisible;
        set => SetField(ref _isBlockedBannerVisible, value);
    }

    private string _blockedMessage = string.Empty;
    public string BlockedMessage
    {
        get => _blockedMessage;
        set => SetField(ref _blockedMessage, value);
    }

    private string _browserStatusText = "Memuat Ujian...";
    public string BrowserStatusText
    {
        get => _browserStatusText;
        set => SetField(ref _browserStatusText, value);
    }

    private string _currentClockTime = DateTime.Now.ToString("HH:mm:ss");
    public string CurrentClockTime
    {
        get => _currentClockTime;
        set => SetField(ref _currentClockTime, value);
    }

    private double _zoomFactor = 1.0;
    public string ZoomPercentText => $"{(int)(_zoomFactor * 100)}%";

    public ICommand ExitExamCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand DismissBlockedBannerCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ResetZoomCommand { get; }

    private readonly System.Windows.Threading.DispatcherTimer _clockTimer;

    public ExamViewModel(
        IConfigurationService configService,
        IBrowserService browserService,
        IExamSessionService sessionService,
        IKioskService kioskService,
        IApplicationStateManager stateManager,
        Func<bool> showAdminExitDialog)
    {
        _configService = configService;
        _browserService = browserService;
        _sessionService = sessionService;
        _kioskService = kioskService;
        _stateManager = stateManager;
        _showAdminExitDialog = showAdminExitDialog;

        ExitExamCommand = new AsyncRelayCommand(OnRequestExitExamAsync);
        ReloadCommand = new RelayCommand(OnReload);
        DismissBlockedBannerCommand = new RelayCommand(() => IsBlockedBannerVisible = false);

        ZoomInCommand = new RelayCommand(() => AdjustZoom(0.1));
        ZoomOutCommand = new RelayCommand(() => AdjustZoom(-0.1));
        ResetZoomCommand = new RelayCommand(() => SetZoom(1.0));

        _clockTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => CurrentClockTime = DateTime.Now.ToString("HH:mm:ss");
        _clockTimer.Start();

        _sessionService.TimeRemainingUpdated += OnTimeRemainingUpdated;
        _sessionService.SessionTimedOut += OnSessionTimedOut;
        _browserService.NavigationBlocked += OnNavigationBlocked;
        _browserService.NavigationStatusChanged += OnNavigationStatusChanged;
        _browserService.IsLoadingChanged += (loading) => IsLoading = loading;
        _kioskService.AltF4Pressed += OnAltF4Pressed;
    }

    private void AdjustZoom(double delta)
    {
        double newZoom = Math.Clamp(_zoomFactor + delta, 0.5, 2.0);
        SetZoom(newZoom);
    }

    private void SetZoom(double zoom)
    {
        _zoomFactor = zoom;
        _browserService.SetZoomFactor(_zoomFactor);
        OnPropertyChanged(nameof(ZoomPercentText));
    }

    public async Task StartExamSessionAsync(Window mainWindow)
    {
        var profile = _configService.ActiveProfile;
        HasTimeLimit = false; // Batas waktu dikelola langsung oleh web server CBT sekolah

        // 1. Mulai session service
        await _sessionService.StartSessionAsync(profile);

        // 2. Aktifkan Kiosk & Fullscreen
        _kioskService.EnableKiosk(mainWindow, profile);

        // 3. Arahkan browser ke StartUrl
        _browserService.Navigate(profile.StartUrl);
    }

    private void OnTimeRemainingUpdated(TimeSpan remaining)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            FormattedTimeRemaining = remaining.ToString(@"hh\:mm\:ss");
        });
    }

    private void OnSessionTimedOut()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            BlockedMessage = "WAKTU UJIAN TELAH HABIS. Hubungi pengawas ruang untuk verifikasi penyelesaian ujian.";
            IsBlockedBannerVisible = true;
        });
    }

    private void OnNavigationBlocked(NavigationCheckResult result)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            BlockedMessage = $"AKSES DITOLAK: {result.Reason}. Upaya navigasi ke luar sistem ujian tidak diizinkan.";
            IsBlockedBannerVisible = true;
        });
    }

    private void OnNavigationStatusChanged(string status, bool isSuccess, string? error)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            BrowserStatusText = isSuccess ? "Halaman Siap" : "Menghubungi Server...";
            if (!isSuccess && !string.IsNullOrEmpty(error) && error != "OperationCanceled")
            {
                BlockedMessage = "KENDALA KONEKSI: Tidak dapat menghubungi server ujian CBT sekolah. Pastikan kabel LAN terpasang atau segera hubungi pengawas ruang.";
                IsBlockedBannerVisible = true;
            }
        });
    }

    private void OnReload()
    {
        _browserService.Reload();
    }

    private bool _isHandlingExit;

    private void OnAltF4Pressed()
    {
        _ = OnRequestExitExamAsync();
    }

    public async Task OnRequestExitExamAsync()
    {
        if (_isHandlingExit)
            return;

        _isHandlingExit = true;
        try
        {
            // Panggil dialog otentikasi pengawas/admin
            bool authorized = _showAdminExitDialog();
            if (authorized)
            {
                await ExitApplicationAsync();
            }
            else
            {
                BlockedMessage = "PERINGATAN: Autentikasi pengawas gagal. Upaya keluar tidak sah telah dicatat di log audit.";
                IsBlockedBannerVisible = true;
            }
        }
        finally
        {
            _isHandlingExit = false;
        }
    }

    public async Task ExitApplicationAsync()
    {
        // 1. Bersihkan browsing data sesuai profil
        if (_configService.ActiveProfile.ClearCacheOnFinish || _configService.ActiveProfile.ClearCookiesOnFinish)
        {
            await _browserService.ClearBrowsingDataAsync();
        }

        // 2. Nonaktifkan Kiosk Mode
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            _kioskService.DisableKiosk(mainWindow);
        }

        // 3. Selesaikan sesi
        await _sessionService.ForceEndSessionAsync();

        // 4. Tutup aplikasi secara resmi
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            if (Application.Current?.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.IsExamModeActive = false;
            }
            Application.Current?.Shutdown();
        });
    }

    public async Task EndSessionAndReturnAsync()
    {
        // 1. Bersihkan browsing data sesuai profil
        if (_configService.ActiveProfile.ClearCacheOnFinish || _configService.ActiveProfile.ClearCookiesOnFinish)
        {
            await _browserService.ClearBrowsingDataAsync();
        }

        // 2. Nonaktifkan Kiosk Mode
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            _kioskService.DisableKiosk(mainWindow);
        }

        // 3. Selesaikan sesi
        await _sessionService.ForceEndSessionAsync();

        // 4. Kembali ke Home View
        _stateManager.NavigateToHome();
    }

    public void Cleanup()
    {
        _clockTimer.Stop();
        _kioskService.AltF4Pressed -= OnAltF4Pressed;
        _sessionService.TimeRemainingUpdated -= OnTimeRemainingUpdated;
        _sessionService.SessionTimedOut -= OnSessionTimedOut;
        _browserService.NavigationBlocked -= OnNavigationBlocked;
        _browserService.NavigationStatusChanged -= OnNavigationStatusChanged;
    }
}
