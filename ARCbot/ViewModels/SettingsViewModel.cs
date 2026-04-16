using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using System.Threading.Tasks;
using ARCbot;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Appearance;
using ARCbot.Helpers;
using ARCbot.Services;

namespace ARCbot.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DownloadService _downloadService;
    private readonly SettingsService _settingsService;

    // ─── 个性化设置 ───

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private double _bgOpacity;

    [ObservableProperty]
    private string _backgroundImagePath;

    public ObservableCollection<string> PresetImages { get; } = new()
    {
        "", // 默认无背景
        "pack://application:,,,/Assets/bg1.png", // 请确保在项目中创建 Assets 文件夹并放入这些图片，生成操作设为 Resource
        "pack://application:,,,/Assets/bg2.png",
        "pack://application:,,,/Assets/bg3.png",
        "pack://application:,,,/Assets/bg4.png",
        "pack://application:,,,/Assets/bg5.png",
        "pack://application:,,,/Assets/bg6.png"
    };

    // ─── Node.js 运行时 ───
    [ObservableProperty]
    private bool _isNodeInstalled;

    [ObservableProperty]
    private bool _isDownloading = false;

    [ObservableProperty]
    private double _downloadProgress = 0;

    [ObservableProperty]
    private string _downloadStatusText = string.Empty;

    // ─── 基础包 ───
    [ObservableProperty]
    private bool _isBaseAgentDownloaded;

    [ObservableProperty]
    private bool _isDownloadingBaseAgent = false;

    [ObservableProperty]
    private double _baseAgentProgress = 0;

    [ObservableProperty]
    private string _baseAgentStatusText = string.Empty;

    // ─── 自定义基础包 ───
    [ObservableProperty]
    private bool _useCustomBaseAgent;

    [ObservableProperty]
    private string _customBaseAgentPath = string.Empty;

    [ObservableProperty]
    private bool _isCustomBaseAgentImported;

    // ─── 弹窗设置 ───
    [ObservableProperty]
    private bool _disableAfdianPopup;

    // ─── 关于 ───
    public string AppVersion => "1.0.3";
    public string DotNetVersion => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
    public string AppDataPath => PathHelper.RootDir;

    public SettingsViewModel(DownloadService downloadService, SettingsService settingsService)
    {
        _downloadService = downloadService;
        _settingsService = settingsService;

        // 从设置服务初始化
        var settings = _settingsService.Settings;
        _isDarkMode = settings.IsDarkMode;
        _bgOpacity = settings.BgOpacity;
        _backgroundImagePath = settings.BackgroundImagePath;
        _useCustomBaseAgent = settings.UseCustomBaseAgent;
        _customBaseAgentPath = settings.CustomBaseAgentPath;
        _disableAfdianPopup = settings.DisableAfdianPopup;

        RefreshStatus();
    }

    partial void OnDisableAfdianPopupChanged(bool value)
    {
        _settingsService.UpdateSettings(s => s.DisableAfdianPopup = value);
    }

    [RelayCommand]
    private void OpenAfdian()
    {
        OpenUrl("https://ifdian.net/a/fentai2333");
    }

    [RelayCommand]
    private void OpenGithub()
    {
        OpenUrl("https://github.com/FENTAIIII");
    }

    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接: {ex.Message}");
        }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        ApplicationThemeManager.Apply(
            value ? ApplicationTheme.Dark : ApplicationTheme.Light
        );
        _settingsService.UpdateSettings(s => s.IsDarkMode = value);
    }

    partial void OnBgOpacityChanged(double value)
    {
        // 更新全局透明度资源，所有使用 GlobalOverlayOpacity 的控件都会自动更新
        Application.Current.Resources["GlobalOverlayOpacity"] = value;
        _settingsService.UpdateSettings(s => s.BgOpacity = value);
    }

    partial void OnBackgroundImagePathChanged(string value)
    {
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.UpdateBackground(value);
        }
        _settingsService.UpdateSettings(s => s.BackgroundImagePath = value);
    }

    partial void OnUseCustomBaseAgentChanged(bool value)
    {
        _settingsService.UpdateSettings(s => s.UseCustomBaseAgent = value);
    }

    [RelayCommand]
    private void SelectCustomImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "选择背景图片"
        };

        if (dialog.ShowDialog() == true)
        {
            BackgroundImagePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void ImportCustomBaseAgent()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "压缩文件|*.zip",
            Title = "选择自定义基础包"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.Copy(dialog.FileName, PathHelper.CustomBaseAgentZipPath, true);
                CustomBaseAgentPath = dialog.FileName;
                _settingsService.UpdateSettings(s => s.CustomBaseAgentPath = dialog.FileName);
                RefreshStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void RefreshStatus()
    {
        IsNodeInstalled = _downloadService.IsNodeInstalled();
        IsBaseAgentDownloaded = _downloadService.IsBaseAgentDownloaded();
        IsCustomBaseAgentImported = File.Exists(PathHelper.CustomBaseAgentZipPath);

        DownloadStatusText = IsNodeInstalled ? "Node.js 运行时已就绪" : "未安装 Node.js 运行时";
        BaseAgentStatusText = IsBaseAgentDownloaded ? "基础包已下载" : "基础包未下载";
    }
    /// <summary>下载 Node.js 运行时</summary>
    [RelayCommand]
    private async Task DownloadNodeAsync()
    {
        if (IsDownloading) return;
        IsDownloading = true;
        DownloadProgress = 0;
        DownloadStatusText = "正在下载 Node.js 运行时...";

        try
        {
            var progress = new Progress<(long downloaded, long total)>(p =>
            {
                if (p.total > 0)
                {
                    DownloadProgress = (double)p.downloaded / p.total * 100;
                    DownloadStatusText = $"下载中... {p.downloaded / 1024 / 1024}MB / {p.total / 1024 / 1024}MB ({DownloadProgress:F1}%)";
                }
                else
                {
                    DownloadStatusText = $"下载中... {p.downloaded / 1024 / 1024}MB";
                }
            });

            await _downloadService.DownloadNodeRuntimeAsync(progress);

            IsNodeInstalled = true;
            DownloadStatusText = "Node.js 运行时下载完成！"; // 去除 Emoji
        }
        catch (Exception ex)
        {
            DownloadStatusText = $"下载失败: {ex.Message}"; // 去除 Emoji
        }
        finally
        {
            IsDownloading = false;
        }
    }

    /// <summary>下载机器人基础包</summary>
    [RelayCommand]
    private async Task DownloadBaseAgentAsync()
    {
        if (IsDownloadingBaseAgent) return;
        IsDownloadingBaseAgent = true;
        BaseAgentProgress = 0;
        BaseAgentStatusText = "正在下载基础包...";

        try
        {
            var progress = new Progress<(long downloaded, long total)>(p =>
            {
                if (p.total > 0)
                {
                    BaseAgentProgress = (double)p.downloaded / p.total * 100;
                    BaseAgentStatusText = $"下载中... {p.downloaded / 1024 / 1024}MB / {p.total / 1024 / 1024}MB ({BaseAgentProgress:F1}%)";
                }
                else
                {
                    BaseAgentStatusText = $"下载中... {p.downloaded / 1024}KB";
                }
            });

            await _downloadService.DownloadBaseAgentAsync(progress);

            IsBaseAgentDownloaded = true;
            BaseAgentStatusText = "基础包下载完成！"; // 去除 Emoji
        }
        catch (Exception ex)
        {
            BaseAgentStatusText = $"下载失败: {ex.Message}"; // 去除 Emoji
        }
        finally
        {
            IsDownloadingBaseAgent = false;
        }
    }



    /// <summary>打开 AppData 目录</summary>
    [RelayCommand]
    private void OpenAppDataFolder()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = PathHelper.RootDir,
            UseShellExecute = true
        });
    }
}
