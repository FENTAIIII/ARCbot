using System.Text.RegularExpressions;

namespace ARCbot.Services;

/// <summary>
/// 微软登录验证码拦截事件参数
/// </summary>
public class AuthCodeEventArgs : EventArgs
{
    public required string InstanceName { get; init; }
    public required string Url { get; init; }
    public required string Code { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// 全局微软登录拦截器。
/// 持续监听所有运行中实例的 stdout，当检测到微软设备码登录提示时，
/// 通过事件通知 UI 层弹出全局验证弹窗。
/// 
/// 设计为单例服务，由 DI 容器管理。
/// </summary>
public partial class AuthInterceptor
{
    /// <summary>
    /// 当检测到微软登录验证码时触发。
    /// 订阅者（MainWindow）应在 UI 线程上处理此事件。
    /// </summary>
    public event EventHandler<AuthCodeEventArgs>? AuthCodeDetected;

    // 正则匹配微软设备码登录提示
    // 示例: [msa] First time signing in... https://www.microsoft.com/link and use the code 72HVKSLY
    // 也兼容其他格式: "To sign in, use a web browser to open the page https://... and enter the code XXXXXXXX"
    [GeneratedRegex(
        @"(?:https?://\S*microsoft\.com\S*)\s+.*?(?:code|Code)\s+([A-Z0-9]{6,12})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MsaCodeRegex();

    // 备用正则：更宽泛的匹配
    [GeneratedRegex(
        @"(https?://\S*microsoft\S+)\s+.*?([A-Z0-9]{8})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MsaCodeFallbackRegex();

    /// <summary>
    /// 分析一行控制台输出，检测是否包含微软登录验证码。
    /// 此方法由 NodeProcessManager 在每次收到 stdout 行时调用。
    /// </summary>
    /// <param name="instanceName">产生该输出的实例名称</param>
    /// <param name="line">一行 stdout 文本</param>
    public void AnalyzeLine(string instanceName, string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        // 尝试主正则
        var match = MsaCodeRegex().Match(line);
        if (match.Success)
        {
            // 提取 URL
            var urlMatch = Regex.Match(line, @"(https?://\S+)", RegexOptions.IgnoreCase);
            var url = urlMatch.Success ? urlMatch.Groups[1].Value : "https://www.microsoft.com/link";
            var code = match.Groups[1].Value.Trim();

            RaiseAuthCodeDetected(instanceName, url, code);
            return;
        }

        // 尝试备用正则
        var fallback = MsaCodeFallbackRegex().Match(line);
        if (fallback.Success)
        {
            var url = fallback.Groups[1].Value.Trim();
            var code = fallback.Groups[2].Value.Trim();

            RaiseAuthCodeDetected(instanceName, url, code);
        }
    }

    private void RaiseAuthCodeDetected(string instanceName, string url, string code)
    {
        AuthCodeDetected?.Invoke(this, new AuthCodeEventArgs
        {
            InstanceName = instanceName,
            Url = url,
            Code = code,
            DetectedAt = DateTime.Now
        });
    }
}