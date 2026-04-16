using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ARCbot.Models;
using ARCbot.Services;

namespace ARCbot.ViewModels;

/// <summary>
/// 首页 ViewModel：展示实例列表，提供启动/编辑/删除/创建功能。
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private readonly InstanceManager _instanceManager;

    public ObservableCollection<BotInstance> Instances => _instanceManager.Instances;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>请求打开创建对话框的事件（由 View 订阅）</summary>
    public event Action? RequestCreateDialog;

    /// <summary>请求编辑实例的事件</summary>
    public event Action<BotInstance>? RequestEditDialog;

    /// <summary>请求导航到控制台页面的事件</summary>
    public event Action<string>? RequestNavigateToConsole;

    public HomeViewModel(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;
    }

    /// <summary>加载所有实例</summary>
    [RelayCommand]
    private void LoadInstances()
    {
        IsLoading = true;
        try
        {
            _instanceManager.LoadAllInstances();
            StatusMessage = $"已加载 {Instances.Count} 个实例";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>启动指定实例</summary>
    [RelayCommand]
    private void StartInstance(BotInstance instance)
    {
        try
        {
            _instanceManager.StartInstance(instance.InstanceName);
            instance.Status = BotStatus.Running;
            StatusMessage = $"✅ 实例 '{instance.InstanceName}' 已启动";

            // 请求跳转到控制台
            RequestNavigateToConsole?.Invoke(instance.InstanceName);
        }
        catch (Exception ex)
        {
            instance.Status = BotStatus.Error;
            StatusMessage = $"❌ 启动失败: {ex.Message}";
            MessageBox.Show($"启动实例失败:\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>停止指定实例</summary>
    [RelayCommand]
    private async Task StopInstanceAsync(BotInstance instance)
    {
        try
        {
            await _instanceManager.StopInstanceAsync(instance.InstanceName);
            instance.Status = BotStatus.Stopped;
            StatusMessage = $"⬛ 实例 '{instance.InstanceName}' 已停止";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 停止失败: {ex.Message}";
        }
    }

    /// <summary>删除指定实例</summary>
    [RelayCommand]
    private async Task DeleteInstanceAsync(BotInstance instance)
    {
        var result = MessageBox.Show(
            $"确定要删除实例 '{instance.InstanceName}' 吗？\n此操作将删除该实例的所有文件，不可恢复。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _instanceManager.DeleteInstanceAsync(instance.InstanceName);
            StatusMessage = $"🗑️ 实例 '{instance.InstanceName}' 已删除";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 删除失败: {ex.Message}";
        }
    }

    /// <summary>打开创建对话框</summary>
    [RelayCommand]
    private void OpenCreateDialog()
    {
        RequestCreateDialog?.Invoke();
    }

    /// <summary>打开编辑对话框</summary>
    [RelayCommand]
    private void EditInstance(BotInstance instance)
    {
        RequestEditDialog?.Invoke(instance);
    }
}