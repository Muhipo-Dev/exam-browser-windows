using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ExambroMuhipo.Models;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.Services;

/// <summary>
/// Layanan terpusat untuk konfigurasi aplikasi dan profil ujian.
/// Mencegah pembacaan JSON acak dari sembarang kelas.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILoggingService _logger;
    private readonly string _baseDirectory;
    private readonly string _configDirectory;
    private readonly string _profilesDirectory;
    private readonly string _configFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppConfiguration Config { get; private set; } = new();
    public ExamProfile ActiveProfile { get; private set; } = new();
    public string ActiveProfilePath { get; private set; } = string.Empty;

    public ConfigurationService(ILoggingService logger, string? customBaseDir = null)
    {
        _logger = logger;
        _baseDirectory = customBaseDir ?? AppDomain.CurrentDomain.BaseDirectory;
        _configDirectory = Path.Combine(_baseDirectory, "Configuration");
        _profilesDirectory = Path.Combine(_baseDirectory, "Profiles");
        _configFilePath = Path.Combine(_configDirectory, "appsettings.json");

        EnsureDirectoriesExist();
    }

    private void EnsureDirectoriesExist()
    {
        try
        {
            if (!Directory.Exists(_configDirectory))
                Directory.CreateDirectory(_configDirectory);

            if (!Directory.Exists(_profilesDirectory))
                Directory.CreateDirectory(_profilesDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(AuditEventType.ConfigurationError, "Gagal membuat direktori konfigurasi.", exception: ex);
        }
    }

    public void LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                string json = File.ReadAllText(_configFilePath);
                var loaded = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
                if (loaded != null)
                {
                    Config = loaded;
                }
            }
            else
            {
                Config = new AppConfiguration();
            }

            // Inisialisasi password admin default jika belum ada: "MUHIPO23"
            if (string.IsNullOrEmpty(Config.AdminPasswordHash) || string.IsNullOrEmpty(Config.AdminPasswordSalt))
            {
                var (salt, hash) = PasswordHasher.HashPassword("MUHIPO23");
                Config.AdminPasswordSalt = salt;
                Config.AdminPasswordHash = hash;
                SaveConfiguration();
            }

            // Inisialisasi password exit default jika belum ada: "MUHIPO23"
            if (string.IsNullOrEmpty(Config.ExitPasswordHash) || string.IsNullOrEmpty(Config.ExitPasswordSalt))
            {
                var (saltExit, hashExit) = PasswordHasher.HashPassword("MUHIPO23");
                Config.ExitPasswordSalt = saltExit;
                Config.ExitPasswordHash = hashExit;
                SaveConfiguration();
            }

            _logger.LogInfo(AuditEventType.ConfigurationLoaded, "Konfigurasi aplikasi berhasil dimuat.", $"File: {_configFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(AuditEventType.ConfigurationError, "Kesalahan saat memuat konfigurasi aplikasi.", exception: ex);
            // Fallback default
            Config = new AppConfiguration();
            var (salt, hash) = PasswordHasher.HashPassword("MUHIPO23");
            Config.AdminPasswordSalt = salt;
            Config.AdminPasswordHash = hash;
            var (saltExit, hashExit) = PasswordHasher.HashPassword("MUHIPO23");
            Config.ExitPasswordSalt = saltExit;
            Config.ExitPasswordHash = hashExit;
        }

        // Muat profil aktif
        LoadActiveProfile();
    }

    private void LoadActiveProfile()
    {
        string candidatePath = Path.Combine(_profilesDirectory, Config.ActiveProfileFile);
        if (File.Exists(candidatePath))
        {
            try
            {
                ActiveProfile = LoadProfile(candidatePath);
                ActiveProfilePath = candidatePath;
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(AuditEventType.ConfigurationError, $"Gagal memuat profil aktif '{candidatePath}'.", exception: ex);
            }
        }

        // Cari profil apa pun di folder Profiles
        var firstProfile = Directory.GetFiles(_profilesDirectory, "*.json").FirstOrDefault();
        if (firstProfile != null)
        {
            try
            {
                ActiveProfile = LoadProfile(firstProfile);
                ActiveProfilePath = firstProfile;
                Config.ActiveProfileFile = Path.GetFileName(firstProfile);
                SaveConfiguration();
                return;
            }
            catch
            {
                // Lanjut ke fallback create
            }
        }

        // Buat profil default jika sama sekali belum ada
        ActiveProfile = CreateDefaultProfile();
        ActiveProfilePath = Path.Combine(_profilesDirectory, "default_profile.json");
        SaveProfile(ActiveProfile, ActiveProfilePath);
        Config.ActiveProfileFile = "default_profile.json";
        SaveConfiguration();
    }

    private static ExamProfile CreateDefaultProfile()
    {
        return new ExamProfile
        {
            ExamName = "Ujian Sekolah SMA Muhammadiyah 1 Ponorogo",
            ExamDescription = "Profil resmi pelaksanaan Asesmen/Ujian Berbasis Komputer SMA Muhammadiyah 1 Ponorogo",
            StartUrl = "https://example.com/ujian",
            AllowedDomains = new List<string>
            {
                "example.com",
                "*.example.com",
                "muhipo.sch.id",
                "*.muhipo.sch.id"
            },
            AllowedUrls = new List<string>(),
            FullScreen = true,
            KioskMode = true,
            DisableContextMenu = true,
            DisableDevTools = true,
            DisableNewWindows = true,
            DisablePopups = true,
            AllowClipboard = false,
            AllowCopy = false,
            AllowPaste = false,
            AllowPrinting = false,
            AllowDownloads = false,
            AllowUploads = true,
            AllowZoom = false,
            AllowCamera = false,
            AllowMicrophone = false,
            AllowAudio = true,
            SessionTimeout = 90,
            RequireFullscreen = true,
            ExitPolicy = "AdminPasswordOnly",
            EnforceHttps = true
        };
    }

    public void SaveConfiguration()
    {
        try
        {
            string json = JsonSerializer.Serialize(Config, JsonOptions);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(AuditEventType.ConfigurationError, "Gagal menyimpan konfigurasi aplikasi.", exception: ex);
            throw;
        }
    }

    public bool VerifyAdminPassword(string plainPassword)
    {
        bool isValid = PasswordHasher.VerifyPassword(plainPassword, Config.AdminPasswordSalt, Config.AdminPasswordHash);
        if (isValid)
        {
            _logger.LogInfo(AuditEventType.AdminLoginSuccess, "Autentikasi administrator berhasil.");
        }
        else
        {
            _logger.LogWarning(AuditEventType.AdminLoginFailed, "Autentikasi administrator ditolak: password salah.");
        }
        return isValid;
    }

    public void SetAdminPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("Password baru tidak boleh kosong.", nameof(newPassword));

        var (salt, hash) = PasswordHasher.HashPassword(newPassword);
        Config.AdminPasswordSalt = salt;
        Config.AdminPasswordHash = hash;
        SaveConfiguration();
        _logger.LogInfo(AuditEventType.ConfigurationLoaded, "Password administrator berhasil diperbarui.");
    }

    public bool VerifyExitPassword(string plainPassword)
    {
        bool isValid = PasswordHasher.VerifyPassword(plainPassword, Config.ExitPasswordSalt, Config.ExitPasswordHash);
        if (isValid)
        {
            _logger.LogInfo(AuditEventType.ExitAuthorized, "Otorisasi keluar aplikasi berhasil disetujui.");
        }
        else
        {
            _logger.LogWarning(AuditEventType.ExitDenied, "Otorisasi keluar aplikasi ditolak: password salah.");
        }
        return isValid;
    }

    public void SetExitPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("Password keluar baru tidak boleh kosong.", nameof(newPassword));

        var (salt, hash) = PasswordHasher.HashPassword(newPassword);
        Config.ExitPasswordSalt = salt;
        Config.ExitPasswordHash = hash;
        SaveConfiguration();
        _logger.LogInfo(AuditEventType.ConfigurationLoaded, "Password keluar aplikasi berhasil diperbarui.");
    }

    public ExamProfile LoadProfile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File profil tidak ditemukan: {filePath}");

        string json = File.ReadAllText(filePath);
        var profile = JsonSerializer.Deserialize<ExamProfile>(json, JsonOptions);
        if (profile == null)
            throw new InvalidOperationException("Format berkas profil JSON tidak valid.");

        var errors = profile.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Profil tidak valid: {string.Join("; ", errors)}");
        }

        return profile;
    }

    public void SaveProfile(ExamProfile profile, string filePath)
    {
        var errors = profile.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Profil tidak dapat disimpan karena tidak valid: {string.Join("; ", errors)}");
        }

        string json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(filePath, json);
        _logger.LogInfo(AuditEventType.ConfigurationLoaded, $"Profil ujian disimpan: {Path.GetFileName(filePath)}");
    }

    public void SetActiveProfile(ExamProfile profile, string filePath)
    {
        ActiveProfile = profile;
        ActiveProfilePath = filePath;
        Config.ActiveProfileFile = Path.GetFileName(filePath);
        SaveConfiguration();
    }

    public string GetProfilesDirectory() => _profilesDirectory;

    public string GetConfigFilePath() => _configFilePath;

    public List<string> GetAvailableProfilePaths()
    {
        if (!Directory.Exists(_profilesDirectory))
            return new List<string>();

        return Directory.GetFiles(_profilesDirectory, "*.json").ToList();
    }
}
