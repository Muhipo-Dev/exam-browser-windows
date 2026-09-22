using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using ExambroMuhipo.Utilities;
using Microsoft.Win32;

namespace ExambroMuhipo.ViewModels;

/// <summary>
/// ViewModel untuk Ruang Administrasi (AdminView).
/// Memfasilitasi pembuatan, penyuntingan, impor/ekspor, pengujian, dan validasi profil ujian,
/// serta audit log sistem dan manajemen kredensial admin.
/// </summary>
public class AdminViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly IProfileManager _profileManager;
    private readonly ILoggingService _logger;
    private readonly IApplicationStateManager _stateManager;
    private readonly INavigationPolicy _navigationPolicy;

    public ObservableCollection<string> AvailableProfiles { get; } = new();

    private string? _selectedProfilePath;
    public string? SelectedProfilePath
    {
        get => _selectedProfilePath;
        set
        {
            if (SetField(ref _selectedProfilePath, value) && value != null)
            {
                LoadProfileForEditing(value);
            }
        }
    }

    private ExamProfile _editingProfile = new();
    public ExamProfile EditingProfile
    {
        get => _editingProfile;
        set => SetField(ref _editingProfile, value);
    }

    public string ServerUrlInput
    {
        get => EditingProfile.StartUrl;
        set
        {
            if (EditingProfile.StartUrl != value)
            {
                EditingProfile.StartUrl = value;
                OnPropertyChanged(nameof(ServerUrlInput));
                OnPropertyChanged(nameof(EditingProfile));
            }
        }
    }

    private string _allowedDomainsText = string.Empty;
    public string AllowedDomainsText
    {
        get => _allowedDomainsText;
        set
        {
            if (SetField(ref _allowedDomainsText, value))
            {
                SyncAllowedDomainsFromText();
            }
        }
    }

    private string _allowedUrlsText = string.Empty;
    public string AllowedUrlsText
    {
        get => _allowedUrlsText;
        set
        {
            if (SetField(ref _allowedUrlsText, value))
            {
                SyncAllowedUrlsFromText();
            }
        }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private bool _isStatusError;
    public bool IsStatusError
    {
        get => _isStatusError;
        set => SetField(ref _isStatusError, value);
    }

    // Ubah Password
    private string _currentPasswordInput = string.Empty;
    public string CurrentPasswordInput
    {
        get => _currentPasswordInput;
        set => SetField(ref _currentPasswordInput, value);
    }

    private string _newPasswordInput = string.Empty;
    public string NewPasswordInput
    {
        get => _newPasswordInput;
        set => SetField(ref _newPasswordInput, value);
    }

    private string _confirmPasswordInput = string.Empty;
    public string ConfirmPasswordInput
    {
        get => _confirmPasswordInput;
        set => SetField(ref _confirmPasswordInput, value);
    }

    public ObservableCollection<LogEntry> AuditLogs { get; } = new();

    // Commands
    public ICommand SaveProfileCommand { get; }
    public ICommand SaveAsProfileCommand { get; }
    public ICommand NewProfileCommand { get; }
    public ICommand DuplicateProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand ImportProfileCommand { get; }
    public ICommand ExportProfileCommand { get; }
    public ICommand TestProfileCommand { get; }
    public ICommand StartExamCommand { get; }
    public ICommand SetActiveProfileCommand { get; }
    public ICommand ChangePasswordCommand { get; }
    public ICommand RefreshLogsCommand { get; }
    public ICommand BackToHomeCommand { get; }
    public ICommand ApplyLocalLanPresetCommand { get; }
    public ICommand ApplyCloudPresetCommand { get; }
    public ICommand ApplyUtbkPresetCommand { get; }
    public ICommand QuickSaveAndActivateCommand { get; }
    public ICommand SaveServerConfigCommand { get; }
    public ICommand ApplyPresetLan1Command { get; }
    public ICommand ApplyPresetLan2Command { get; }
    public ICommand ApplyPresetCloudServerCommand { get; }

    public AdminViewModel(
        IConfigurationService configService,
        IProfileManager profileManager,
        ILoggingService logger,
        IApplicationStateManager stateManager,
        INavigationPolicy navigationPolicy)
    {
        _configService = configService;
        _profileManager = profileManager;
        _logger = logger;
        _stateManager = stateManager;
        _navigationPolicy = navigationPolicy;

        SaveProfileCommand = new RelayCommand(OnSaveProfile);
        SaveAsProfileCommand = new RelayCommand(OnSaveAsProfile);
        NewProfileCommand = new RelayCommand(OnNewProfile);
        DuplicateProfileCommand = new RelayCommand(OnDuplicateProfile);
        DeleteProfileCommand = new RelayCommand(OnDeleteProfile);
        ImportProfileCommand = new RelayCommand(OnImportProfile);
        ExportProfileCommand = new RelayCommand(OnExportProfile);
        TestProfileCommand = new RelayCommand(OnTestProfile);
        StartExamCommand = new RelayCommand(OnStartExam);
        SetActiveProfileCommand = new RelayCommand(OnSetActiveProfile);
        ChangePasswordCommand = new RelayCommand(OnChangePassword);
        RefreshLogsCommand = new RelayCommand(LoadAuditLogs);
        BackToHomeCommand = new RelayCommand(() => _stateManager.NavigateToHome());

        ApplyLocalLanPresetCommand = new RelayCommand(OnApplyLocalLanPreset);
        ApplyCloudPresetCommand = new RelayCommand(OnApplyCloudPreset);
        ApplyUtbkPresetCommand = new RelayCommand(OnApplyUtbkPreset);
        QuickSaveAndActivateCommand = new RelayCommand(OnQuickSaveAndActivate);

        SaveServerConfigCommand = new RelayCommand(OnSaveServerConfig);
        ApplyPresetLan1Command = new RelayCommand(() => SetPresetUrl("http://192.168.1.200:8080/cbt"));
        ApplyPresetLan2Command = new RelayCommand(() => SetPresetUrl("http://192.168.0.100"));
        ApplyPresetCloudServerCommand = new RelayCommand(() => SetPresetUrl("https://cbt.muhipo.sch.id"));

        RefreshProfileList();
        LoadAuditLogs();
    }

    private void SetPresetUrl(string url)
    {
        ServerUrlInput = url;
        ShowMessage($"Preset diisi: {url}. Klik 'Simpan Alamat Server' untuk menerapkan.", isError: false);
    }

    public void OnSaveServerConfig()
    {
        if (string.IsNullOrWhiteSpace(EditingProfile.StartUrl))
        {
            ShowMessage("Masukkan alamat IP atau tautan server CBT terlebih dahulu.", isError: true);
            return;
        }

        string trimmed = EditingProfile.StartUrl.Trim();
        if (!trimmed.Contains("://") && !trimmed.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "http://" + trimmed;
            EditingProfile.StartUrl = trimmed;
        }

        EditingProfile.AllowLocalIpHttp = true;
        EditingProfile.KioskMode = true;
        EditingProfile.FullScreen = true;
        EditingProfile.SessionTimeout = 0; // Tanpa batas waktu klien
        EditingProfile.ExamName = "Ujian CBT SMA Muhammadiyah 1 Ponorogo";

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (EditingProfile.AllowedDomains == null)
                EditingProfile.AllowedDomains = new();
            if (!EditingProfile.AllowedDomains.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                EditingProfile.AllowedDomains.Add(uri.Host);
        }

        string savePath = !string.IsNullOrEmpty(SelectedProfilePath) 
            ? SelectedProfilePath 
            : _configService.ActiveProfilePath;

        if (string.IsNullOrEmpty(savePath))
        {
            savePath = Path.Combine(_configService.GetProfilesDirectory(), "default_profile.json");
        }

        _configService.SaveProfile(EditingProfile, savePath);
        _configService.ActiveProfile.StartUrl = trimmed;
        _configService.ActiveProfile.AllowLocalIpHttp = true;
        _configService.ActiveProfile.SessionTimeout = 0;

        _logger.LogInfo(AuditEventType.ConfigurationLoaded, $"Alamat IP server diperbarui ke: {trimmed}");
        ShowMessage($"✅ Berhasil disimpan! Alamat server aktif: {trimmed}", isError: false);
        OnPropertyChanged(nameof(ServerUrlInput));
    }

    private void OnApplyLocalLanPreset()
    {
        EditingProfile.ExamName = "Ujian CBT Server Lokal Lab (LAN)";
        EditingProfile.ExamDescription = "Ujian offline menggunakan server lokal laboratorium komputer (HTTP)";
        EditingProfile.StartUrl = "http://192.168.1.200:8080/cbt";
        EditingProfile.AllowLocalIpHttp = true;
        EditingProfile.EnforceHttps = false;
        EditingProfile.KioskMode = true;
        EditingProfile.FullScreen = true;
        EditingProfile.SessionTimeout = 90;
        AllowedDomainsText = "192.168.1.200" + Environment.NewLine + "192.168.1.*" + Environment.NewLine + "localhost";
        ShowMessage("Preset Server Lokal LAN berhasil dimuat. Sesuaikan IP server jika berbeda, lalu klik 'Simpan & Aktifkan'.", isError: false);
    }

    private void OnApplyCloudPreset()
    {
        EditingProfile.ExamName = "Asesmen Sumatif SMA Muhammadiyah 1 Ponorogo";
        EditingProfile.ExamDescription = "Ujian resmi server cloud sekolah via HTTPS";
        EditingProfile.StartUrl = "https://cbt.muhipo.sch.id/exam";
        EditingProfile.AllowLocalIpHttp = true;
        EditingProfile.EnforceHttps = true;
        EditingProfile.KioskMode = true;
        EditingProfile.FullScreen = true;
        EditingProfile.SessionTimeout = 90;
        AllowedDomainsText = "cbt.muhipo.sch.id" + Environment.NewLine + "*.muhipo.sch.id" + Environment.NewLine + "muhipo.sch.id";
        ShowMessage("Preset Server Cloud Sekolah berhasil dimuat.", isError: false);
    }

    private void OnApplyUtbkPreset()
    {
        EditingProfile.ExamName = "Simulasi UTBK / CBT Mandiri Siswa";
        EditingProfile.ExamDescription = "Simulasi latihan UTBK mandiri dengan durasi 120 menit";
        EditingProfile.StartUrl = "https://example.com/utbk";
        EditingProfile.AllowLocalIpHttp = true;
        EditingProfile.EnforceHttps = true;
        EditingProfile.KioskMode = true;
        EditingProfile.FullScreen = true;
        EditingProfile.SessionTimeout = 120;
        AllowedDomainsText = "example.com" + Environment.NewLine + "*.example.com";
        ShowMessage("Preset Simulasi UTBK berhasil dimuat.", isError: false);
    }

    private void OnQuickSaveAndActivate()
    {
        OnSaveProfile();
        OnSetActiveProfile();
        ShowMessage($"Profil '{EditingProfile.ExamName}' berhasil disimpan dan dijadikan profil aktif utama!", isError: false);
    }

    public void RefreshProfileList()
    {
        AvailableProfiles.Clear();
        var paths = _configService.GetAvailableProfilePaths();
        foreach (var path in paths)
        {
            AvailableProfiles.Add(path);
        }

        if (AvailableProfiles.Count > 0)
        {
            string active = _configService.ActiveProfilePath;
            SelectedProfilePath = AvailableProfiles.Contains(active) ? active : AvailableProfiles.First();
        }
    }

    private void LoadProfileForEditing(string path)
    {
        try
        {
            EditingProfile = _configService.LoadProfile(path);
            _allowedDomainsText = string.Join(Environment.NewLine, EditingProfile.AllowedDomains ?? new());
            _allowedUrlsText = string.Join(Environment.NewLine, EditingProfile.AllowedUrls ?? new());
            OnPropertyChanged(nameof(AllowedDomainsText));
            OnPropertyChanged(nameof(AllowedUrlsText));
            ShowMessage($"Profil '{Path.GetFileName(path)}' siap disunting.", isError: false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Gagal memuat profil: {ex.Message}", isError: true);
        }
    }

    private void SyncAllowedDomainsFromText()
    {
        var list = _allowedDomainsText
            .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .ToList();
        EditingProfile.AllowedDomains = list;
    }

    private void SyncAllowedUrlsFromText()
    {
        var list = _allowedUrlsText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => !string.IsNullOrEmpty(u))
            .Distinct()
            .ToList();
        EditingProfile.AllowedUrls = list;
    }

    private void OnSaveProfile()
    {
        if (string.IsNullOrEmpty(SelectedProfilePath))
        {
            ShowMessage("Pilih atau buat profil terlebih dahulu.", isError: true);
            return;
        }

        if (!string.IsNullOrWhiteSpace(EditingProfile.StartUrl))
        {
            string trimmed = EditingProfile.StartUrl.Trim();
            if (!trimmed.Contains("://") && !trimmed.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                EditingProfile.StartUrl = "http://" + trimmed;
                OnPropertyChanged(nameof(EditingProfile));
            }

            if (Uri.TryCreate(EditingProfile.StartUrl, UriKind.Absolute, out var parsedUri))
            {
                if (ExamProfile.IsLocalOrPrivateIp(parsedUri.Host))
                {
                    EditingProfile.AllowLocalIpHttp = true;
                }

                if (EditingProfile.AllowedDomains == null)
                    EditingProfile.AllowedDomains = new();

                if (!EditingProfile.AllowedDomains.Contains(parsedUri.Host, StringComparer.OrdinalIgnoreCase))
                    EditingProfile.AllowedDomains.Add(parsedUri.Host);

                if (!string.IsNullOrEmpty(parsedUri.Authority) && !EditingProfile.AllowedDomains.Contains(parsedUri.Authority, StringComparer.OrdinalIgnoreCase))
                    EditingProfile.AllowedDomains.Add(parsedUri.Authority);

                _allowedDomainsText = string.Join(Environment.NewLine, EditingProfile.AllowedDomains);
                OnPropertyChanged(nameof(AllowedDomainsText));
            }
        }

        SyncAllowedDomainsFromText();
        SyncAllowedUrlsFromText();

        var errors = EditingProfile.Validate();
        if (errors.Count > 0)
        {
            ShowMessage($"Validasi gagal: {string.Join("; ", errors)}", isError: true);
            return;
        }

        try
        {
            _configService.SaveProfile(EditingProfile, SelectedProfilePath);
            if (string.Equals(SelectedProfilePath, _configService.ActiveProfilePath, StringComparison.OrdinalIgnoreCase))
            {
                _configService.SetActiveProfile(EditingProfile, SelectedProfilePath);
            }
            ShowMessage($"Profil '{Path.GetFileName(SelectedProfilePath)}' berhasil disimpan.", isError: false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Gagal menyimpan: {ex.Message}", isError: true);
        }
    }

    private void OnSaveAsProfile()
    {
        SyncAllowedDomainsFromText();
        SyncAllowedUrlsFromText();

        var errors = EditingProfile.Validate();
        if (errors.Count > 0)
        {
            ShowMessage($"Validasi gagal: {string.Join("; ", errors)}", isError: true);
            return;
        }

        var dlg = new SaveFileDialog
        {
            InitialDirectory = _configService.GetProfilesDirectory(),
            Filter = "JSON Profile (*.json)|*.json",
            FileName = $"{EditingProfile.ExamName.Replace(' ', '_')}.json"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                _configService.SaveProfile(EditingProfile, dlg.FileName);
                RefreshProfileList();
                SelectedProfilePath = dlg.FileName;
                ShowMessage($"Profil disimpan sebagai '{Path.GetFileName(dlg.FileName)}'.", isError: false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Gagal menyimpan sebagai: {ex.Message}", isError: true);
            }
        }
    }

    private void OnNewProfile()
    {
        var newProf = _profileManager.CreateNewProfile("Profil Ujian Baru");
        string safeName = $"profil_baru_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string targetPath = Path.Combine(_configService.GetProfilesDirectory(), safeName);

        _configService.SaveProfile(newProf, targetPath);
        RefreshProfileList();
        SelectedProfilePath = targetPath;
        ShowMessage("Profil baru berhasil dibuat.", isError: false);
    }

    private void OnDuplicateProfile()
    {
        if (string.IsNullOrEmpty(SelectedProfilePath))
            return;

        try
        {
            string newName = $"{EditingProfile.ExamName} (Salinan)";
            var dup = _profileManager.DuplicateProfile(SelectedProfilePath, newName);
            RefreshProfileList();
            ShowMessage($"Profil berhasil diduplikasi: {dup.ExamName}", isError: false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Gagal menduplikasi profil: {ex.Message}", isError: true);
        }
    }

    private void OnDeleteProfile()
    {
        if (string.IsNullOrEmpty(SelectedProfilePath))
            return;

        var result = MessageBox.Show(
            $"Apakah Anda yakin ingin menghapus profil '{Path.GetFileName(SelectedProfilePath)}'?",
            "Konfirmasi Hapus Profil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _profileManager.DeleteProfile(SelectedProfilePath);
                RefreshProfileList();
                ShowMessage("Profil berhasil dihapus.", isError: false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Gagal menghapus profil: {ex.Message}", isError: true);
            }
        }
    }

    private void OnImportProfile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON Profile (*.json)|*.json"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                string targetName = Path.GetFileName(dlg.FileName);
                var imported = _profileManager.ImportProfile(dlg.FileName, targetName);
                RefreshProfileList();
                ShowMessage($"Profil '{imported.ExamName}' berhasil diimpor.", isError: false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Gagal mengimpor profil: {ex.Message}", isError: true);
            }
        }
    }

    private void OnExportProfile()
    {
        if (EditingProfile == null)
            return;

        var dlg = new SaveFileDialog
        {
            Filter = "JSON Profile (*.json)|*.json",
            FileName = $"{EditingProfile.ExamName.Replace(' ', '_')}.json"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                _profileManager.ExportProfile(EditingProfile, dlg.FileName);
                ShowMessage($"Profil diekspor ke: {dlg.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Gagal mengekspor profil: {ex.Message}", isError: true);
            }
        }
    }

    private void OnTestProfile()
    {
        SyncAllowedDomainsFromText();
        SyncAllowedUrlsFromText();

        var errors = EditingProfile.Validate();
        if (errors.Count > 0)
        {
            ShowMessage($"Pengujian gagal! Ditemukan kesalahan: {string.Join("; ", errors)}", isError: true);
            return;
        }

        // Uji evaluasi navigasi StartUrl
        var navResult = _navigationPolicy.EvaluateNavigation(EditingProfile.StartUrl, EditingProfile);
        if (!navResult.IsAllowed)
        {
            ShowMessage($"PERINGATAN: Start URL '{EditingProfile.StartUrl}' akan diblokir oleh kebijakan sendiri! Alasan: {navResult.Reason}", isError: true);
            return;
        }

        ShowMessage($"UJI PROFIL BERHASIL! Start URL lolos evaluasi whitelist domain '{navResult.Hostname}'.", isError: false);
    }

    private void OnSetActiveProfile()
    {
        if (string.IsNullOrEmpty(SelectedProfilePath))
            return;

        OnSaveProfile();
        _configService.SetActiveProfile(EditingProfile, SelectedProfilePath);
        ShowMessage($"Profil '{EditingProfile.ExamName}' telah ditetapkan sebagai profil aktif utama.", isError: false);
    }

    private void OnStartExam()
    {
        OnSaveServerConfig();
        _stateManager.NavigateToExam();
    }

    private void OnChangePassword()
    {
        if (string.IsNullOrWhiteSpace(NewPasswordInput))
        {
            ShowMessage("Password baru tidak boleh kosong.", isError: true);
            return;
        }

        if (NewPasswordInput != ConfirmPasswordInput)
        {
            ShowMessage("Konfirmasi password baru tidak cocok.", isError: true);
            return;
        }

        if (!_configService.VerifyAdminPassword(CurrentPasswordInput))
        {
            ShowMessage("Password saat ini salah.", isError: true);
            return;
        }

        try
        {
            _configService.SetAdminPassword(NewPasswordInput);
            CurrentPasswordInput = string.Empty;
            NewPasswordInput = string.Empty;
            ConfirmPasswordInput = string.Empty;
            ShowMessage("Password administrator berhasil diubah dan dienkripsi PBKDF2.", isError: false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Gagal mengubah password: {ex.Message}", isError: true);
        }
    }

    public void LoadAuditLogs()
    {
        AuditLogs.Clear();
        var logs = _logger.GetRecentLogs(100);
        foreach (var l in logs)
        {
            AuditLogs.Add(l);
        }
    }

    private void ShowMessage(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
