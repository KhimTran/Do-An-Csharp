using App.Models;
using App.Services;
using App.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Storage;

namespace App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;

    private bool _daZoomLanDau;
    private bool _daCanhKhungTheoPoi;
    private readonly List<PoiModel> _danhSachPoiHienTai = new();

    private readonly List<Pin> _pinsPoi = new();
    private readonly List<Circle> _vungPoi = new();

    private Pin? _pinViTriNguoiDung;
    private Pin? _pinDiemDich;
    private Polyline? _tuyenDuongNen;
    private Polyline? _tuyenDuongChinh;

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
        var trungTamVinhKhanh = new Location(10.7605, 106.7002);
        BanDo.MoveToRegion(MapSpan.FromCenterAndRadius(trungTamVinhKhanh, Distance.FromMeters(900)));
    }

    private void ThemPinLenBanDo(List<PoiModel> danhSach)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _danhSachPoiHienTai.Clear();
            _danhSachPoiHienTai.AddRange(danhSach);

            foreach (var pin in _pinsPoi)
            {
                pin.MarkerClicked -= OnPoiMarkerClicked;
                BanDo.Pins.Remove(pin);
            }
            _pinsPoi.Clear();

            foreach (var circle in _vungPoi)
                BanDo.MapElements.Remove(circle);
            _vungPoi.Clear();

            foreach (var poi in danhSach)
            {
                var moTa = ChonMoTaTheoNgonNgu(poi);

                var pin = new Pin
                {
                    Label = poi.Ten,
                    Address = moTa,
                    Type = PinType.Place,
                    Location = new Location(poi.Lat, poi.Lng)
                };
                pin.MarkerClicked += OnPoiMarkerClicked;

                _pinsPoi.Add(pin);
                BanDo.Pins.Add(pin);

                var vungPoi = new Circle
                {
                    Center = pin.Location,
                    Radius = Distance.FromMeters(poi.BanKinh > 0 ? poi.BanKinh : 100),
                    StrokeColor = Color.FromArgb("#1E88E5"),
                    StrokeWidth = 2,
                    FillColor = Color.FromRgba(30, 136, 229, 42)
                };

                _vungPoi.Add(vungPoi);
                BanDo.MapElements.Add(vungPoi);
            }

            CanhKhungBanDoTheoDanhSachPoiNeuCan();
        });
    }

    private async void OnPoiMarkerClicked(object? sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;

        if (sender is not Pin pin || string.IsNullOrWhiteSpace(pin.Address))
            return;

        await DisplayAlert(
            string.IsNullOrWhiteSpace(pin.Label) ? "POI" : pin.Label,
            pin.Address,
            LocalizationResourceManager.Instance["Common_Close"]);
    }

    private void LamMoiMoTaPoiTheoNgonNgu()
    {
        if (_danhSachPoiHienTai.Count == 0) return;
        ThemPinLenBanDo(_danhSachPoiHienTai.ToList());
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
            var viTriNguoiDung = new Location(lat, lng);

            if (_pinViTriNguoiDung == null)
            {
                _pinViTriNguoiDung = new Pin
                {
                    Label = LocalizationResourceManager.Instance["MapPage_LegendUser"],
                    Type = PinType.SavedPin,
                    Location = viTriNguoiDung
                };
                BanDo.Pins.Add(_pinViTriNguoiDung);
            }
            else
            {
                _pinViTriNguoiDung.Location = viTriNguoiDung;
                _pinViTriNguoiDung.Label = LocalizationResourceManager.Instance["MapPage_LegendUser"];
            }

            VeTuyenDuongDenPoiGanNhat(lat, lng);

            if (!_daZoomLanDau)
            {
                BanDo.MoveToRegion(MapSpan.FromCenterAndRadius(viTriNguoiDung, Distance.FromMeters(250)));
                _daZoomLanDau = true;
            }
            else if (BanDo.VisibleRegion is MapSpan khungHienTai)
            {
                BanDo.MoveToRegion(new MapSpan(viTriNguoiDung, khungHienTai.LatitudeDegrees, khungHienTai.LongitudeDegrees));
            }
        });
    }

    private void VeTuyenDuongDenPoiGanNhat(double latNguoiDung, double lngNguoiDung)
    {
        if (_tuyenDuongNen != null)
            BanDo.MapElements.Remove(_tuyenDuongNen);

        if (_tuyenDuongChinh != null)
            BanDo.MapElements.Remove(_tuyenDuongChinh);

        if (_pinDiemDich != null)
            BanDo.Pins.Remove(_pinDiemDich);

        if (_danhSachPoiHienTai.Count == 0)
            return;

        var poiGanNhat = _danhSachPoiHienTai
            .OrderBy(p => GeofenceService.TinhKhoangCachMetres(latNguoiDung, lngNguoiDung, p.Lat, p.Lng))
            .FirstOrDefault();

        if (poiGanNhat == null)
            return;

        var viTriNguoiDung = new Location(latNguoiDung, lngNguoiDung);
        var viTriPoi = new Location(poiGanNhat.Lat, poiGanNhat.Lng);

        _tuyenDuongNen = new Polyline
        {
            StrokeColor = Colors.White,
            StrokeWidth = 9,
            Geopath = { viTriNguoiDung, viTriPoi }
        };

        _tuyenDuongChinh = new Polyline
        {
            StrokeColor = Color.FromArgb("#0078FF"),
            StrokeWidth = 6,
            Geopath = { viTriNguoiDung, viTriPoi }
        };

        _pinDiemDich = new Pin
        {
            Label = poiGanNhat.Ten,
            Type = PinType.SearchResult,
            Location = viTriPoi
        };

        BanDo.MapElements.Add(_tuyenDuongNen);
        BanDo.MapElements.Add(_tuyenDuongChinh);
        BanDo.Pins.Add(_pinDiemDich);
    }

    private void CanhKhungBanDoTheoDanhSachPoiNeuCan()
    {
        if (_daCanhKhungTheoPoi || _danhSachPoiHienTai.Count == 0)
            return;

        var minLat = _danhSachPoiHienTai.Min(p => p.Lat);
        var maxLat = _danhSachPoiHienTai.Max(p => p.Lat);
        var minLng = _danhSachPoiHienTai.Min(p => p.Lng);
        var maxLng = _danhSachPoiHienTai.Max(p => p.Lng);

        const double minSpan = 0.0045;
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

        BanDo.MoveToRegion(new MapSpan(
            new Location(centerLatFinal, centerLngFinal),
            (maxLat - minLat) * 1.35,
            (maxLng - minLng) * 1.35));

        _daCanhKhungTheoPoi = true;
    }
}
