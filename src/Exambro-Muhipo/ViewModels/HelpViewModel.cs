using System.Windows.Input;
using ExambroMuhipo.Services;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.ViewModels;

/// <summary>
/// ViewModel untuk Halaman Bantuan & Panduan (HelpView).
/// </summary>
public class HelpViewModel : ViewModelBase
{
    private readonly IApplicationStateManager _stateManager;

    public ICommand BackToHomeCommand { get; }

    public HelpViewModel(IApplicationStateManager stateManager)
    {
        _stateManager = stateManager;
        BackToHomeCommand = new RelayCommand(() => _stateManager.NavigateToHome());
    }
}
