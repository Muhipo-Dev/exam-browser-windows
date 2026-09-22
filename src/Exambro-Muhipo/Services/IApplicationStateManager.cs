using System;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.Services;

/// <summary>
/// Jenis tampilan aktif dalam aplikasi.
/// </summary>
public enum AppViewType
{
    Home,
    Exam,
    Admin,
    Settings,
    About,
    Help
}

/// <summary>
/// Kontrak manajemen navigasi state global aplikasi.
/// </summary>
public interface IApplicationStateManager
{
    AppViewType CurrentViewType { get; }
    ViewModelBase? CurrentViewModel { get; }

    event Action<AppViewType, ViewModelBase?>? ViewChanged;

    void NavigateToHome();
    void NavigateToExam();
    void NavigateToAdmin();
    void NavigateToSettings();
    void NavigateToAbout();
    void NavigateToHelp();
    void SetCurrentView(AppViewType viewType, ViewModelBase viewModel);
}
