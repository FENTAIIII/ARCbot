using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ARCbot.Helpers;
using ARCbot.Services;

namespace ARCbot.ViewModels;

/// <summary>
/// 插件管理 ViewModel：列出插件文件，支持导入和删除。
/// </summary>
public partial class PluginsViewModel : ObservableObject
{
    private readonly InstanceManager _instanceManager;

    /// <summary>可选实例列表</summary>
    [ObservableProperty]
    private ObservableCollection<string> _instanceNames = new();

    /// <summary>当前选中的实例名</summary>
    [ObservableProperty]
    private string? _selectedInstanceName;

    /// <summary>当前实例的插件文件列表</summary>
    [ObservableProperty]
    private ObservableCollection<PluginFileInfo> _plugins = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public PluginsViewModel(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;
    }

    /// <summary>刷新实例列表</summary>
    [RelayCommand]
    private void LoadInstanceNames()
    {
        InstanceNames.Clear();
        foreach (var inst in _instanceManager.Instances)
        {
            InstanceNames.Add(inst.InstanceName);
        }

        if (SelectedInstanceName == null && InstanceNames.Count > 0)
            SelectedInstanceName = InstanceNames[0];
    }

    /// <summary>选中实例变化时刷新插件列表</summary>
    partial void OnSelectedInstanceNameChanged(string? value)
    {
        RefreshPlugins();
    }

    /// <summary>刷新插件文件列表</summary>
    [RelayCommand]
    private void RefreshPlugins()
    {
        Plugins.Clear();
        if (SelectedInstanceName == null) return;

        var pluginsDir = PathHelper.GetInstancePluginsDir(SelectedInstanceName);
        if (!Directory.Exists(pluginsDir))
        {
            Directory.CreateDirectory(pluginsDir);
            return;
        }

        foreach (var file in Directory.GetFiles(pluginsDir, "*.js"))
        {
            var fi = new FileInfo(file);
            Plugins.Add(new PluginFileInfo
            {
                FileName = fi.Name,
                FullPath = fi.FullName,
                SizeKB = fi.Length / 1024.0,
                LastModified = fi.LastWriteTime
            });
        }

        StatusMessage = $"共 {Plugins.Count} 个插件";
    }

    /// <summary>导入插件文件</summary>
    [RelayCommand]
    private void ImportPlugin()
    {
        if (SelectedInstanceName == null)
        {
            StatusMessage = "请先选择一个实例。";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "选择插件文件",
            Filter = "JavaScript 文件 (*.js)|*.js|所有文件 (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true) return;

        var pluginsDir = PathHelper.GetInstancePluginsDir(SelectedInstanceName);
        Directory.CreateDirectory(pluginsDir);

        int imported = 0;
        foreach (var filePath in dialog.FileNames)
        {
            var destPath = Path.Combine(pluginsDir, Path.GetFileName(filePath));
            File.Copy(filePath, destPath, overwrite: true);
            imported++;
        }

        StatusMessage = $"✅ 已导入 {imported} 个插件";
        RefreshPlugins();
    }

    /// <summary>删除插件</summary>
    [RelayCommand]
    private void DeletePlugin(PluginFileInfo plugin)
    {
        try
        {
            if (File.Exists(plugin.FullPath))
                File.Delete(plugin.FullPath);

            Plugins.Remove(plugin);
            StatusMessage = $"🗑️ 已删除 {plugin.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 删除失败: {ex.Message}";
        }
    }

    /// <summary>打开开发文档</summary>
    [RelayCommand]
    private void OpenDevDocs()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://docs.myarc.icu/dev/skills",
            UseShellExecute = true
        });
    }
}

/// <summary>插件文件信息</summary>
public class PluginFileInfo
{
    public string FileName { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public double SizeKB { get; init; }
    public DateTime LastModified { get; init; }
    public string DisplaySize => SizeKB < 1 ? "<1 KB" : $"{SizeKB:F1} KB";
}