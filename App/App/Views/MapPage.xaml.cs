using App.Models;
using App.Services;
using App.ViewModels;
using System.Collections;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Microsoft.Maui.Storage;
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
    private bool _daCanhKhungTheoPoi = false;
    private readonly List<PoiModel> _danhSachPoiHienTai = new();

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.OnDaCoiPoi += ThemPinLenBanDo;
        _vm.OnViTriCapNhat += CapNhatViTriBanDo;
        BanDo.Info += async (s, e) => await HienThiThongTinPoiTuSuKienAsync(e);

        KhoiTaoBanDo();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LocalizationResourceManager.Instance.PropertyChanged += OnLocalizationChanged;
        await _vm.KhoiDongAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        LocalizationResourceManager.Instance.PropertyChanged -= OnLocalizationChanged;
        _vm.DungGps();
        _daZoomLanDau = false;
        _daCanhKhungTheoPoi = false;
    }

    private void KhoiTaoBanDo()
    {
        _map = new Mapsui.Map();
        _map.Widgets.Clear();

        // Dùng OpenStreetMap tile mặc định, thêm lớp đường dẫn trực quan ở phía trên để dễ nhìn đường.
        _map.Layers.Add(OpenStreetMap.CreateTileLayer());

        var center = SphericalMercator.FromLonLat(106.7002, 10.7605);
        _map.Navigator.CenterOnAndZoomTo(new MPoint(center.x, center.y), _map.Navigator.Resolutions[14]);

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
                    ["Loai"] = "POI",
                    ["MoTa"] = ChonMoTaTheoNgonNgu(poi)
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
                circleFeature["Ten"] = poi.Ten;
                circleFeature["Loai"] = "POI";
                circleFeature["MoTa"] = ChonMoTaTheoNgonNgu(poi);
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
            CanhKhungBanDoTheoDanhSachPoiNeuCan();
            BanDo.Refresh();
        });
    }

    private async Task HienThiThongTinPoiKhiBamAsync(IFeature? feature)
    {
        if (feature == null) return;
        string loai = DocThuocTinhFeature(feature, "Loai");
        if (!string.Equals(loai, "POI", StringComparison.OrdinalIgnoreCase)) return;

        string ten = DocThuocTinhFeature(feature, "Ten");
        string moTa = DocThuocTinhFeature(feature, "MoTa");
        if (string.IsNullOrWhiteSpace(ten)) ten = "POI";
        if (string.IsNullOrWhiteSpace(moTa)) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert(
                ten,
                moTa,
                LocalizationResourceManager.Instance["Common_Close"]);
        });
    }

    private async Task HienThiThongTinPoiTuSuKienAsync(object? eventArgs)
    {
        if (eventArgs == null) return;
        var feature = LayFeatureTuInfoEvent(eventArgs);
        await HienThiThongTinPoiKhiBamAsync(feature);
    }

    private static IFeature? LayFeatureTuInfoEvent(object eventArgs)
    {
        var eventType = eventArgs.GetType();

        // Một số phiên bản Mapsui có eventArgs.Feature
        if (eventType.GetProperty("Feature")?.GetValue(eventArgs) is IFeature featureTrucTiep)
            return featureTrucTiep;

        // Một số phiên bản Mapsui có eventArgs.MapInfo.Feature
        var mapInfo = eventType.GetProperty("MapInfo")?.GetValue(eventArgs);
        if (mapInfo != null && mapInfo.GetType().GetProperty("Feature")?.GetValue(mapInfo) is IFeature featureTrongMapInfo)
            return featureTrongMapInfo;

        // Một số phiên bản trả tập hợp eventArgs.MapInfos[]
        if (eventType.GetProperty("MapInfos")?.GetValue(eventArgs) is IEnumerable mapInfos)
        {
            foreach (var item in mapInfos)
            {
                if (item?.GetType().GetProperty("Feature")?.GetValue(item) is IFeature featureTuDanhSach)
                    return featureTuDanhSach;
            }
        }

        return null;
    }

    private void LamMoiMoTaPoiTheoNgonNgu()
    {
        if (_danhSachPoiHienTai.Count == 0) return;
        ThemPinLenBanDo(_danhSachPoiHienTai.ToList());
    }

    private static string DocThuocTinhFeature(IFeature feature, string key)
    {
        try
        {
            return feature[key]?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ChonMoTaTheoNgonNgu(PoiModel poi)
    {
        string maNgonNgu = Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));

        if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(poi.MoTa_En) ? poi.MoTa_Vi : poi.MoTa_En;

        if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(poi.MoTa_Zh) ? poi.MoTa_Vi : poi.MoTa_Zh;

        return poi.MoTa_Vi;
    }

    private void OnLocalizationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        LamMoiMoTaPoiTheoNgonNgu();
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
                _map.Navigator.CenterOnAndZoomTo(point, _map.Navigator.Resolutions[17]);
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
            Line = new Pen(new MapsuiColor(0, 120, 255), 7)
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

    private void CanhKhungBanDoTheoDanhSachPoiNeuCan()
    {
        if (_daCanhKhungTheoPoi || _danhSachPoiHienTai.Count == 0)
            return;

        var minLat = _danhSachPoiHienTai.Min(p => p.Lat);
        var maxLat = _danhSachPoiHienTai.Max(p => p.Lat);
        var minLng = _danhSachPoiHienTai.Min(p => p.Lng);
        var maxLng = _danhSachPoiHienTai.Max(p => p.Lng);

        // Nếu POI tập trung quá sát nhau thì chủ động mở rộng khung một chút để thấy rõ đường xung quanh.
        const double minSpan = 0.0045; // ~500m
        if ((maxLat - minLat) < minSpan)
        {
            var centerLat = (minLat + maxLat) / 2;
            minLat = centerLat - minSpan / 2;
            maxLat = centerLat + minSpan / 2;
        }

        if ((maxLng - minLng) < minSpan)
        {
            var centerLng = (minLng + maxLng) / 2;
            minLng = centerLng - minSpan / 2;
            maxLng = centerLng + minSpan / 2;
        }

        var centerLatFinal = (minLat + maxLat) / 2;
        var centerLngFinal = (minLng + maxLng) / 2;
        var (centerX, centerY) = SphericalMercator.FromLonLat(centerLngFinal, centerLatFinal);
        _map.Navigator.CenterOnAndZoomTo(new MPoint(centerX, centerY), _map.Navigator.Resolutions[15]);
        _daCanhKhungTheoPoi = true;
    }
}
