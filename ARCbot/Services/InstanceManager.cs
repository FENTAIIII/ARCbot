using System.Collections.ObjectModel;
using System.IO;
using ARCbot.Helpers;
using ARCbot.Models;

namespace ARCbot.Services;

/// <summary>
/// ʵ���� CRUD ������״̬������
/// ��Ϊ��������ά������ʵ���б��������еĽ����ֵ䡣
/// </summary>
public class InstanceManager
{
    private readonly EnvManager _envManager;
    private readonly AuthInterceptor _authInterceptor;
    private readonly DownloadService _downloadService;
    private readonly SettingsService _settingsService;

    /// <summary>已创建的实例列表（UI 数据源）</summary>
    public ObservableCollection<BotInstance> Instances { get; } = new();

    /// <summary>运行中的进程管理字典 [实例名 -> 进程管理器]</summary>
    public Dictionary<string, NodeProcessManager> RunningProcesses { get; } = new();

    /// <summary>当实例进程启动时触发</summary>
    public event Action<string, NodeProcessManager>? ProcessStarted;

    public InstanceManager(EnvManager envManager, AuthInterceptor authInterceptor, DownloadService downloadService, SettingsService settingsService)
    {
        _envManager = envManager;
        _authInterceptor = authInterceptor;
        _downloadService = downloadService;
        _settingsService = settingsService;
    }

    /// <summary>
    /// ɨ��ʵ��Ŀ¼�����������Ѵ��ڵ�ʵ����
    /// </summary>
    public void LoadAllInstances()
    {
        Instances.Clear();

        if (!Directory.Exists(PathHelper.InstancesDir)) return;

        foreach (var dir in Directory.GetDirectories(PathHelper.InstancesDir))
        {
            var instanceName = Path.GetFileName(dir);
            var envPath = PathHelper.GetInstanceEnvPath(instanceName);

            if (File.Exists(envPath))
            {
                var instance = _envManager.ReadEnv(instanceName);
                instance.Status = RunningProcesses.ContainsKey(instanceName)
                    ? BotStatus.Running
                    : BotStatus.Stopped;
                Instances.Add(instance);
            }
            else
            {
                // Ŀ¼���ڵ�û�� .env������һ��Ĭ�ϵ�
                Instances.Add(new BotInstance
                {
                    InstanceName = instanceName,
                    Status = BotStatus.Stopped
                });
            }
        }
    }

    /// <summary>
    /// ������ʵ������ѹ������ + д�� .env��
    /// </summary>
    public void CreateInstance(BotInstance instance)
    {
        var instanceDir = PathHelper.GetInstanceDir(instance.InstanceName);
        if (Directory.Exists(instanceDir))
            throw new InvalidOperationException($"ʵ�� '{instance.InstanceName}' �Ѵ��ڡ�");

        // ��ѹ������
        _downloadService.ExtractBaseAgentToInstance(instance.InstanceName, _settingsService.Settings.UseCustomBaseAgent);

        // д�� .env
        _envManager.WriteEnv(instance);

        instance.Status = BotStatus.Stopped;
        Instances.Add(instance);
    }

    /// <summary>
    /// ����ʵ�����ã�����д�� .env����
    /// </summary>
    public void UpdateInstance(BotInstance instance)
    {
        _envManager.WriteEnv(instance);

        // �����б��еĶ�Ӧ��
        var existing = Instances.FirstOrDefault(i => i.InstanceName == instance.InstanceName);
        if (existing != null)
        {
            var index = Instances.IndexOf(existing);
            Instances[index] = instance;
        }
    }

    /// <summary>
    /// ɾ��ʵ����ֹͣ���� + ɾ��Ŀ¼��
    /// </summary>
    public async Task DeleteInstanceAsync(string instanceName)
    {
        // ����������У���ֹͣ
        if (RunningProcesses.TryGetValue(instanceName, out var pm))
        {
            await pm.StopAsync();
            pm.Dispose();
            RunningProcesses.Remove(instanceName);
        }

        // ɾ��Ŀ¼
        var dir = PathHelper.GetInstanceDir(instanceName);
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);

        // ���б��Ƴ�
        var instance = Instances.FirstOrDefault(i => i.InstanceName == instanceName);
        if (instance != null)
            Instances.Remove(instance);
    }

    /// <summary>
    /// ����ʵ���� Node.js ���̡�
    /// </summary>
    public NodeProcessManager StartInstance(string instanceName)
    {
        if (RunningProcesses.ContainsKey(instanceName))
            throw new InvalidOperationException($"ʵ�� '{instanceName}' ���������С�");

        var pm = new NodeProcessManager(instanceName, _authInterceptor);

        pm.ProcessExited += (_, exitCode) =>
        {
            RunningProcesses.Remove(instanceName);
            var inst = Instances.FirstOrDefault(i => i.InstanceName == instanceName);
            if (inst != null)
                inst.Status = exitCode == 0 ? BotStatus.Stopped : BotStatus.Error;
        };

        pm.Start();
        RunningProcesses[instanceName] = pm;
        ProcessStarted?.Invoke(instanceName, pm);

        var instance = Instances.FirstOrDefault(i => i.InstanceName == instanceName);
        if (instance != null)
            instance.Status = BotStatus.Running;

        return pm;
    }

    /// <summary>
    /// ֹͣʵ���� Node.js ���̡�
    /// </summary>
    public async Task StopInstanceAsync(string instanceName)
    {
        if (!RunningProcesses.TryGetValue(instanceName, out var pm)) return;

        await pm.StopAsync();
        pm.Dispose();
        RunningProcesses.Remove(instanceName);

        var instance = Instances.FirstOrDefault(i => i.InstanceName == instanceName);
        if (instance != null)
            instance.Status = BotStatus.Stopped;
    }
}