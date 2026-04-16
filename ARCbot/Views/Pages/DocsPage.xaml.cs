namespace ARCbot.Views.Pages;

using System.Windows.Controls;

public partial class DocsPage : Page
{
    public DocsPage()
    {
        InitializeComponent();
        InnerWeb.SetUrl("https://bot.myarc.icu/docs");
    }
}
