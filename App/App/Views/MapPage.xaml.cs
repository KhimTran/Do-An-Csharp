using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
namespace App.Views;


public partial class MapPage : ContentPage
{
    public MapPage()
    {
        InitializeComponent();
    }

    private void BanDo_Loaded(object sender, EventArgs e)
    {
        BanDo.MoveToRegion(
            Microsoft.Maui.Controls.Maps.MapSpan.FromCenterAndRadius(
                new Location(10.757, 106.690),
                Microsoft.Maui.Controls.Maps.Distance.FromKilometers(0.5)
            )
        );
    }
}