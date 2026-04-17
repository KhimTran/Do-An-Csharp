using App.Models;
using App.Services;
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

    private bool _daZoomLanDau = false;
    private readonly List<PoiModel> _danhSachPoiHienTai = new();

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
        _daZoomLanDau = false;
    }

    private void KhoiTaoBanDo()
    {
        _map = new Mapsui.Map();
        _map.Widgets.Clear();

        // Dùng OpenStreetMap tile mặc định, thêm lớp đường dẫn trực quan ở phía trên để dễ nhìn đường.
        _map.Layers.Add(OpenStreetMap.CreateTileLayer());

        var center = SphericalMercator.FromLonLat(106.7002, 10.7605);
        _map.Navigator.CenterOnAndZoomTo(new MPoint(center.x, center.y), 3200);

        BanDo.Map = _map;
    }

    private void ThemPinLenBanDo(List<PoiModel> danhSach)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _danhSachPoiHienTai.Clear();
            _danhSachPoiHienTai.AddRange(danhSach);

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

                // Pin POI có viền trắng dày để nổi bật trên nền bản đồ nhiều chi tiết.
                pinFeature.Styles.Add(new SymbolStyle
                {
                    Fill = new MapsuiBrush(MapsuiColor.Red),
                    Outline = new Pen(MapsuiColor.White, 3),
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 0.9
                });

                features.Add(pinFeature);

                double banKinhHienThi = poi.BanKinh > 0 ? poi.BanKinh : 100;
                double radiusMercator = banKinhHienThi / Math.Cos(poi.Lat * Math.PI / 180.0);

                var pointGeom = new NtsPoint(point.X, point.Y);
                var circleGeometry = pointGeom.Buffer(radiusMercator);

                var circleFeature = new GeometryFeature(circleGeometry);
                circleFeature.Styles.Add(new VectorStyle
                {
                    Fill = new MapsuiBrush(new MapsuiColor(30, 136, 229, 42)),
                    Outline = new Pen(new MapsuiColor(30, 136, 229, 180), 3)
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
                Fill = new MapsuiBrush(new MapsuiColor(33, 99, 255)),
                Outline = new Pen(MapsuiColor.White, 4),
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.75
            });

            var userLayer = new MemoryLayer
            {
                Name = "UserLocationLayer",
                Features = new List<IFeature> { userFeature }
            };

            _map.Layers.Add(userLayer);
            VeTuyenDuongDenPoiGanNhat(lat, lng);

            // Chỉ zoom mạnh lần đầu, những lần sau chỉ pan để tránh map giật và khó theo dõi.
            if (!_daZoomLanDau)
            {
                _map.Navigator.CenterOnAndZoomTo(point, _map.Navigator.Resolutions[15]);
                _daZoomLanDau = true;
            }
            else
            {
                _map.Navigator.CenterOn(point);
            }

            BanDo.Refresh();
        });
    }

    private void VeTuyenDuongDenPoiGanNhat(double latNguoiDung, double lngNguoiDung)
    {
        var routeLayerCu = _map.Layers.FirstOrDefault(l => l.Name == "RouteLayer");
        if (routeLayerCu != null)
            _map.Layers.Remove(routeLayerCu);

        if (_danhSachPoiHienTai.Count == 0)
            return;

        var poiGanNhat = _danhSachPoiHienTai
            .OrderBy(p => GeofenceService.TinhKhoangCachMetres(latNguoiDung, lngNguoiDung, p.Lat, p.Lng))
            .FirstOrDefault();

        if (poiGanNhat == null)
            return;

        var (xUser, yUser) = SphericalMercator.FromLonLat(lngNguoiDung, latNguoiDung);
        var (xPoi, yPoi) = SphericalMercator.FromLonLat(poiGanNhat.Lng, poiGanNhat.Lat);

        var lineString = new LineString(new[]
        {
            new Coordinate(xUser, yUser),
            new Coordinate(xPoi, yPoi)
        });

        var routeFeature = new GeometryFeature(lineString);

        // Vẽ tuyến đường 2 lớp: trắng bên dưới + xanh dương bên trên để nổi bật trên mọi nền.
        routeFeature.Styles.Add(new VectorStyle
        {
            Line = new Pen(MapsuiColor.White, 10)
        });
        routeFeature.Styles.Add(new VectorStyle
        {
            Line = new Pen(new MapsuiColor(0, 120, 255), 6)
        });

        // Đánh dấu POI mục tiêu của tuyến để khách du lịch dễ nhận biết đích đến.
        var diemDich = new PointFeature(new MPoint(xPoi, yPoi));
        diemDich.Styles.Add(new SymbolStyle
        {
            Fill = new MapsuiBrush(new MapsuiColor(255, 152, 0)),
            Outline = new Pen(MapsuiColor.White, 3),
            SymbolType = SymbolType.Ellipse,
            SymbolScale = 1.0
        });

        var routeLayer = new MemoryLayer
        {
            Name = "RouteLayer",
            Features = new List<IFeature> { routeFeature, diemDich }
        };

        _map.Layers.Add(routeLayer);
    }
}
