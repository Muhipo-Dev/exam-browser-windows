using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExambroMuhipo.Models;

/// <summary>
/// Profil konfigurasi ujian terstruktur untuk Exambro-Muhipo.
/// </summary>
public class ExamProfile
{
    [JsonPropertyName("examName")]
    public string ExamName { get; set; } = "Ujian Sekolah SMA Muhammadiyah 1 Ponorogo";

    [JsonPropertyName("examDescription")]
    public string ExamDescription { get; set; } = "Profil standar pelaksanaan ujian daring sekolah";

    [JsonPropertyName("startUrl")]
    public string StartUrl { get; set; } = "https://example.com/exam";

    [JsonPropertyName("allowedDomains")]
    public List<string> AllowedDomains { get; set; } = new()
    {
        "example.com",
        "*.example.com"
    };

    [JsonPropertyName("allowedUrls")]
    public List<string> AllowedUrls { get; set; } = new();

    [JsonPropertyName("fullScreen")]
    public bool FullScreen { get; set; } = true;

    [JsonPropertyName("kioskMode")]
    public bool KioskMode { get; set; } = true;

    [JsonPropertyName("disableContextMenu")]
    public bool DisableContextMenu { get; set; } = true;

    [JsonPropertyName("disableDevTools")]
    public bool DisableDevTools { get; set; } = true;

    [JsonPropertyName("disableNewWindows")]
    public bool DisableNewWindows { get; set; } = true;

    [JsonPropertyName("disablePopups")]
    public bool DisablePopups { get; set; } = true;

    [JsonPropertyName("allowClipboard")]
    public bool AllowClipboard { get; set; } = false;

    [JsonPropertyName("allowCopy")]
    public bool AllowCopy { get; set; } = false;

    [JsonPropertyName("allowPaste")]
    public bool AllowPaste { get; set; } = false;

    [JsonPropertyName("allowPrinting")]
    public bool AllowPrinting { get; set; } = false;

    [JsonPropertyName("allowDownloads")]
    public bool AllowDownloads { get; set; } = false;

    [JsonPropertyName("allowUploads")]
    public bool AllowUploads { get; set; } = true;

    [JsonPropertyName("allowZoom")]
    public bool AllowZoom { get; set; } = false;

    [JsonPropertyName("allowCamera")]
    public bool AllowCamera { get; set; } = false;

    [JsonPropertyName("allowMicrophone")]
    public bool AllowMicrophone { get; set; } = false;

    [JsonPropertyName("allowAudio")]
    public bool AllowAudio { get; set; } = true;

    [JsonPropertyName("sessionTimeout")]
    public int SessionTimeout { get; set; } = 90; // Menit (0 = tanpa batas)

    [JsonPropertyName("requireFullscreen")]
    public bool RequireFullscreen { get; set; } = true;

    [JsonPropertyName("exitPolicy")]
    public string ExitPolicy { get; set; } = "AdminPasswordOnly";

    [JsonPropertyName("enforceHttps")]
    public bool EnforceHttps { get; set; } = true;

    [JsonPropertyName("allowLocalIpHttp")]
    public bool AllowLocalIpHttp { get; set; } = true;

    [JsonPropertyName("clearCacheOnFinish")]
    public bool ClearCacheOnFinish { get; set; } = true;

    [JsonPropertyName("clearCookiesOnFinish")]
    public bool ClearCookiesOnFinish { get; set; } = false;

    /// <summary>
    /// Membuat salinan mendalam (deep clone) dari profil.
    /// </summary>
    public ExamProfile Clone()
    {
        return new ExamProfile
        {
            ExamName = this.ExamName,
            ExamDescription = this.ExamDescription,
            StartUrl = this.StartUrl,
            AllowedDomains = new List<string>(this.AllowedDomains),
            AllowedUrls = new List<string>(this.AllowedUrls),
            FullScreen = this.FullScreen,
            KioskMode = this.KioskMode,
            DisableContextMenu = this.DisableContextMenu,
            DisableDevTools = this.DisableDevTools,
            DisableNewWindows = this.DisableNewWindows,
            DisablePopups = this.DisablePopups,
            AllowClipboard = this.AllowClipboard,
            AllowCopy = this.AllowCopy,
            AllowPaste = this.AllowPaste,
            AllowPrinting = this.AllowPrinting,
            AllowDownloads = this.AllowDownloads,
            AllowUploads = this.AllowUploads,
            AllowZoom = this.AllowZoom,
            AllowCamera = this.AllowCamera,
            AllowMicrophone = this.AllowMicrophone,
            AllowAudio = this.AllowAudio,
            SessionTimeout = this.SessionTimeout,
            RequireFullscreen = this.RequireFullscreen,
            ExitPolicy = this.ExitPolicy,
            EnforceHttps = this.EnforceHttps,
            AllowLocalIpHttp = this.AllowLocalIpHttp,
            ClearCacheOnFinish = this.ClearCacheOnFinish,
            ClearCookiesOnFinish = this.ClearCookiesOnFinish
        };
    }

    /// <summary>
    /// Memeriksa apakah alamat hostname merupakan IP privat LAN (10.*, 172.16-31.*, 192.168.*) atau localhost.
    /// </summary>
    public static bool IsLocalOrPrivateIp(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;

        string cleanHost = host.Trim().ToLowerInvariant();
        if (cleanHost == "localhost" || cleanHost == "127.0.0.1")
            return true;

        if (System.Net.IPAddress.TryParse(cleanHost, out var ip))
        {
            byte[] bytes = ip.GetAddressBytes();
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                // Loopback 127.x.x.x
                if (bytes[0] == 127) return true;
                // Link-local 169.254.x.x
                if (bytes[0] == 169 && bytes[1] == 254) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Memvalidasi kelayakan profil.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ExamName))
            errors.Add("Nama ujian tidak boleh kosong.");

        if (string.IsNullOrWhiteSpace(StartUrl))
        {
            errors.Add("Start URL tidak boleh kosong.");
        }
        else if (!Uri.TryCreate(StartUrl, UriKind.Absolute, out var uri) || 
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("Start URL harus berupa alamat HTTP/HTTPS yang valid.");
        }
        else if (EnforceHttps && uri.Scheme != Uri.UriSchemeHttps)
        {
            if (!AllowLocalIpHttp || !IsLocalOrPrivateIp(uri.Host))
            {
                errors.Add("Start URL wajib menggunakan protokol HTTPS sesuai kebijakan keamanan (kecuali server IP lokal).");
            }
        }

        if (AllowedDomains == null || AllowedDomains.Count == 0)
        {
            errors.Add("Minimal harus ada satu domain atau IP yang diizinkan (AllowedDomains).");
        }

        if (SessionTimeout < 0)
        {
            errors.Add("Session timeout tidak boleh bernilai negatif.");
        }

        return errors;
    }
}
