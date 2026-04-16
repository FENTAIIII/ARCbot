using ARCbot.Services;
using ARCbot.Views.Pages;
using ARCbot.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace ARCbot;

public partial class MainWindow : FluentWindow
{
    private readonly AuthInterceptor _authInterceptor;
    private string _currentAuthUrl = string.Empty;
    private string _currentAuthCode = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        _authInterceptor = App.Services.GetRequiredService<AuthInterceptor>();
        _authInterceptor.AuthCodeDetected += OnAuthCodeDetected;

        var pageService = new PageService();
        RootNavigation.SetPageService(pageService);

        Loaded += (_, _) =>
        {
            RootNavigation.Navigate(typeof(HomePage));
            ShowAfdianPopupIfNeeded();
        };
    }

    private void ShowAfdianPopupIfNeeded()
    {
        var settingsService = App.Services.GetRequiredService<SettingsService>();
        if (settingsService.Settings.DisableAfdianPopup) return;

        AfdianOverlay.Visibility = Visibility.Visible;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
        {
            BeginTime = TimeSpan.FromMilliseconds(1000), // 延迟1秒弹出，体验更好
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        AfdianOverlay.BeginAnimation(OpacityProperty, fadeIn);

        var scaleX = new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(600))
        {
            BeginTime = TimeSpan.FromMilliseconds(1000),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var scaleY = new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(600))
        {
            BeginTime = TimeSpan.FromMilliseconds(1000),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        AfdianDialogScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleX);
        AfdianDialogScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleY);
    }

    private void AfdianLink_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://ifdian.net/a/fentai2333");
        CloseAfdianOverlay();
    }

    private void GithubLink_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/FENTAIIII");
        CloseAfdianOverlay();
    }

    private void AfdianClose_Click(object sender, RoutedEventArgs e)
    {
        CloseAfdianOverlay();
    }

    private void CloseAfdianOverlay()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            AfdianOverlay.Visibility = Visibility.Collapsed;
        };
        AfdianOverlay.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"无法打开链接: {ex.Message}");
        }
    }

    private void OnAuthCodeDetected(object? sender, AuthCodeEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _currentAuthUrl = e.Url;
            _currentAuthCode = e.Code;

            AuthInstanceLabel.Text = $"来自实例: {e.InstanceName}";
            AuthCodeText.Text = e.Code;

            AuthOverlay.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            AuthOverlay.BeginAnimation(OpacityProperty, fadeIn);

            var scaleX = new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var scaleY = new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            AuthDialogScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleX);
            AuthDialogScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleY);

            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        });
    }
    // 在 MainWindow 类中添加/修改以下方法：

    public void UpdateBackgroundOverlayOpacity(double opacity)
    {
        BackgroundOverlay.Opacity = opacity;
    }

    public void UpdateBackground(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            BackgroundImage.Source = null;
            WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.Mica; // 恢复默认材质
            BackgroundOverlay.Visibility = Visibility.Collapsed;
            RootNavigation.Background = null; // 恢复导航栏默认背景
        }
        else
        {
            try
            {
                BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.None; // 禁用 Mica 以显示图片
                BackgroundOverlay.Visibility = Visibility.Visible;
                RootNavigation.Background = System.Windows.Media.Brushes.Transparent; // 强制导航栏透明，透出底图
            }
            catch (Exception)
            {
                // 图片加载失败时回退
                BackgroundImage.Source = null;
                WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.Mica;
                BackgroundOverlay.Visibility = Visibility.Collapsed;
                RootNavigation.Background = null;
            }
        }
    }

    public void NavigateToConsole(string instanceName)
    {
        // 导航到控制台页面
        RootNavigation.Navigate(typeof(ConsolePage));

        // 获取 ConsoleViewModel 并选中指定实例
        var consoleViewModel = App.Services.GetRequiredService<ConsoleViewModel>();
        consoleViewModel.FocusInstance(instanceName);
    }

    private void AuthCopyAndOpen_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(_currentAuthCode);
            Process.Start(new ProcessStartInfo
            {
                FileName = _currentAuthUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // ★ 修复: 使用完全限定名避免歧义
            System.Windows.MessageBox.Show(
                $"操作失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        CloseAuthOverlay();
    }

    private void AuthClose_Click(object sender, RoutedEventArgs e)
    {
        CloseAuthOverlay();
    }

    private void CloseAuthOverlay()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            AuthOverlay.Visibility = Visibility.Collapsed;
        };
        AuthOverlay.BeginAnimation(OpacityProperty, fadeOut);
    }

    protected override void OnClosed(EventArgs e)
    {
        _authInterceptor.AuthCodeDetected -= OnAuthCodeDetected;
        base.OnClosed(e);
    }
}

/// <summary>
/// 简易页面服务，为 NavigationView 提供页面实例解析。
/// </summary>
public class PageService : Wpf.Ui.IPageService
{
    public T? GetPage<T>() where T : class
    {
        return GetPage(typeof(T)) as T;
    }

    public FrameworkElement? GetPage(Type pageType)
    {
        try
        {
            return App.Services.GetService(pageType) as FrameworkElement
                   ?? Activator.CreateInstance(pageType) as FrameworkElement;
        }
        catch
        {
            return Activator.CreateInstance(pageType) as FrameworkElement;
        }
    }
}