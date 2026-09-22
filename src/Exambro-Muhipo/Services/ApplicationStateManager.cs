using System;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi ApplicationStateManager untuk koordinasi perpindahan view utama.
/// </summary>
public class ApplicationStateManager : IApplicationStateManager
{
    private readonly ILoggingService _logger;

    public AppViewType CurrentViewType { get; private set; } = AppViewType.Home;
    public ViewModelBase? CurrentViewModel { get; private set; }

    public event Action<AppViewType, ViewModelBase?>? ViewChanged;

    public ApplicationStateManager(ILoggingService logger)
    {
        _logger = logger;
    }

    public void NavigateToHome()
    {
        CurrentViewType = AppViewType.Home;
        ViewChanged?.Invoke(AppViewType.Home, null);
    }

    public void NavigateToExam()
    {
        CurrentViewType = AppViewType.Exam;
        ViewChanged?.Invoke(AppViewType.Exam, null);
    }

    public void NavigateToAdmin()
    {
        CurrentViewType = AppViewType.Admin;
        ViewChanged?.Invoke(AppViewType.Admin, null);
    }

    public void NavigateToSettings()
    {
        CurrentViewType = AppViewType.Settings;
        ViewChanged?.Invoke(AppViewType.Settings, null);
    }

    public void NavigateToAbout()
    {
        CurrentViewType = AppViewType.About;
        ViewChanged?.Invoke(AppViewType.About, null);
    }

    public void NavigateToHelp()
    {
        CurrentViewType = AppViewType.Help;
        ViewChanged?.Invoke(AppViewType.Help, null);
    }

    public void SetCurrentView(AppViewType viewType, ViewModelBase viewModel)
    {
        CurrentViewType = viewType;
        CurrentViewModel = viewModel;
        ViewChanged?.Invoke(viewType, viewModel);
    }
}
