namespace ExambroMuhipo.Models;

/// <summary>
/// Hasil evaluasi keputusan navigasi browser.
/// </summary>
public enum NavigationDecisionType
{
    Allowed,
    BlockedDomain,
    BlockedProtocol,
    BlockedMalformedUri,
    BlockedNonHttps,
    BlockedPattern
}

/// <summary>
/// Detail hasil verifikasi kebijakan navigasi.
/// </summary>
public class NavigationCheckResult
{
    public bool IsAllowed => Decision == NavigationDecisionType.Allowed;
    public NavigationDecisionType Decision { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string NormalizedUrl { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;

    public static NavigationCheckResult Allow(string url, string hostname) =>
        new()
        {
            Decision = NavigationDecisionType.Allowed,
            Reason = "Navigasi diizinkan oleh whitelist.",
            NormalizedUrl = url,
            Hostname = hostname
        };

    public static NavigationCheckResult Block(NavigationDecisionType decision, string reason, string url, string hostname = "") =>
        new()
        {
            Decision = decision,
            Reason = reason,
            NormalizedUrl = url,
            Hostname = hostname
        };
}
