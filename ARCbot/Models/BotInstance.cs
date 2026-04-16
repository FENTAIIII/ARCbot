using CommunityToolkit.Mvvm.ComponentModel;

namespace ARCbot.Models;

/// <summary>������ʵ������״̬</summary>
public enum BotStatus
{
    Stopped,
    Running,
    Starting,
    Error
}

/// <summary>
/// ����һ�� Minecraft AI ������ʵ������������ģ�͡�
/// ʹ�� ObservableObject ��֧�� UI ʵʱ�󶨡�
/// </summary>
public partial class BotInstance : ObservableObject
{
    // ������ ������Ϣ ������
    [ObservableProperty]
    private string _instanceName = string.Empty;

    [ObservableProperty]
    private BotStatus _status = BotStatus.Stopped;

    // ������ .env ӳ���ֶ� ������
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
    private string _aiStylePrompt = "��˵��Ҫ��һ�����ÿɰ���è���βϲ������\"��~\"��";

    /// <summary>����һ����������ڱ༭ʱ��Ӱ��ԭʼ���ݣ�</summary>
    public BotInstance Clone()
    {
        return new BotInstance
        {
            InstanceName = InstanceName,
            Status = Status,
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
    }
}