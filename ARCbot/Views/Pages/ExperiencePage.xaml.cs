namespace ARCbot.Views.Pages;

using System.Windows.Controls;

public partial class ExperiencePage : Page
{
    public ExperiencePage()
    {
        InitializeComponent();
        InnerWeb.SetUrl("https://bot.myarc.icu/experience");
    }
}
