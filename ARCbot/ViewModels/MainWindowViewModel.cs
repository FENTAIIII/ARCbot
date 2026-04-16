using CommunityToolkit.Mvvm.ComponentModel;

namespace ARCbot.ViewModels;

/// <summary>
/// 主窗口 ViewModel，管理全局导航状态和应用级别属性。
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "ARCbot";

    [ObservableProperty]
    private bool _isInitialized = false;

    /// <summary>当前选中的导航页面标识</summary>
    [ObservableProperty]
    private string _currentPage = "Home";
}