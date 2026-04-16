using System;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace ARCbot.Views.Pages;

public partial class WebPage : UserControl
{
    public WebPage()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        try
        {
            // 设置环境以确保数据文件夹在合适的位置
            var userDataFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ARCbot_WebView2");
            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataFolder);
            
            await WebView.EnsureCoreWebView2Async(env);
            
            WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            
            WebView.CoreWebView2.NavigationStarting += (s, e) => 
            {
                LoadingProgressBar.Visibility = System.Windows.Visibility.Visible;
            };
            
            WebView.CoreWebView2.NavigationCompleted += (s, e) => 
            {
                LoadingProgressBar.Visibility = System.Windows.Visibility.Collapsed;
            };
        }
        catch (Exception)
        {
            // 处理初始化失败的情况
        }
    }

    public void SetUrl(string url)
    {
        if (WebView != null)
        {
            WebView.Source = new Uri(url);
        }
    }
}
