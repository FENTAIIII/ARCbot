using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ARCbot.ViewModels;

namespace ARCbot.Views.Pages;

public partial class ConsolePage : Page
{
    private readonly ConsoleViewModel _viewModel;

    private INotifyCollectionChanged? _currentCollection;

    public ConsolePage()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<ConsoleViewModel>();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConsoleViewModel.ConsoleEntries))
            {
                SubscribeAutoScroll();
            }
            if (e.PropertyName == nameof(ConsoleViewModel.HasSelectedInstance))
            {
                NoSelectionHint.Visibility = _viewModel.HasSelectedInstance
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        };
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.RefreshRunningInstancesCommand.Execute(null);
        SubscribeAutoScroll();
        UpdateEmptyState();

        NoSelectionHint.Visibility = _viewModel.HasSelectedInstance
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void SubscribeAutoScroll()
    {
        // 先取消旧的订阅
        if (_currentCollection != null)
        {
            _currentCollection.CollectionChanged -= OnConsoleEntriesChanged;
        }

        _currentCollection = _viewModel.ConsoleEntries as INotifyCollectionChanged;
        if (_currentCollection != null)
        {
            _currentCollection.CollectionChanged += OnConsoleEntriesChanged;
        }
    }

    private void OnConsoleEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (ConsoleOutput.Items.Count > 0)
                {
                    // 检查是否需要自动滚动（如果用户正在往上翻，则不自动滚动）
                    var scrollViewer = GetScrollViewer(ConsoleOutput);
                    if (scrollViewer == null || scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 50)
                    {
                        ConsoleOutput.ScrollIntoView(ConsoleOutput.Items[^1]);
                    }
                }
            });
        }
    }

    private ScrollViewer? GetScrollViewer(DependencyObject element)
    {
        if (element is ScrollViewer viewer) return viewer;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(element, i);
            var result = GetScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    private void ConsoleInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SendInputCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void UpdateEmptyState()
    {
        NoRunningLabel.Visibility = _viewModel.RunningInstances.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}