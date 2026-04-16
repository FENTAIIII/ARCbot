using System.IO;
using System.Text;
using ARCbot.Helpers;
using ARCbot.Models;

namespace ARCbot.Services;

/// <summary>
/// ���� BotInstance ģ���� .env �ļ�֮�����˫��ת����
/// ֧�ֶ�ȡ��д�롢�Լ�����ע���С�
/// </summary>
public class EnvManager
{
    /// <summary>
    /// �� BotInstance ������д�뵽��Ӧʵ��Ŀ¼�� .env �ļ���
    /// </summary> 
    public void WriteEnv(BotInstance instance)
    {
        var envPath = PathHelper.GetInstanceEnvPath(instance.InstanceName);
        var dir = Path.GetDirectoryName(envPath)!;
        Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        sb.AppendLine("# �T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T");
        sb.AppendLine($"# ARCbot Instance Config: {instance.InstanceName}");
        sb.AppendLine($"# Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("# �T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T");
        sb.AppendLine();

        sb.AppendLine("#  Minecraft Server ");
        sb.AppendLine($"MC_HOST={instance.McHost}");
        sb.AppendLine($"MC_PORT={instance.McPort}");
        sb.AppendLine($"MC_VERSION={instance.McVersion}");
        sb.AppendLine($"MC_USERNAME={instance.McUsername}");
        sb.AppendLine($"MC_AUTH_TYPE={instance.McAuthType}");
        sb.AppendLine($"MC_LOGIN_PASSWORD={instance.McLoginPassword}");
        sb.AppendLine($"MC_OWNER_NAME={instance.McOwnerName}");
        sb.AppendLine();

        sb.AppendLine("#  AI Configuration ");
        sb.AppendLine($"LLM_API_KEY={instance.LlmApiKey}");
        sb.AppendLine($"LLM_API_URL={instance.LlmApiUrl}");
        sb.AppendLine($"LLM_MODEL={instance.LlmModel}");
        sb.AppendLine($"AI_STYLE_PROMPT={instance.AiStylePrompt}");
        sb.AppendLine();

        sb.AppendLine("# ������ Behavior ������");
        sb.AppendLine($"TELL_MODE={instance.TellMode}");
        sb.AppendLine($"AUTO_DEFEND_ENABLED={BoolToEnv(instance.AutoDefendEnabled)}");
        sb.AppendLine($"INSTINCT_AUTO_TP_LOGIN={BoolToEnv(instance.InstinctAutoTpLogin)}");
        sb.AppendLine($"INSTINCT_AUTO_EAT={BoolToEnv(instance.InstinctAutoEat)}");
        sb.AppendLine($"INSTINCT_AUTO_TOOL={BoolToEnv(instance.InstinctAutoTool)}");
        sb.AppendLine($"INSTINCT_AUTO_DUMP={BoolToEnv(instance.InstinctAutoDump)}");
        sb.AppendLine($"DEBUG_MODE={BoolToEnv(instance.DebugMode)}");

        File.WriteAllText(envPath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// �� .env �ļ���ȡ���ò���䵽 BotInstance��
    /// </summary>
    public BotInstance ReadEnv(string instanceName)
    {
        var envPath = PathHelper.GetInstanceEnvPath(instanceName);
        var instance = new BotInstance { InstanceName = instanceName };

        if (!File.Exists(envPath))
            return instance;

        var lines = File.ReadAllLines(envPath, Encoding.UTF8);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0) continue;

            var key = trimmed[..eqIndex].Trim();
            var value = trimmed[(eqIndex + 1)..].Trim();

            // ȥ�����ܵ����Ű���
            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            dict[key] = value;
        }

        // ӳ�䵽ģ��
        if (dict.TryGetValue("MC_HOST", out var v)) instance.McHost = v;
        if (dict.TryGetValue("MC_PORT", out v)) instance.McPort = v;
        if (dict.TryGetValue("MC_VERSION", out v)) instance.McVersion = v;
        if (dict.TryGetValue("MC_USERNAME", out v)) instance.McUsername = v;
        if (dict.TryGetValue("MC_AUTH_TYPE", out v)) instance.McAuthType = v;
        if (dict.TryGetValue("MC_LOGIN_PASSWORD", out v)) instance.McLoginPassword = v;
        if (dict.TryGetValue("MC_OWNER_NAME", out v)) instance.McOwnerName = v;
        
        if (dict.TryGetValue("LLM_API_KEY", out v)) instance.LlmApiKey = v;
        else if (dict.TryGetValue("DEEPSEEK_API_KEY", out v)) instance.LlmApiKey = v; // Backward compatibility

        if (dict.TryGetValue("LLM_API_URL", out v)) instance.LlmApiUrl = v;
        if (dict.TryGetValue("LLM_MODEL", out v)) instance.LlmModel = v;
        if (dict.TryGetValue("AI_STYLE_PROMPT", out v)) instance.AiStylePrompt = v;
        if (dict.TryGetValue("TELL_MODE", out v)) instance.TellMode = v;
        if (dict.TryGetValue("AUTO_DEFEND_ENABLED", out v)) instance.AutoDefendEnabled = EnvToBool(v);
        if (dict.TryGetValue("INSTINCT_AUTO_TP_LOGIN", out v)) instance.InstinctAutoTpLogin = EnvToBool(v);
        if (dict.TryGetValue("INSTINCT_AUTO_EAT", out v)) instance.InstinctAutoEat = EnvToBool(v);
        if (dict.TryGetValue("INSTINCT_AUTO_TOOL", out v)) instance.InstinctAutoTool = EnvToBool(v);
        if (dict.TryGetValue("INSTINCT_AUTO_DUMP", out v)) instance.InstinctAutoDump = EnvToBool(v);
        if (dict.TryGetValue("DEBUG_MODE", out v)) instance.DebugMode = EnvToBool(v);

        return instance;
    }

    private static string BoolToEnv(bool value) => value ? "true" : "false";

    private static bool EnvToBool(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        value == "1" ||
        value.Equals("yes", StringComparison.OrdinalIgnoreCase);
}