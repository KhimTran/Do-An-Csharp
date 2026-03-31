using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using Mapsui.Tiling;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
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

        vm.OnDaCoiPoi += ThemPinLenBanDo;
        vm.OnViTriCapNhat += CapNhatViTriBanDo;  // ← nhận (double, double)
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
        _vm.OnDaCoiPoi -= ThemPinLenBanDo;
        _vm.OnViTriCapNhat -= CapNhatViTriBanDo;
    }

    private void BanDo_Loaded(object sender, EventArgs e)
    {
        var map = new Mapsui.Map();
        map.Widgets.Clear();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        BanDo.Map = map;

        var centerTuple = SphericalMercator.FromLonLat(106.690, 10.757);
        var centerPoint = new MPoint(centerTuple.x, centerTuple.y);
        BanDo.Map.Navigator.CenterOnAndZoomTo(centerPoint, 2);
    }

    private void ThemPinLenBanDo(List<PoiModel> danhSach)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var features = new List<IFeature>();

            foreach (var poi in danhSach)
            {
                var tuple = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
                var point = new MPoint(tuple.x, tuple.y);

                var pinFeature = new PointFeature(point)
                {
                    ["Ten"] = poi.Ten,
                    ["MoTa"] = $"📍 {poi.MoTa_Vi}\n\n🌐 {poi.MoTa_En}\n\n📏 Bán kính: {poi.BanKinh}m"
                };

                pinFeature.Styles.Add(new SymbolStyle
                {
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red),
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 0.5
                });

                features.Add(pinFeature);

                double radiusInMercator = poi.BanKinh / Math.Cos(poi.Lat * Math.PI / 180.0);
                var pointGeom = new NetTopologySuite.Geometries.Point(point.X, point.Y);
                var circleGeometry = pointGeom.Buffer(radiusInMercator);
                var circleFeature = new GeometryFeature(circleGeometry);

                circleFeature.Styles.Add(new VectorStyle
                {
                    Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(0, 150, 255, 50)),
                    Outline = new Pen(Mapsui.Styles.Color.DodgerBlue, 2)
                });

                features.Add(circleFeature);
            }

            var layer = new MemoryLayer
            {
                Name = "PoiLayer",
                Features = features
            };

            BanDo.Map?.Layers.Add(layer);
        });
    }

    // ← Đã sửa: nhận (double lat, double lng) cho khớp với MapViewModel
    private void CapNhatViTriBanDo(double lat, double lng)
    {
        // Tuần 4 sẽ dùng để vẽ vị trí người dùng lên bản đồ
    }
}