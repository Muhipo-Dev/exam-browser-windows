using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;

namespace ExambroMuhipo.Views.Controls;

public partial class WebViewHost : UserControl
{
    private IBrowserService? _browserService;

    public WebViewHost()
    {
        InitializeComponent();
    }

    public async Task InitializeAsync(IBrowserService browserService, ExamProfile profile)
    {
        _browserService = browserService;
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            await _browserService.InitializeAsync(Wv2BrowserControl, NativeBrowserControl, profile);

            if (_browserService.IsUsingNativeFallback)
            {
                Wv2BrowserControl.Visibility = Visibility.Collapsed;
                NativeBrowserControl.Visibility = Visibility.Visible;
            }
            else
            {
                Wv2BrowserControl.Visibility = Visibility.Visible;
                NativeBrowserControl.Visibility = Visibility.Collapsed;
            }
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    public void ShowLoading(bool show)
    {
        LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }
}
