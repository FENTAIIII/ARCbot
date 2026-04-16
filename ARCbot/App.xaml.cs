using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ARCbot.Helpers;
using ARCbot.Services;
using ARCbot.ViewModels;
using ARCbot.Views.Pages;
using ARCbot.Views.Dialogs;
using Wpf.Ui.Appearance; // 引入 WPF UI 外观命名空间

namespace ARCbot;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        PathHelper.EnsureDirectories();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        // 加载并应用设置
        var settingsService = Services.GetRequiredService<SettingsService>();
        var settings = settingsService.Settings;

        // 应用主题
        ApplicationThemeManager.Apply(
            settings.IsDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light
        );

        // 应用全局透明度
        Application.Current.Resources["GlobalOverlayOpacity"] = settings.BgOpacity;

        var mainWindow = Services.GetRequiredService<MainWindow>();

        // 应用背景图
        mainWindow.Loaded += async (s, ev) =>
        {
            mainWindow.UpdateBackground(settings.BackgroundImagePath);

            // 检查更新
            var updateService = Services.GetRequiredService<UpdateService>();
            await updateService.CheckUpdateAsync();
        };

        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // ─── 单例服务 ───
        services.AddSingleton<AuthInterceptor>();
        services.AddSingleton<EnvManager>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<InstanceManager>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<UpdateService>();

        // ─── ViewModels ───
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<CreateInstanceViewModel>();
        services.AddSingleton<ConsoleViewModel>();
        services.AddTransient<PluginsViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // ─── Pages（注册到 DI 以便 PageService 解析） ───
        services.AddTransient<HomePage>();
        services.AddTransient<ConsolePage>();
        services.AddTransient<PluginsPage>();
        services.AddTransient<MarketPage>();
        services.AddTransient<ExperiencePage>();
        services.AddTransient<DocsPage>();
        services.AddSingleton<SettingsPage>();

        // ─── 窗口 ───
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
        {
            // 停止所有运行中的进程
            var instanceManager = Services.GetRequiredService<InstanceManager>();
            foreach (var pm in instanceManager.RunningProcesses.Values)
            {
                pm.Dispose();
            }
        }

        base.OnExit(e);
    }
}
