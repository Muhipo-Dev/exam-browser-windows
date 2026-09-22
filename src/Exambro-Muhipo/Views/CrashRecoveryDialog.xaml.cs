using System.Windows;
using ExambroMuhipo.Services;

namespace ExambroMuhipo.Views;

public partial class CrashRecoveryDialog : Window
{
    private readonly IConfigurationService _configService;

    public bool ShouldRestartSession { get; private set; }
    public bool ShouldExitToNormal { get; private set; }

    public CrashRecoveryDialog(IConfigurationService configService, string? customMessage = null)
    {
        InitializeComponent();
        _configService = configService;

        if (!string.IsNullOrEmpty(customMessage))
        {
            MessageText.Text = customMessage;
        }
    }

    private void RestartSession_Click(object sender, RoutedEventArgs e)
    {
        ShouldRestartSession = true;
        DialogResult = true;
        Close();
    }

    private void AdminPasswordInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
        {
            e.Handled = true;
            UnlockAndExit_Click(sender, e);
        }
    }

    private void AdminPasswordInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
        {
            e.Handled = true;
            UnlockAndExit_Click(sender, e);
        }
    }

    private void UnlockAndExit_Click(object sender, RoutedEventArgs e)
    {
        string pwd = AdminPasswordInput.Password;
        if (string.IsNullOrEmpty(pwd))
        {
            ErrorText.Text = "Masukkan password administrator/pengawas.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (_configService.VerifyAdminPassword(pwd))
        {
            ShouldExitToNormal = true;
            DialogResult = true;
            Close();
        }
        else
        {
            ErrorText.Text = "Password administrator salah.";
            ErrorText.Visibility = Visibility.Visible;
            AdminPasswordInput.SelectAll();
            AdminPasswordInput.Focus();
        }
    }
}
