namespace ARCbot.Views.Pages;

using System.Windows.Controls;

public partial class MarketPage : Page
{
    public MarketPage()
    {
        InitializeComponent();
        InnerWeb.SetUrl("https://bot.myarc.icu/market");
    }
}
