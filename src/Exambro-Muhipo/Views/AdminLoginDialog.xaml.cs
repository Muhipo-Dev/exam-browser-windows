using System.Windows;
using System.Windows.Input;
using ExambroMuhipo.Services;

namespace ExambroMuhipo.Views;

public partial class AdminLoginDialog : Window
{
    private readonly IConfigurationService _configService;
    private readonly IAlarmService? _alarmService;
    private readonly Func<string, bool>? _customValidator;

    public bool IsAuthenticated { get; private set; }

    public AdminLoginDialog(
        IConfigurationService configService, 
        string title = "Autentikasi Administrator", 
        string message = "Masukkan password administrator untuk melanjutkan tindakan ini.",
        IAlarmService? alarmService = null,
        Func<string, bool>? customValidator = null)
    {
        InitializeComponent();
        _configService = configService;
        _alarmService = alarmService;
        _customValidator = customValidator;

        DialogTitleText.Text = title;
        DescriptionText.Text = message;

        Loaded += (s, e) => PasswordInput.Focus();
    }

    private void PasswordInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            e.Handled = true;
            ValidateAndConfirm();
        }
    }

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            e.Handled = true;
            ValidateAndConfirm();
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        ValidateAndConfirm();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsAuthenticated = false;
        DialogResult = false;
        Close();
    }


    private void ValidateAndConfirm()
    {
        string entered = PasswordInput.Password;
        if (string.IsNullOrEmpty(entered))
        {
            _alarmService?.PlaySecurityBuzzer();
            ShowError("Silakan masukkan password.");
            return;
        }

        bool isAuth = _customValidator != null 
            ? _customValidator(entered) 
            : _configService.VerifyAdminPassword(entered);

        if (isAuth)
        {
            IsAuthenticated = true;
            DialogResult = true;
            Close();
        }
        else
        {
            _alarmService?.PlaySecurityBuzzer();
            ShowError("Password salah. Silakan coba kembali.");
            PasswordInput.SelectAll();
            PasswordInput.Focus();
        }
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}
