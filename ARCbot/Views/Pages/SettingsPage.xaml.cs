using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ARCbot.ViewModels;

namespace ARCbot.Views.Pages;

public partial class SettingsPage : Page
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        DataContext = _viewModel;

        // 监听下载状态，控制 ProgressBar 可见性
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.IsDownloading))
            {
                NodeProgressBar.Visibility = _viewModel.IsDownloading
                    ? Visibility.Visible : Visibility.Collapsed;
                DownloadNodeBtn.IsEnabled = !_viewModel.IsDownloading;
            }
            if (e.PropertyName == nameof(SettingsViewModel.IsDownloadingBaseAgent))
            {
                BaseProgressBar.Visibility = _viewModel.IsDownloadingBaseAgent
                    ? Visibility.Visible : Visibility.Collapsed;
                DownloadBaseBtn.IsEnabled = !_viewModel.IsDownloadingBaseAgent;
            }
        };
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.RefreshStatusCommand.Execute(null);
    }
}
