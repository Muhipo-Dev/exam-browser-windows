using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Kontrak layanan penegakan seluruh kebijakan keamanan (Security Policy).
/// </summary>
public interface ISecurityPolicyService
{
    NavigationCheckResult CheckNavigation(string url, ExamProfile profile);
    bool CanOpenNewWindow(ExamProfile profile, string targetUrl);
    bool CanShowPopup(ExamProfile profile, string targetUrl);
    bool CanDownload(ExamProfile profile, string uri);
    bool CanUseClipboard(ExamProfile profile);
    bool CanPrint(ExamProfile profile);
    bool CanUseDevTools(ExamProfile profile);
    bool CanUseContextMenu(ExamProfile profile);
    bool CanUseZoom(ExamProfile profile);
    bool CanGrantPermission(ExamProfile profile, string permissionKind);
}
