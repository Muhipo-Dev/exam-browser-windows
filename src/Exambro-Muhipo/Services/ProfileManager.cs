using System;
using System.Collections.Generic;
using System.IO;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi manajemen file profil ujian (create, duplicate, delete, import, export, validate).
/// </summary>
public class ProfileManager : IProfileManager
{
    private readonly IConfigurationService _configService;
    private readonly ILoggingService _logger;

    public ProfileManager(IConfigurationService configService, ILoggingService logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public IReadOnlyList<string> GetProfileList()
    {
        return _configService.GetAvailableProfilePaths();
    }

    public ExamProfile CreateNewProfile(string name)
    {
        var profile = new ExamProfile
        {
            ExamName = name,
            ExamDescription = $"Profil ujian {name} - SMA Muhammadiyah 1 Ponorogo",
            StartUrl = "https://example.com/exam",
            AllowedDomains = new List<string> { "example.com", "*.example.com" },
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

        return profile;
    }

    public ExamProfile DuplicateProfile(string sourceFilePath, string newName)
    {
        var source = _configService.LoadProfile(sourceFilePath);
        var copy = source.Clone();
        copy.ExamName = newName;
        copy.ExamDescription = $"Salinan dari {source.ExamName}";

        string safeFileName = SanitizeFileName(newName) + ".json";
        string targetPath = Path.Combine(_configService.GetProfilesDirectory(), safeFileName);

        int counter = 1;
        while (File.Exists(targetPath))
        {
            safeFileName = $"{SanitizeFileName(newName)}_{counter}.json";
            targetPath = Path.Combine(_configService.GetProfilesDirectory(), safeFileName);
            counter++;
        }

        _configService.SaveProfile(copy, targetPath);
        return copy;
    }

    public void DeleteProfile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        // Cek jika profil yang dihapus adalah profil aktif saat ini
        if (string.Equals(Path.GetFullPath(filePath), Path.GetFullPath(_configService.ActiveProfilePath), StringComparison.OrdinalIgnoreCase))
        {
            var remaining = _configService.GetAvailableProfilePaths().Where(p => !string.Equals(p, filePath, StringComparison.OrdinalIgnoreCase)).ToList();
            if (remaining.Count == 0)
            {
                throw new InvalidOperationException("Tidak dapat menghapus satu-satunya profil yang tersedia.");
            }
            var next = _configService.LoadProfile(remaining.First());
            _configService.SetActiveProfile(next, remaining.First());
        }

        File.Delete(filePath);
        _logger.LogInfo(AuditEventType.ConfigurationLoaded, $"Profil ujian dihapus: {Path.GetFileName(filePath)}");
    }

    public ExamProfile ImportProfile(string sourceFilePath, string destinationFileName)
    {
        var imported = _configService.LoadProfile(sourceFilePath);
        string safeName = SanitizeFileName(destinationFileName);
        if (!safeName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            safeName += ".json";

        string targetPath = Path.Combine(_configService.GetProfilesDirectory(), safeName);
        _configService.SaveProfile(imported, targetPath);
        _logger.LogInfo(AuditEventType.ConfigurationLoaded, $"Profil berhasil diimpor: {safeName}");
        return imported;
    }

    public void ExportProfile(ExamProfile profile, string destinationFilePath)
    {
        _configService.SaveProfile(profile, destinationFilePath);
        _logger.LogInfo(AuditEventType.ConfigurationLoaded, $"Profil diekspor ke: {destinationFilePath}");
    }

    public List<string> ValidateProfile(ExamProfile profile)
    {
        return profile.Validate();
    }

    private static string SanitizeFileName(string name)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars());
        string clean = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "profile" : clean.Replace(" ", "_");
    }
}
