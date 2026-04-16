using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ARCbot.Models;
using ARCbot.Views.Dialogs;

namespace ARCbot.Services;

public class UpdateService
{
    private readonly string _currentVersion = "1.0.3"; // 你的当前版本号
    private readonly string _updateUrl = "http://localhost:3000/update";
    private readonly SettingsService _settingsService;

    public UpdateService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task CheckUpdateAsync()
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            // 1. 获取服务器的更新信息
            string json = await client.GetStringAsync(_updateUrl);
            UpdateInfo updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json);

            if (updateInfo == null || !Version.TryParse(updateInfo.Version, out Version remoteVersion))
                return;

            Version localVersion = new Version(_currentVersion);

            // 2. 判断是否有新版本
            if (remoteVersion > localVersion)
            {
                // 3. 检查用户是否曾经勾选过"跳过此版本"
                string skippedVersion = GetSkippedVersionFromSettings(); 
                if (skippedVersion == updateInfo.Version)
                {
                    return; // 用户选择过跳过该版本，直接静默退出
                }

                // 4. 在 UI 线程弹出我们自定义的更新窗口
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new UpdateDialog(updateInfo);
                    bool? result = dialog.ShowDialog();

                    if (result == true && dialog.IsDownloadSuccessful)
                    {
                        // 用户点击了"立即更新"且下载成功，程序将自动关闭并安装更新
                    }
                    else
                    {
                        // 用户点击了"暂不更新"或关闭了窗口，或下载失败
                        if (dialog.IsSkipChecked)
                        {
                            // 如果勾选了"跳过此版本"，保存这个版本号到你的配置文件中
                            SaveSkippedVersionToSettings(updateInfo.Version);
                        }
                    }
                });
            }
        }
        catch (Exception)
        {
            // 网络异常等情况，静默处理，不要打扰用户
        }
    }

    private string GetSkippedVersionFromSettings()
    {
        return _settingsService.Settings.SkippedVersion;
    }

    private void SaveSkippedVersionToSettings(string version)
    {
        _settingsService.UpdateSettings(settings =>
        {
            settings.SkippedVersion = version;
        });
    }
}
