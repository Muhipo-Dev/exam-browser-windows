using System.Windows.Input;
using ExambroMuhipo.Services;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.ViewModels;

/// <summary>
/// ViewModel untuk Halaman Tentang (AboutView).
/// Menyediakan informasi resmi versi, institusi, arsitektur, dan pernyataan keterbatasan keamanan.
/// </summary>
public class AboutViewModel : ViewModelBase
{
    private readonly IApplicationStateManager _stateManager;
    private readonly IConfigurationService _configService;

    public string AppTitle => $"Exambro-Muhipo v{_configService.Config.AppVersion}";
    public string AppSubtitle => "Secure Examination Browser";
    public string Institution => _configService.Config.SchoolName;
    public string DeveloperCredit => "Developed by Muhipo Dev";
    public string VersionText => $"Versi {_configService.Config.AppVersion} (Build 2026.3)";
    public string TechStackInfo => "C# .NET 8 (LTS) • Windows Presentation Foundation (WPF) • Multi-Engine Browser (Microsoft Edge, Google Chrome & Web Bawaan Windows) • x64 Architecture";

    public string SecurityNotice => 
        "Pemberitahuan Keamanan & Batasan Sistem:\n" +
        "Exambro-Muhipo dirancang sebagai Secure Examination Environment untuk menunjang integritas dan fokus " +
        "peserta didik selama pelaksanaan ujian berbasis komputer sekolah. Sistem mengimplementasikan isolasi navigasi " +
        "whitelist, restriksi tombol sistem keyboard hook (User32), dan penonaktifan inspeksi developer tools.\n\n" +
        "PERHATIAN: Sesuai prinsip arsitektur sistem operasi modern, aplikasi pengguna (user-mode) pada Windows tidak dapat " +
        "menjamin lockdown absolut 100% terhadap seluruh kemungkinan intervensi fisik atau akses administratif lokal tingkat kernel. " +
        "Oleh karena itu, Exambro-Muhipo tidak mengklaim sebagai solusi anti-cheat absolut yang mustahil dibypass. Integritas ujian " +
        "tetap memerlukan perpaduan antara keandalan sistem perangkat lunak dan pengawasan proktor ruang secara profesional.";

    public ICommand BackToHomeCommand { get; }

    public AboutViewModel(IApplicationStateManager stateManager, IConfigurationService configService)
    {
        _stateManager = stateManager;
        _configService = configService;
        BackToHomeCommand = new RelayCommand(() => _stateManager.NavigateToHome());
    }
}
