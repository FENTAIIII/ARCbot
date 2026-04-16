using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using ARCbot.Models;

namespace ARCbot.Views.Dialogs;

public partial class UpdateDialog : Window
{
    // 对外暴露用户是否勾选了"不再提醒"
    public bool IsSkipChecked => SkipCheckBox.IsChecked == true;

    // 下载是否成功
    public bool IsDownloadSuccessful { get; private set; }

    private readonly UpdateInfo _updateInfo;
    private readonly HttpClient _httpClient;

    public UpdateDialog(UpdateInfo info)
    {
        InitializeComponent();
        _updateInfo = info;
        TitleText.Text = $"发现新版本：v{info.Version}";
        ReleaseNotesBox.Text = info.ReleaseNotes;
        _httpClient = new HttpClient();
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        // 禁用按钮防止重复点击
        UpdateButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        SkipCheckBox.IsEnabled = false;

        // 显示进度条
        DownloadProgressGrid.Visibility = Visibility.Visible;
        DownloadStatusText.Text = "正在下载更新...";

        // 开始下载
        bool success = await DownloadUpdateAsync();

        if (success)
        {
            DownloadStatusText.Text = "下载完成！正在安装...";
            DownloadProgressBar.Value = 100;

            // 安装更新
            InstallUpdate();

            // 返回 true 表示更新成功
            IsDownloadSuccessful = true;
            this.DialogResult = true;
            this.Close();
        }
        else
        {
            DownloadStatusText.Text = "下载失败，请检查网络连接后重试。";
            DownloadProgressBar.Foreground = System.Windows.Media.Brushes.Red;

            // 重新启用按钮
            UpdateButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            SkipCheckBox.IsEnabled = true;
        }
    }

    private async Task<bool> DownloadUpdateAsync()
    {
        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"ARCbot_Update_{_updateInfo.Version}.exe");

            using var response = await _httpClient.GetAsync(_updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                // 更新进度条
                if (totalBytes > 0)
                {
                    double progress = (double)totalRead / totalBytes * 100;
                    DownloadProgressBar.Value = progress;
                    DownloadStatusText.Text = $"正在下载... {progress:F1}% ({FormatBytes(totalRead)} / {FormatBytes(totalBytes)})";
                }
                else
                {
                    DownloadStatusText.Text = $"正在下载... {FormatBytes(totalRead)}";
                }

                // 刷新UI
                await Dispatcher.InvokeAsync(() => { });
            }

            // 保存下载的文件路径供安装使用
            _downloadedFilePath = tempPath;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"下载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private string _downloadedFilePath = string.Empty;

    private void InstallUpdate()
    {
        try
        {
            if (string.IsNullOrEmpty(_downloadedFilePath) || !File.Exists(_downloadedFilePath))
            {
                MessageBox.Show("安装文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 创建批处理脚本来替换当前运行的EXE
            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrEmpty(currentExePath))
            {
                MessageBox.Show("无法获取当前程序路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string batchScript = $@"
@echo off
chcp 65001 >nul
echo 正在安装更新...
timeout /t 2 /nobreak >nul

:retry
del /f /q ""{currentExePath}"" 2>nul
if exist ""{currentExePath}"" (
    timeout /t 1 /nobreak >nul
    goto retry
)

move /y ""{_downloadedFilePath}"" ""{currentExePath}""
start "" ""{currentExePath}""
del /f /q ""%~f0"" & exit
";

            string batchPath = Path.Combine(Path.GetTempPath(), "ARCbot_Updater.bat");
            File.WriteAllText(batchPath, batchScript);

            // 启动批处理脚本并关闭当前程序
            Process.Start(new ProcessStartInfo
            {
                FileName = batchPath,
                UseShellExecute = true,
                CreateNoWindow = true
            });

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"安装失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F2} {sizes[order]}";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // 返回 false 表示用户取消更新
        this.DialogResult = false;
        this.Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _httpClient?.Dispose();
        base.OnClosed(e);
    }
}
