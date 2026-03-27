using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using Mapsui.Tiling;
using App.ViewModels;
using App.Models;
using Color = Mapsui.Styles.Color;

namespace App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        vm.OnDaCoiPoi += ThemPinLenBanDo;
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
        // Gỡ sự kiện để tránh tràn RAM
        _vm.OnDaCoiPoi -= ThemPinLenBanDo;
        _vm.OnViTriCapNhat -= CapNhatViTriBanDo;
    }

    private void BanDo_Loaded(object sender, EventArgs e)
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        BanDo.Map = map;

        // Di chuyển camera tới Vĩnh Khánh
        var center = SphericalMercator.FromLonLat(106.690, 10.757);
        BanDo.Map.Navigator.CenterOnAndZoomTo(center, 2);

        BanDo.Info += BanDo_Info;
    }

    private async void BanDo_Info(object? sender, MapInfoEventArgs e)
    {
        if (e.MapInfo?.Feature != null)
        {
            var ten = e.MapInfo.Feature["Ten"]?.ToString();
            var moTa = e.MapInfo.Feature["MoTa"]?.ToString();

            if (!string.IsNullOrEmpty(ten))
            {
                await Application.Current!.MainPage!.DisplayAlert(ten, moTa, "Đóng");
            }
        }
    }

    private void ThemPinLenBanDo(List<PoiModel> danhSach)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var features = new List<PointFeature>();
            foreach (var poi in danhSach)
            {
                var point = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
                var feature = new PointFeature(point)
                {
                    ["Ten"] = poi.Ten,
                    ["MoTa"] = $"📍 {poi.MoTa_Vi}\n\n🌐 {poi.MoTa_En}\n\n📏 Bán kính: {poi.BanKinh}m"
                };
                features.Add(feature);
            }

            var layer = new MemoryLayer
            {
                Name = "PoiLayer",
                DataSource = new MemoryProvider(features),
                Style = SymbolStyles.CreatePinStyle(Color.Red, 0.8)
            };

            BanDo.Map?.Layers.Add(layer);
        });
    }

    private void CapNhatViTriBanDo(Location viTri)
    {
        // Mapsui tự động cập nhật chấm xanh MyLocationEnabled
    }
}