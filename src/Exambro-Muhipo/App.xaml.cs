using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using ExambroMuhipo.ViewModels;
using ExambroMuhipo.Views;

namespace ExambroMuhipo;

/// <summary>
/// Logika interaksi untuk App.xaml dengan penanganan global exception,
/// inisialisasi arsitektur modular, dan deteksi pemulihan sesi (crash recovery).
/// </summary>
public partial class App : Application
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

    public ILoggingService LoggingService { get; private set; } = null!;
    public IAlarmService AlarmService { get; private set; } = null!;
    public IConfigurationService ConfigService { get; private set; } = null!;
    public IProfileManager ProfileManager { get; private set; } = null!;
    public INavigationPolicy NavigationPolicy { get; private set; } = null!;
    public ISecurityPolicyService SecurityPolicyService { get; private set; } = null!;
    public IKioskService KioskService { get; private set; } = null!;
    public IBrowserService BrowserService { get; private set; } = null!;
    public IExamSessionService SessionService { get; private set; } = null!;
    public IApplicationStateManager StateManager { get; private set; } = null!;

    public MainViewModel MainViewModel { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 0. Daftarkan AppUserModelID unik agar Taskbar Windows 10/11 langsung menampilkan icon SMA Muhipo terbaru
        try
        {
            SetCurrentProcessExplicitAppUserModelID("SMA.Muhipo.Exambro.CBT");
        }
        catch
        {
            // Ignored on legacy environments
        }

        // 1. Pasang Global Exception Handlers
        SetupGlobalExceptionHandling();

        // 2. Inisialisasi Services
        InitializeServices();

        // 2b. Cek CLI Arguments (--version, --check-config, --smoke-test)
        if (e.Args.Length > 0)
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            string arg = e.Args[0].ToLowerInvariant();

            if (arg == "--version" || arg == "-v")
            {
                Console.WriteLine($"Exambro-Muhipo v{ConfigService.Config.AppVersion}");
                Console.WriteLine("Secure Examination Browser - SMA Muhammadiyah 1 Ponorogo");
                Shutdown(0);
                return;
            }
            if (arg == "--check-config")
            {
                Console.WriteLine("STATUS: OK");
                Console.WriteLine($"Aplikasi: {ConfigService.Config.AppName} v{ConfigService.Config.AppVersion}");
                Console.WriteLine($"Institusi: {ConfigService.Config.SchoolName}");
                Console.WriteLine($"Profil Aktif: {ConfigService.ActiveProfile.ExamName} ({ConfigService.ActiveProfile.StartUrl})");
                Console.WriteLine($"WebView2 Engine: {BrowserService.GetRuntimeVersion()}");
                Shutdown(0);
                return;
            }
            if (arg == "--smoke-test")
            {
                Console.WriteLine("EXAMBRO_MUHIPO_SMOKE_TEST_PASSED");
                Shutdown(0);
                return;
            }
        }

        LoggingService.LogInfo(AuditEventType.ApplicationStarted, $"Exambro-Muhipo v{ConfigService.Config.AppVersion} berhasil dijalankan.", "SMA Muhammadiyah 1 Ponorogo");

        // 3. Bangun MainViewModel
        MainViewModel = new MainViewModel(
            ConfigService,
            LoggingService,
            BrowserService,
            SessionService,
            KioskService,
            ProfileManager,
            NavigationPolicy,
            StateManager,
            ShowAdminLoginDialog,
            ShowExitAuthorizationDialog);

        // 4. Buat MainWindow
        var mainWindow = new MainWindow
        {
            DataContext = MainViewModel
        };
        MainWindow = mainWindow;
        mainWindow.Show();

        // 5. Cek Crash Recovery / Unfinished Session
        CheckForCrashRecovery(mainWindow);
    }

    private void SetupGlobalExceptionHandling()
    {
        // UI Thread Exception
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Non-UI AppDomain Exception
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        // Background Task Exception
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LoggingService?.LogError(AuditEventType.ApplicationError, "Unhandled UI Dispatcher Exception", exception: e.Exception);
        MessageBox.Show(
            $"Terjadi kesalahan sistem yang tidak terduga:\n{e.Exception.Message}\n\nDetail telah dicatat ke berkas log audit.",
            "Kesalahan Aplikasi - Exambro-Muhipo",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LoggingService?.LogError(AuditEventType.ApplicationError, "Fatal AppDomain Unhandled Exception", exception: ex);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggingService?.LogError(AuditEventType.ApplicationError, "Unobserved Task Exception", exception: e.Exception);
        e.SetObserved();
    }

    private void InitializeServices()
    {
        LoggingService = new LoggingService();
        AlarmService = new AlarmService(LoggingService);
        ConfigService = new ConfigurationService(LoggingService);
        ConfigService.LoadConfiguration();

        ProfileManager = new ProfileManager(ConfigService, LoggingService);
        NavigationPolicy = new NavigationPolicy();
        SecurityPolicyService = new SecurityPolicyService(NavigationPolicy, LoggingService);
        KioskService = new KioskService(LoggingService);
        BrowserService = new WebView2BrowserService(SecurityPolicyService, LoggingService);
        SessionService = new ExamSessionService(ConfigService, LoggingService);
        StateManager = new ApplicationStateManager(LoggingService);
    }

    private bool ShowAdminLoginDialog()
    {
        var dlg = new AdminLoginDialog(
            ConfigService,
            "Autentikasi Administrator",
            "Masukkan password administrator untuk membuka Pengaturan Server.",
            AlarmService,
            ConfigService.VerifyAdminPassword);

        dlg.Owner = MainWindow;
        return dlg.ShowDialog() == true && dlg.IsAuthenticated;
    }

    private bool ShowExitAuthorizationDialog()
    {
        // Keluar aplikasi tidak memerlukan password
        return true;
    }

    private void CheckForCrashRecovery(Window mainWindow)
    {
        var unfinishedSession = SessionService.CheckUnfinishedSession();
        if (unfinishedSession != null)
        {
            LoggingService.LogInfo(AuditEventType.ExamStarted, $"Pemulihan sesi otomatis diaktifkan untuk '{unfinishedSession.ExamName}'. Menyimpan & memulihkan konfigurasi server sebelumnya.");
            
            // Otomatis bersihkan marker pemulihan & arahkan langsung ke sesi ujian menggunakan pengaturan server sebelumnya
            SessionService.ClearRecoveryMarker();
            StateManager.NavigateToExam();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (MainWindow != null && KioskService != null && KioskService.IsKioskActive)
            {
                KioskService.DisableKiosk(MainWindow);
            }
            BrowserService?.Shutdown();
            LoggingService?.LogInfo(AuditEventType.ExamCompleted, "Aplikasi Exambro-Muhipo ditutup secara normal.");
        }
        catch
        {
            // Ignored on app exit
        }

        base.OnExit(e);
    }
}
