using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ARCbot.ViewModels;

namespace ARCbot.Views.Dialogs;

public partial class CreateInstanceDialog : Window
{
    public CreateInstanceDialog()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (DataContext is CreateInstanceViewModel vm)
            {
                // 编辑模式下：更新标题文字、禁用实例名输入、更改按钮文字
                if (vm.IsEditMode)
                {
                    DialogTitle.Text = "编辑实例";
                    InstanceNameBox.IsEnabled = false;
                    SaveButton.Content = "保存更改";
                    PasswordField.Password = vm.McLoginPassword;
                }
                else
                {
                    DialogTitle.Text = "创建新实例";
                    InstanceNameBox.IsEnabled = true;
                    SaveButton.Content = "保存并创建";
                }
            }
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// PasswordBox 不支持数据绑定，手动同步到 ViewModel。
    /// </summary>
    private void PasswordField_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is CreateInstanceViewModel vm)
        {
            vm.McLoginPassword = PasswordField.Password;
        }
    }
}
