using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ARCbot.Models;
using ARCbot.Services;

namespace ARCbot.ViewModels;

public partial class ConsoleViewModel : ObservableObject
{
    private readonly InstanceManager _instanceManager;
    private readonly Dispatcher _dispatcher;

    [ObservableProperty]
    private ObservableCollection<string> _runningInstances = new();

    [ObservableProperty]
    private string? _selectedInstance;

    [ObservableProperty]
    private ObservableCollection<ConsoleEntry> _consoleEntries = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _hasSelectedInstance = false;

    private readonly Dictionary<string, ObservableCollection<ConsoleEntry>> _logCache = new();
    private readonly HashSet<string> _subscribedInstances = new();

    public ConsoleViewModel(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;
        _dispatcher = System.Windows.Application.Current.Dispatcher;

        // 订阅进程启动事件，以便在后台也能收集日志
        _instanceManager.ProcessStarted += (name, pm) =>
        {
            _dispatcher.Invoke(() =>
            {
                if (!RunningInstances.Contains(name))
                    RunningInstances.Add(name);
                SubscribeToProcess(name);
            });
        };

        // 初始化时加载已在运行的实例
        RefreshRunningInstances();
    }

    [RelayCommand]
    private void RefreshRunningInstances()
    {
        RunningInstances.Clear();
        foreach (var name in _instanceManager.RunningProcesses.Keys)
        {
            RunningInstances.Add(name);
            SubscribeToProcess(name);
        }

        if (SelectedInstance != null && !RunningInstances.Contains(SelectedInstance))
            SelectedInstance = RunningInstances.FirstOrDefault();

        if (SelectedInstance == null && RunningInstances.Count > 0)
            SelectedInstance = RunningInstances[0];
    }

    partial void OnSelectedInstanceChanged(string? value)
    {
        HasSelectedInstance = value != null;

        if (value == null)
        {
            ConsoleEntries = new ObservableCollection<ConsoleEntry>();
            return;
        }

        if (!_logCache.ContainsKey(value))
            _logCache[value] = new ObservableCollection<ConsoleEntry>();

        ConsoleEntries = _logCache[value];
        SubscribeToProcess(value);
    }

    private void SubscribeToProcess(string instanceName)
    {
        if (_subscribedInstances.Contains(instanceName)) return;
        if (!_instanceManager.RunningProcesses.TryGetValue(instanceName, out var pm)) return;

        if (!_logCache.ContainsKey(instanceName))
            _logCache[instanceName] = new ObservableCollection<ConsoleEntry>();

        pm.OutputReceived += (_, entry) =>
        {
            _dispatcher.Invoke(() =>
            {
                if (!_logCache.ContainsKey(instanceName))
                    _logCache[instanceName] = new ObservableCollection<ConsoleEntry>();

                _logCache[instanceName].Add(entry);

                // 移除 5000 行限制，允许用户滚动查看所有日志
                // while (_logCache[instanceName].Count > 5000)
                //     _logCache[instanceName].RemoveAt(0);
            });
        };

        pm.ProcessExited += (_, exitCode) =>
        {
            _dispatcher.Invoke(() =>
            {
                _logCache.GetValueOrDefault(instanceName)?.Add(new ConsoleEntry
                {
                    Level = exitCode == 0 ? LogLevel.Info : LogLevel.Error,
                    Text = $"进程已退出，退出码: {exitCode}",
                    InstanceName = instanceName
                });

                _subscribedInstances.Remove(instanceName);
                RefreshRunningInstances();
            });
        };

        _subscribedInstances.Add(instanceName);
    }

    [RelayCommand]
    private async Task SendInputAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || SelectedInstance == null) return;
        if (!_instanceManager.RunningProcesses.TryGetValue(SelectedInstance, out var pm)) return;

        var text = InputText;
        InputText = string.Empty;
        await pm.WriteInputAsync(text);
    }

    [RelayCommand]
    private async Task ForceStopAsync()
    {
        if (SelectedInstance == null) return;

        // ★ 修复: 完全限定名
        var result = System.Windows.MessageBox.Show(
            $"确定要强制停止实例 '{SelectedInstance}' 吗？",
            "确认停止",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        await _instanceManager.StopInstanceAsync(SelectedInstance);
        RefreshRunningInstances();
    }

    [RelayCommand]
    private void ClearConsole()
    {
        if (SelectedInstance != null && _logCache.TryGetValue(SelectedInstance, out var logs))
            logs.Clear();
    }

    public void FocusInstance(string instanceName)
    {
        RefreshRunningInstances();
        if (RunningInstances.Contains(instanceName))
            SelectedInstance = instanceName;
    }
}