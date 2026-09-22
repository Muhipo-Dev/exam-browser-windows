using System;
using System.Collections.Generic;
using System.Linq;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi penegakan whitelist domain, whitelist URL, protokol eksternal, dan HTTPS enforcement.
/// </summary>
public class NavigationPolicy : INavigationPolicy
{
    private static readonly HashSet<string> DangerousProtocols = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailto",
        "tel",
        "file",
        "ms-settings",
        "ms-windows-store",
        "powershell",
        "cmd",
        "javascript",
        "vbscript",
        "ftp",
        "sftp",
        "smb",
        "ldap",
        "calculator",
        "explorer"
    };

    public NavigationCheckResult EvaluateNavigation(string targetUrl, ExamProfile profile)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return NavigationCheckResult.Block(NavigationDecisionType.BlockedMalformedUri, "Target URL kosong atau tidak terdefinisi.", targetUrl);
        }

        string trimmedUrl = targetUrl.Trim();

        // 1. Izinkan about: (about:blank, about:srcdoc)
        if (trimmedUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            return NavigationCheckResult.Allow(trimmedUrl, "about");
        }

        // 2. Cek protokol berbahaya eksternal Windows (seperti cmd:, powershell:, javascript:, mailto:, tel:, dll.)
        int colonIdx = trimmedUrl.IndexOf(':');
        if (colonIdx > 0)
        {
            string potentialScheme = trimmedUrl.Substring(0, colonIdx).Trim().ToLowerInvariant();
            if (IsDangerousProtocol(potentialScheme))
            {
                return NavigationCheckResult.Block(NavigationDecisionType.BlockedProtocol, $"Protokol eksternal '{potentialScheme}:' diblokir oleh kebijakan keamanan.", targetUrl);
            }
        }

        // 3. Parsing URL (atau coba dengan http:// jika tanpa skema)
        Uri? uri;
        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out uri))
        {
            if (trimmedUrl.Contains("://") || !Uri.TryCreate("http://" + trimmedUrl, UriKind.Absolute, out uri))
            {
                return NavigationCheckResult.Block(NavigationDecisionType.BlockedMalformedUri, "Format URL tidak valid atau bukan URL absolut.", targetUrl);
            }
        }

        string scheme = uri.Scheme.ToLowerInvariant();

        // 4. Izinkan protokol web standar: data:, blob:, http:, https:
        if (scheme == "data" || scheme == "blob")
        {
            return NavigationCheckResult.Allow(targetUrl, scheme);
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return NavigationCheckResult.Block(NavigationDecisionType.BlockedMalformedUri, "Format URL tidak valid atau host tidak ditemukan.", targetUrl);
        }

        if (scheme != Uri.UriSchemeHttp && scheme != Uri.UriSchemeHttps)
        {
            return NavigationCheckResult.Block(NavigationDecisionType.BlockedProtocol, $"Protokol '{scheme}:' tidak didukung untuk sesi ujian.", targetUrl);
        }

        // 5. HTTPS enforcement (Kecuali alamat IP lokal / LAN jika AllowLocalIpHttp aktif)
        bool isLocalIp = ExamProfile.IsLocalOrPrivateIp(uri.Host);
        if (profile.EnforceHttps && scheme != Uri.UriSchemeHttps)
        {
            if (!(profile.AllowLocalIpHttp && isLocalIp))
            {
                return NavigationCheckResult.Block(NavigationDecisionType.BlockedNonHttps, "Koneksi HTTP tidak terenkripsi dilarang untuk domain publik. Wajib menggunakan HTTPS.", targetUrl, uri.Host);
            }
        }

        // 6. Auto-allow jika host atau authority sama dengan StartUrl ujian
        string normalizedStartUrl = profile.StartUrl?.Trim() ?? string.Empty;
        if (!normalizedStartUrl.Contains("://") && !string.IsNullOrEmpty(normalizedStartUrl))
        {
            normalizedStartUrl = "http://" + normalizedStartUrl;
        }

        if (Uri.TryCreate(normalizedStartUrl, UriKind.Absolute, out var startUri))
        {
            if (string.Equals(uri.Host, startUri.Host, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Authority, startUri.Authority, StringComparison.OrdinalIgnoreCase))
            {
                return NavigationCheckResult.Allow(targetUrl, uri.Host);
            }
        }

        // 7. Auto-allow semua IP lokal jika AllowLocalIpHttp diaktifkan
        if (profile.AllowLocalIpHttp && isLocalIp)
        {
            return NavigationCheckResult.Allow(targetUrl, uri.Host);
        }

        // 8. Cek Whitelist URL spesifik
        if (profile.AllowedUrls != null && profile.AllowedUrls.Count > 0 && IsUrlAllowed(targetUrl, profile.AllowedUrls))
        {
            return NavigationCheckResult.Allow(targetUrl, uri.Host);
        }

        // 9. Cek Whitelist Domain / Hostname / IP (termasuk authority dengan port)
        if (profile.AllowedDomains != null && profile.AllowedDomains.Count > 0 && 
            (IsDomainAllowed(uri.Host, profile.AllowedDomains) || IsDomainAllowed(uri.Authority, profile.AllowedDomains)))
        {
            return NavigationCheckResult.Allow(targetUrl, uri.Host);
        }

        return NavigationCheckResult.Block(NavigationDecisionType.BlockedDomain, $"Alamat '{uri.Authority}' tidak terdaftar dalam whitelist ujian.", targetUrl, uri.Host);
    }

    public bool IsDangerousProtocol(string scheme)
    {
        return DangerousProtocols.Contains(scheme);
    }

    public bool IsDomainAllowed(string hostname, IEnumerable<string> allowedDomains)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return false;

        string normalizedHost = hostname.Trim().ToLowerInvariant();

        foreach (var pattern in allowedDomains)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            string normalizedPattern = pattern.Trim().ToLowerInvariant();

            // Exact match
            if (string.Equals(normalizedHost, normalizedPattern, StringComparison.OrdinalIgnoreCase))
                return true;

            // Wildcard subdomain: *.example.com
            if (normalizedPattern.StartsWith("*."))
            {
                string baseDomain = normalizedPattern.Substring(2);
                if (normalizedHost.EndsWith("." + baseDomain, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedHost, baseDomain, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            // Domain with implied subdomains
            else if (normalizedHost.EndsWith("." + normalizedPattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsUrlAllowed(string targetUrl, IEnumerable<string> allowedUrls)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
            return false;

        string normalizedTarget = targetUrl.Trim();

        foreach (var pattern in allowedUrls)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            string normalizedPattern = pattern.Trim();

            // Exact match
            if (string.Equals(normalizedTarget, normalizedPattern, StringComparison.OrdinalIgnoreCase))
                return true;

            // Prefix match jika pola diakhiri dengan '*'
            if (normalizedPattern.EndsWith("*"))
            {
                string prefix = normalizedPattern.Substring(0, normalizedPattern.Length - 1);
                if (normalizedTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
