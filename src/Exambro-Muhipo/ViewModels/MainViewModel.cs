using System;
using System.Windows;
using System.Windows.Input;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.ViewModels;

/// <summary>
/// ViewModel Utama (Shell) yang mengontrol penukaran halaman/view aktif
/// dan status tampilan jendela utama (Windowed vs Borderless Kiosk).
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly ILoggingService _logger;
    private readonly IBrowserService _browserService;
    private readonly IExamSessionService _sessionService;
    private readonly IKioskService _kioskService;
    private readonly IProfileManager _profileManager;
    private readonly INavigationPolicy _navigationPolicy;
    private readonly IApplicationStateManager _stateManager;

    private ViewModelBase? _currentView;
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetField(ref _currentView, value);
    }

    private bool _isExamModeActive;
    public bool IsExamModeActive
    {
        get => _isExamModeActive;
        set => SetField(ref _isExamModeActive, value);
    }

    public string WindowTitle => IsExamModeActive
        ? "Exambro-Muhipo - Sesi Ujian Berlangsung"
        : "Exambro-Muhipo - Secure Examination Browser - SMA Muhammadiyah 1 Ponorogo";

    public HomeViewModel HomeVM { get; }
    public ExamViewModel ExamVM { get; }
    public AdminViewModel AdminVM { get; }
    public SettingsViewModel SettingsVM { get; }
    public AboutViewModel AboutVM { get; }
    public HelpViewModel HelpVM { get; }

    public ICommand NavigateHomeCommand { get; }
    public ICommand ExitAppCommand { get; }

    public MainViewModel(
        IConfigurationService configService,
        ILoggingService logger,
        IBrowserService browserService,
        IExamSessionService sessionService,
        IKioskService kioskService,
        IProfileManager profileManager,
        INavigationPolicy navigationPolicy,
        IApplicationStateManager stateManager,
        Func<bool> showAdminLoginDialog,
        Func<bool> showAdminExitDialog)
    {
        _configService = configService;
        _logger = logger;
        _browserService = browserService;
        _sessionService = sessionService;
        _kioskService = kioskService;
        _profileManager = profileManager;
        _navigationPolicy = navigationPolicy;
        _stateManager = stateManager;

        HomeVM = new HomeViewModel(_configService, _browserService, _stateManager, showAdminLoginDialog);
        ExamVM = new ExamViewModel(_configService, _browserService, _sessionService, _kioskService, _stateManager, showAdminExitDialog);
        AdminVM = new AdminViewModel(_configService, _profileManager, _logger, _stateManager, _navigationPolicy);
        SettingsVM = new SettingsViewModel(_configService, _stateManager);
        AboutVM = new AboutViewModel(_stateManager, _configService);
        HelpVM = new HelpViewModel(_stateManager);

        NavigateHomeCommand = new RelayCommand(() => _stateManager.NavigateToHome());
        ExitAppCommand = new RelayCommand(OnExitApp);

        _stateManager.ViewChanged += OnViewChanged;

        // View awal adalah Home
        CurrentView = HomeVM;
    }

    private void OnViewChanged(AppViewType viewType, ViewModelBase? customVM)
    {
        if (customVM != null)
        {
            CurrentView = customVM;
            IsExamModeActive = viewType == AppViewType.Exam;
            OnPropertyChanged(nameof(WindowTitle));
            return;
        }

        switch (viewType)
        {
            case AppViewType.Home:
                HomeVM.RefreshProfileInfo();
                CurrentView = HomeVM;
                IsExamModeActive = false;
                break;
            case AppViewType.Exam:
                CurrentView = ExamVM;
                IsExamModeActive = true;
                // Jalankan sesi ujian
                var window = Application.Current.MainWindow;
                if (window != null)
                {
                    _ = ExamVM.StartExamSessionAsync(window);
                }
                break;
            case AppViewType.Admin:
                AdminVM.RefreshProfileList();
                AdminVM.LoadAuditLogs();
                CurrentView = AdminVM;
                IsExamModeActive = false;
                break;
            case AppViewType.Settings:
                CurrentView = SettingsVM;
                IsExamModeActive = false;
                break;
            case AppViewType.About:
                CurrentView = AboutVM;
                IsExamModeActive = false;
                break;
            case AppViewType.Help:
                CurrentView = HelpVM;
                IsExamModeActive = false;
                break;
        }

        OnPropertyChanged(nameof(WindowTitle));
    }

    private void OnExitApp()
    {
        if (IsExamModeActive)
        {
            _logger.LogWarning(AuditEventType.ExitDenied, "Upaya penutupan aplikasi langsung saat ujian aktif ditolak.");
            return;
        }

        Application.Current.Shutdown();
    }
}
