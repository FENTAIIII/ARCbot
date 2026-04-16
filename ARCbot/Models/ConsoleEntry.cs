namespace ARCbot.Models;

/// <summary>日志级别，用于控制台输出着色</summary>
public enum LogLevel
{
    Info,
    Warn,
    Error,
    Stdin   // 用户输入的命令回显
}

/// <summary>控制台中的一条日志条目</summary>
public class ConsoleEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public LogLevel Level { get; init; } = LogLevel.Info;
    public string Text { get; init; } = string.Empty;
    public string InstanceName { get; init; } = string.Empty;

    /// <summary>根据日志级别返回对应颜色 Hex</summary>
    public string ColorHex => Level switch
    {
        LogLevel.Info => "#DCE4EE",   // 浅蓝白
        LogLevel.Warn => "#FFC107",   // 琥珀黄
        LogLevel.Error => "#FF5252",   // 红色
        LogLevel.Stdin => "#69F0AE",   // 绿色
        _ => "#DCE4EE"
    };

    /// <summary>格式化显示文本</summary>
    public string Display => $"[{Timestamp:HH:mm:ss}] {Text}";
}