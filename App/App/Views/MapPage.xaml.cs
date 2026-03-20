using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using App.ViewModels;
using App.Models;
namespace App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Khi có danh sách POI → thêm pin lên bản đồ
        vm.OnDaCoiPoi += ThemPinLenBanDo;

        // Khi GPS cập nhật → di chuyển bản đồ theo
        vm.OnViTriCapNhat += CapNhatViTriBanDo;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.KhoiDongAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.DungGps();
    }

    private void BanDo_Loaded(object sender, EventArgs e)
    {
        BanDo.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                new Location(10.757, 106.690),
                Distance.FromKilometers(0.5)
            )
        );
    }

    private void ThemPinLenBanDo(List<PoiModel> danhSach)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BanDo.Pins.Clear();
            foreach (var poi in danhSach)
            {
                var pin = new Pin
                {
                    Label = poi.Ten,
                    Address = poi.MoTa_Vi,
                    Location = new Location(poi.Lat, poi.Lng),
                    Type = PinType.Place
                };
                BanDo.Pins.Add(pin);
            }
        });
    }

    private void CapNhatViTriBanDo(Location viTri)
    {
        // Không auto-move bản đồ, chỉ để GPS chạy ngầm
        // Nếu muốn follow người dùng thì bỏ comment dòng dưới:
        // MainThread.BeginInvokeOnMainThread(() =>
        //     BanDo.MoveToRegion(MapSpan.FromCenterAndRadius(viTri, Distance.FromKilometers(0.3))));
    }
}