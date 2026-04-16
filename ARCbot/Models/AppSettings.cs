namespace ARCbot.Models;

public class AppSettings
{
    public bool IsDarkMode { get; set; } = true;
    public double BgOpacity { get; set; } = 0.6;
    public string BackgroundImagePath { get; set; } = "pack://application:,,,/Assets/bg2.png";

    // --- 基础包设置 ---
    public bool UseCustomBaseAgent { get; set; } = false;
    public string CustomBaseAgentPath { get; set; } = string.Empty;

    // --- 弹窗设置 ---
    public bool DisableAfdianPopup { get; set; } = false;

    // --- 更新设置 ---
    public string SkippedVersion { get; set; } = string.Empty;
}
