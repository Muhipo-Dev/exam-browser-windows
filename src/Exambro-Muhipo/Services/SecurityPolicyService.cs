using System;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi penegakan kebijakan keamanan aplikasi (navigation, downloads, popups, clipboard, permissions, dll).
/// </summary>
public class SecurityPolicyService : ISecurityPolicyService
{
    private readonly INavigationPolicy _navigationPolicy;
    private readonly ILoggingService _logger;

    public SecurityPolicyService(INavigationPolicy navigationPolicy, ILoggingService logger)
    {
        _navigationPolicy = navigationPolicy;
        _logger = logger;
    }

    public NavigationCheckResult CheckNavigation(string url, ExamProfile profile)
    {
        var result = _navigationPolicy.EvaluateNavigation(url, profile);
        if (result.IsAllowed)
        {
            _logger.LogInfo(AuditEventType.NavigationAllowed, $"Navigasi diizinkan: {result.NormalizedUrl}", $"Host: {result.Hostname}");
        }
        else
        {
            _logger.LogWarning(AuditEventType.NavigationBlocked, $"Navigasi diblokir: {result.Reason}", $"URL: {url}");
        }
        return result;
    }

    public bool CanOpenNewWindow(ExamProfile profile, string targetUrl)
    {
        if (profile.DisableNewWindows)
        {
            _logger.LogWarning(AuditEventType.NewWindowBlocked, "Permintaan pembukaan jendela baru (NewWindow) diblokir sesuai profil.", $"Target: {targetUrl}");
            return false;
        }

        var navResult = _navigationPolicy.EvaluateNavigation(targetUrl, profile);
        if (!navResult.IsAllowed)
        {
            _logger.LogWarning(AuditEventType.NewWindowBlocked, $"Permintaan jendela baru ke URL yang tidak diizinkan diblokir: {navResult.Reason}", $"Target: {targetUrl}");
            return false;
        }

        return true;
    }

    public bool CanShowPopup(ExamProfile profile, string targetUrl)
    {
        if (profile.DisablePopups)
        {
            _logger.LogWarning(AuditEventType.PopupBlocked, "Popup diblokir sesuai kebijakan profil.", $"Target: {targetUrl}");
            return false;
        }

        var navResult = _navigationPolicy.EvaluateNavigation(targetUrl, profile);
        if (!navResult.IsAllowed)
        {
            _logger.LogWarning(AuditEventType.PopupBlocked, $"Popup ke URL di luar whitelist diblokir: {navResult.Reason}", $"Target: {targetUrl}");
            return false;
        }

        return true;
    }

    public bool CanDownload(ExamProfile profile, string uri)
    {
        if (!profile.AllowDownloads)
        {
            _logger.LogWarning(AuditEventType.DownloadBlocked, "Pengunduhan berkas diblokir oleh kebijakan keamanan.", $"URI: {uri}");
            return false;
        }

        return true;
    }

    public bool CanUseClipboard(ExamProfile profile) => profile.AllowClipboard;

    public bool CanPrint(ExamProfile profile) => profile.AllowPrinting;

    public bool CanUseDevTools(ExamProfile profile) => !profile.DisableDevTools;

    public bool CanUseContextMenu(ExamProfile profile) => !profile.DisableContextMenu;

    public bool CanUseZoom(ExamProfile profile) => profile.AllowZoom;

    public bool CanGrantPermission(ExamProfile profile, string permissionKind)
    {
        bool allowed = permissionKind.ToLowerInvariant() switch
        {
            "camera" => profile.AllowCamera,
            "microphone" => profile.AllowMicrophone,
            "geolocation" => false, // Geolocation dilarang untuk ujian
            "notifications" => false, // Notifikasi desktop dilarang
            "clipboard-read" => profile.AllowClipboard && profile.AllowPaste,
            "clipboard-write" => profile.AllowClipboard && profile.AllowCopy,
            _ => false
        };

        if (!allowed)
        {
            _logger.LogWarning(AuditEventType.PermissionBlocked, $"Permintaan izin browser '{permissionKind}' diblokir.", $"Profil: {profile.ExamName}");
        }

        return allowed;
    }
}
