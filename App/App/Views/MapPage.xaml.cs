using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using Mapsui.Tiling;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using App.ViewModels;
using App.Models;
using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
// Xóa Using Color = Mapsui.Styles.Color để tránh lỗi trùng

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
        _vm.OnDaCoiPoi -= ThemPinLenBanDo;
        _vm.OnViTriCapNhat -= CapNhatViTriBanDo;
    }

    private void BanDo_Loaded(object sender, EventArgs e)
    {
        // Đã sửa: Ghi rõ Mapsui.Map để không bị trùng với Map của MAUI
        var map = new Mapsui.Map();
        // Thêm dòng này để xóa toàn bộ các chữ log, khung fps mặc định
        map.Widgets.Clear();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        BanDo.Map = map;

        // Đã sửa: Chuyển đổi Tuple sang MPoint theo chuẩn Mapsui 5.0
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
                // 1. Chuyển đổi tọa độ
                var tuple = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
                var point = new MPoint(tuple.x, tuple.y);

                // 2. TẠO PIN (Cắm cờ)
                var pinFeature = new PointFeature(point)
                {
                    ["Ten"] = poi.Ten,
                    ["MoTa"] = $"📍 {poi.MoTa_Vi}\n\n🌐 {poi.MoTa_En}\n\n📏 Bán kính: {poi.BanKinh}m"
                };

                // Đã sửa: Mapsui 5.0 yêu cầu tạo Style trực tiếp thay vì dùng SymbolStyles class cũ
                pinFeature.Styles.Add(new SymbolStyle
                {
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red),
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 0.5
                });

                features.Add(pinFeature);

                // 3. TẠO VÒNG TRÒN GEOFENCE
                double radiusInMercator = poi.BanKinh / Math.Cos(poi.Lat * Math.PI / 180.0);

                var pointFactory = new NetTopologySuite.Geometries.Point(point.X, point.Y);
                var circleGeometry = pointFactory.Buffer(radiusInMercator);
                var circleFeature = new GeometryFeature(circleGeometry);

                // Đã sửa: Ghi rõ Mapsui.Styles.Brush để không trùng với Brush của MAUI
                circleFeature.Styles.Add(new VectorStyle
                {
                    Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(0, 150, 255, 50)),
                    Outline = new Pen(Mapsui.Styles.Color.DodgerBlue, 2)
                });

                features.Add(circleFeature);
            }

            // Đã sửa: Mapsui 5.0 dùng thuộc tính Features thay vì DataSource
            var layer = new MemoryLayer
            {
                Name = "PoiLayer",
                Features = features
            };

            BanDo.Map?.Layers.Add(layer);
        });
    }

    // Đã sửa: Ghi rõ Microsoft.Maui.Devices.Sensors.Location để không trùng với Location của thư viện Geofence
    private void CapNhatViTriBanDo(Microsoft.Maui.Devices.Sensors.Location viTri)
    {
        // Xử lý vị trí tại đây
    }
}