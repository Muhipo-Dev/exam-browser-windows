using System.Windows;
using System.Windows.Controls;
using ExambroMuhipo.Services;
using ExambroMuhipo.ViewModels;

namespace ExambroMuhipo.Views;

public partial class ExamView : UserControl
{
    private bool _isInitialized;

    public ExamView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var app = Application.Current as App;
        if (app == null)
            return;

        if (_isInitialized)
        {
            // Jika view dimuat kembali, pastikan StartUrl dimuat jika belum
            string url = app.ConfigService.ActiveProfile.StartUrl;
            if (!string.IsNullOrWhiteSpace(url))
            {
                app.BrowserService.Navigate(url);
            }
            return;
        }

        if (DataContext is ExamViewModel)
        {
            await ExamWebViewHost.InitializeAsync(app.BrowserService, app.ConfigService.ActiveProfile);
            _isInitialized = true;

            // Pastikan navigasi ke StartUrl dipanggil setelah host siap
            string startUrl = app.ConfigService.ActiveProfile.StartUrl;
            if (!string.IsNullOrWhiteSpace(startUrl))
            {
                app.BrowserService.Navigate(startUrl);
            }
        }
    }
}
