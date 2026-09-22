using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Kontrak evaluasi kebijakan navigasi dan whitelist URL/Domain.
/// </summary>
public interface INavigationPolicy
{
    NavigationCheckResult EvaluateNavigation(string targetUrl, ExamProfile profile);
    bool IsDomainAllowed(string hostname, System.Collections.Generic.IEnumerable<string> allowedDomains);
    bool IsUrlAllowed(string targetUrl, System.Collections.Generic.IEnumerable<string> allowedUrls);
    bool IsDangerousProtocol(string scheme);
}
