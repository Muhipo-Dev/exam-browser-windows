using System.Windows.Input;
using ExambroMuhipo.Services;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.ViewModels;

/// <summary>
/// ViewModel untuk Pengaturan Umum Aplikasi (SettingsView).
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly IApplicationStateManager _stateManager;

    public string SchoolName
    {
        get => _configService.Config.SchoolName;
        set
        {
            _configService.Config.SchoolName = value;
            OnPropertyChanged();
        }
    }

    public bool EnableDetailedAuditLog
    {
        get => _configService.Config.EnableDetailedAuditLog;
        set
        {
            _configService.Config.EnableDetailedAuditLog = value;
            OnPropertyChanged();
        }
    }

    public int LogRetentionDays
    {
        get => _configService.Config.LogRetentionDays;
        set
        {
            _configService.Config.LogRetentionDays = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveSettingsCommand { get; }
    public ICommand BackToHomeCommand { get; }

    public SettingsViewModel(IConfigurationService configService, IApplicationStateManager stateManager)
    {
        _configService = configService;
        _stateManager = stateManager;

        SaveSettingsCommand = new RelayCommand(OnSaveSettings);
        BackToHomeCommand = new RelayCommand(() => _stateManager.NavigateToHome());
    }

    private void OnSaveSettings()
    {
        _configService.SaveConfiguration();
        _stateManager.NavigateToHome();
    }
}
