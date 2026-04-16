using ARCbot.Models;
using ARCbot.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace ARCbot.ViewModels;

/// <summary>
/// 创建/编辑实例表单的 ViewModel。
/// 支持新建和编辑两种模式。
/// </summary>
public partial class CreateInstanceViewModel : ObservableObject
{
    private readonly InstanceManager _instanceManager;

    /// <summary>是否为编辑模式</summary>
    [ObservableProperty]
    private bool _isEditMode = false;

    /// <summary>原始实例名（编辑模式下不可更改）</summary>
    [ObservableProperty]
    private string _originalInstanceName = string.Empty;

    // ─── 表单字段 ───

    [ObservableProperty]
    private string _instanceName = string.Empty;

    [ObservableProperty]
    private string _mcHost = "myarc.fun";

    [ObservableProperty]
    private string _mcPort = "25565";

    [ObservableProperty]
    private string _mcVersion = "1.20.1";

    [ObservableProperty]
    private string _mcUsername = string.Empty;

    [ObservableProperty]
    private string _llmApiKey = string.Empty;

    [ObservableProperty]
    private string _llmApiUrl = "https://ark.cn-beijing.volces.com/api/v3";

    [ObservableProperty]
    private string _llmModel = "doubao-seed-2-0-lite-260215";

    [ObservableProperty]
    private string _mcOwnerName = "_FENTAI_";

    [ObservableProperty]
    private string _mcLoginPassword = string.Empty;

    [ObservableProperty]
    private string _mcAuthType = "microsoft";

    [ObservableProperty]
    private string _tellMode = "whisper";

    [ObservableProperty]
    private bool _autoDefendEnabled = false;

    [ObservableProperty]
    private bool _instinctAutoTpLogin = false;

    [ObservableProperty]
    private bool _instinctAutoEat = false;

    [ObservableProperty]
    private bool _instinctAutoTool = false;

    [ObservableProperty]
    private bool _instinctAutoDump = false;

    [ObservableProperty]
    private bool _debugMode = false;

    [ObservableProperty]
    private string _aiStylePrompt = "你说话要像一个活泼可爱的猫娘，结尾喜欢带上\"喵~\"。";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    // ─── 下拉框选项 ───

    public ObservableCollection<string> AuthTypes { get; } = new() { "microsoft", "offline" };
    public ObservableCollection<string> TellModes { get; } = new() { "whisper", "public" };

    /// <summary>保存成功事件（通知 View 关闭对话框）</summary>
    public event Action? SaveCompleted;

    public CreateInstanceViewModel(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;
    }

    /// <summary>
    /// 以编辑模式加载现有实例数据。
    /// </summary>
    public void LoadForEdit(BotInstance instance)
    {
        IsEditMode = true;
        OriginalInstanceName = instance.InstanceName;

        InstanceName = instance.InstanceName;
        McHost = instance.McHost;
        McPort = instance.McPort;
        McVersion = instance.McVersion;
        McUsername = instance.McUsername;
        LlmApiKey = instance.LlmApiKey;
        LlmApiUrl = instance.LlmApiUrl;
        LlmModel = instance.LlmModel;
        McOwnerName = instance.McOwnerName;
        McLoginPassword = instance.McLoginPassword;
        McAuthType = instance.McAuthType;
        TellMode = instance.TellMode;
        AutoDefendEnabled = instance.AutoDefendEnabled;
        InstinctAutoTpLogin = instance.InstinctAutoTpLogin;
        InstinctAutoEat = instance.InstinctAutoEat;
        InstinctAutoTool = instance.InstinctAutoTool;
        InstinctAutoDump = instance.InstinctAutoDump;
        DebugMode = instance.DebugMode;
        AiStylePrompt = instance.AiStylePrompt;
    }

    /// <summary>重置为新建模式</summary>
    public void ResetForCreate()
    {
        IsEditMode = false;
        OriginalInstanceName = string.Empty;
        InstanceName = string.Empty;
        McHost = "myarc.fun";
        McPort = "25565";
        McVersion = "1.20.1";
        McUsername = string.Empty;
        LlmApiKey = string.Empty;
        LlmApiUrl = "https://ark.cn-beijing.volces.com/api/v3";
        LlmModel = "doubao-seed-2-0-lite-260215";
        McOwnerName = "_FENTAI_";
        McLoginPassword = string.Empty;
        McAuthType = "microsoft";
        TellMode = "whisper";
        AutoDefendEnabled = false;
        InstinctAutoTpLogin = false;
        InstinctAutoEat = false;
        InstinctAutoTool = false;
        InstinctAutoDump = false;
        DebugMode = false;
        AiStylePrompt = "你说话要像一个活泼可爱的猫娘，结尾喜欢带上\"喵~\"。";
        ErrorMessage = string.Empty;
        HasError = false;
    }

    /// <summary>验证并保存</summary>
    [RelayCommand]
    private void Save()
    {
        // ─── 表单验证 ───
        if (string.IsNullOrWhiteSpace(InstanceName))
        {
            SetError("实例名称不能为空。");
            return;
        }

        if (InstanceName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            SetError("实例名称包含非法字符。");
            return;
        }

        if (string.IsNullOrWhiteSpace(McUsername))
        {
            SetError("Minecraft 用户名不能为空。");
            return;
        }

        // 构建模型
        var instance = new BotInstance
        {
            InstanceName = InstanceName.Trim(),
            McHost = McHost,
            McPort = McPort,
            McVersion = McVersion,
            McUsername = McUsername,
            LlmApiKey = LlmApiKey,
            LlmApiUrl = LlmApiUrl,
            LlmModel = LlmModel,
            McOwnerName = McOwnerName,
            McLoginPassword = McLoginPassword,
            McAuthType = McAuthType,
            TellMode = TellMode,
            AutoDefendEnabled = AutoDefendEnabled,
            InstinctAutoTpLogin = InstinctAutoTpLogin,
            InstinctAutoEat = InstinctAutoEat,
            InstinctAutoTool = InstinctAutoTool,
            InstinctAutoDump = InstinctAutoDump,
            DebugMode = DebugMode,
            AiStylePrompt = AiStylePrompt
        };

        try
        {
            if (IsEditMode)
            {
                _instanceManager.UpdateInstance(instance);
            }
            else
            {
                _instanceManager.CreateInstance(instance);
            }

            HasError = false;
            ErrorMessage = string.Empty;
            SaveCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}