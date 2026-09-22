using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ExambroMuhipo.ViewModels;

namespace ExambroMuhipo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void CustomTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.IsExamModeActive)
        {
            // Cegah penutupan aplikasi langsung saat ujian aktif tanpa otorisasi pengawas
            e.Cancel = true;
            _ = mainVm.ExamVM.OnRequestExitExamAsync();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.IsExamModeActive)
        {
            // Cegah F11 mengubah mode layar secara tidak sah
            if (e.Key == Key.F11)
            {
                e.Handled = true;
            }

            // Tangkap Alt + F4 di level WPF preview jika hook dilewati
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
                _ = mainVm.ExamVM.OnRequestExitExamAsync();
            }
        }
    }
}
