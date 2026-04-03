using App.Models;
using App.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;

using MapsuiBrush = Mapsui.Styles.Brush;
using MapsuiColor = Mapsui.Styles.Color;
using NtsPoint = NetTopologySuite.Geometries.Point;

namespace App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;
    private Mapsui.Map _map = new();

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.OnDaCoiPoi += ThemPinLenBanDo;
        _vm.OnViTriCapNhat += CapNhatViTriBanDo;

        KhoiTaoBanDo();
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

    private void KhoiTaoBanDo()
    {
        _map = new Mapsui.Map();
        _map.Widgets.Clear();
        _map.Layers.Add(OpenStreetMap.CreateTileLayer());

        var center = SphericalMercator.FromLonLat(106.7002, 10.7605);
        _map.Navigator.CenterOnAndZoomTo(new MPoint(center.x, center.y), 3000);

        BanDo.Map = _map;
    }

    private void ThemPinLenBanDo(List<PoiModel> danhSach)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var layerCu = _map.Layers.FirstOrDefault(l => l.Name == "PoiLayer");
            if (layerCu != null)
                _map.Layers.Remove(layerCu);

            var features = new List<IFeature>();

            foreach (var poi in danhSach)
            {
                var (x, y) = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
                var point = new MPoint(x, y);

                var pinFeature = new PointFeature(point)
                {
                    ["Ten"] = poi.Ten,
                    ["MoTa"] = poi.MoTa_Vi
                };

                pinFeature.Styles.Add(new SymbolStyle
                {
                    Fill = new MapsuiBrush(MapsuiColor.Red),
                    Outline = new Pen(MapsuiColor.White, 2),
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 0.6
                });

                features.Add(pinFeature);

                double banKinhHienThi = poi.BanKinh > 0 ? poi.BanKinh : 100;
                double radiusMercator = banKinhHienThi / Math.Cos(poi.Lat * Math.PI / 180.0);

                var pointGeom = new NtsPoint(point.X, point.Y);
                var circleGeometry = pointGeom.Buffer(radiusMercator);

                var circleFeature = new GeometryFeature(circleGeometry);
                circleFeature.Styles.Add(new VectorStyle
                {
                    Fill = new MapsuiBrush(new MapsuiColor(0, 150, 255, 50)),
                    Outline = new Pen(MapsuiColor.DodgerBlue, 2)
                });

                features.Add(circleFeature);
            }

            var layer = new MemoryLayer
            {
                Name = "PoiLayer",
                Features = features
            };

            _map.Layers.Add(layer);
            BanDo.Refresh();
        });
    }

    private void CapNhatViTriBanDo(double lat, double lng)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var layerCu = _map.Layers.FirstOrDefault(l => l.Name == "UserLocationLayer");
            if (layerCu != null)
                _map.Layers.Remove(layerCu);

            var (x, y) = SphericalMercator.FromLonLat(lng, lat);
            var point = new MPoint(x, y);

            var userFeature = new PointFeature(point);
            userFeature.Styles.Add(new SymbolStyle
            {
                Fill = new MapsuiBrush(MapsuiColor.Blue),
                Outline = new Pen(MapsuiColor.White, 3),
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.8
            });

            var userLayer = new MemoryLayer
            {
                Name = "UserLocationLayer",
                Features = new List<IFeature> { userFeature }
            };

            _map.Layers.Add(userLayer);
            _map.Navigator.CenterOn(point);

            BanDo.Refresh();
        });
    }
}