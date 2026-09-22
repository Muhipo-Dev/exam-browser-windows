using System.Text.Json.Serialization;

namespace ExambroMuhipo.Models;

/// <summary>
/// Konfigurasi aplikasi Exambro-Muhipo.
/// </summary>
public class AppConfiguration
{
    [JsonPropertyName("appName")]
    public string AppName { get; set; } = "Exambro-Muhipo";

    [JsonPropertyName("appSubtitle")]
    public string AppSubtitle { get; set; } = "Secure Examination Browser";

    [JsonPropertyName("schoolName")]
    public string SchoolName { get; set; } = "SMA Muhammadiyah 1 Ponorogo";

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = "1.3.2-beta";

    [JsonPropertyName("activeProfileFile")]
    public string ActiveProfileFile { get; set; } = "default_profile.json";

    /// <summary>
    /// PBKDF2 salt dalam format Base64.
    /// </summary>
    [JsonPropertyName("adminPasswordSalt")]
    public string AdminPasswordSalt { get; set; } = "";

    /// <summary>
    /// PBKDF2 hash password admin dalam format Base64.
    /// </summary>
    [JsonPropertyName("adminPasswordHash")]
    public string AdminPasswordHash { get; set; } = "";

    [JsonPropertyName("enableDetailedAuditLog")]
    public bool EnableDetailedAuditLog { get; set; } = true;

    [JsonPropertyName("logRetentionDays")]
    public int LogRetentionDays { get; set; } = 30;

    [JsonPropertyName("allowOfflineEmergencyExit")]
    public bool AllowOfflineEmergencyExit { get; set; } = true;
}
