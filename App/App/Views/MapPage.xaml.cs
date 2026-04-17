using App.Models;
using App.Services;
using App.ViewModels;
#if ANDROID
using Android.App;
#endif
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Storage;

namespace App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;
    private static bool _daCanhBaoGoogleMapsKey;
    private static readonly string[] BaseImageUrls =
    {
        "http://10.0.2.2:5099",
        "http://localhost:5099",
        "https://10.0.2.2:7099",
        "https://localhost:7099"
    };

    private bool _daZoomLanDau;
    private bool _daCanhKhungTheoPoi;
    private readonly List<PoiModel> _danhSachPoiHienTai = new();

    private readonly List<Pin> _pinsPoi = new();
    private readonly List<Circle> _vungPoi = new();

    private Circle? _vungSangViTriNguoiDung;
    private Circle? _chamViTriNguoiDung;
    private Polyline? _tuyenDuongNen;
    private Polyline? _tuyenDuongChinh;
    private PoiModel? _poiDangTracking;
    private PoiModel? _poiDangMoPopup;
    private Location? _viTriNguoiDungHienTai;

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
        await CanhBaoNeuChuaCauHinhGoogleMapsKeyAsync();
        await _vm.KhoiDongAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        LocalizationResourceManager.Instance.PropertyChanged -= OnLocalizationChanged;
        _vm.DungGps();
        _daZoomLanDau = false;
        _daCanhKhungTheoPoi = false;
        PoiPopupOverlay.IsVisible = false;
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

        if (sender is not Pin pin)
            return;

        var poiDuocChon = TimPoiTheoPin(pin);
        if (poiDuocChon == null)
            return;

        HienThiPopupPoi(poiDuocChon);
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

    private async Task CanhBaoNeuChuaCauHinhGoogleMapsKeyAsync()
    {
#if ANDROID
        if (_daCanhBaoGoogleMapsKey)
            return;

        var context = Android.App.Application.Context;
        var resources = context.Resources;
        if (resources == null)
            return;

        int resourceId = resources.GetIdentifier("google_maps_key", "string", context.PackageName);
        if (resourceId <= 0)
            return;

        string apiKey = context.GetString(resourceId) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey) ||
            apiKey.Contains("REPLACE_WITH_GOOGLE_MAPS_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            _daCanhBaoGoogleMapsKey = true;
            await DisplayAlert(
                "Google Maps chưa cấu hình",
                "Bản đồ đang trống vì chưa có Google Maps API key hợp lệ. Hãy thay giá trị google_maps_key trong Platforms/Android/Resources/values/google_maps_api.xml, bật Maps SDK for Android và Billing trong Google Cloud.",
                "Đã hiểu");
        }
#endif
    }

    private void CapNhatViTriBanDo(double lat, double lng)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var viTriNguoiDung = new Location(lat, lng);
            _viTriNguoiDungHienTai = viTriNguoiDung;

            if (_vungSangViTriNguoiDung == null || _chamViTriNguoiDung == null)
            {
                _vungSangViTriNguoiDung = new Circle
                {
                    Center = viTriNguoiDung,
                    Radius = Distance.FromMeters(14),
                    StrokeColor = Color.FromRgba(66, 133, 244, 80),
                    StrokeWidth = 1,
                    FillColor = Color.FromRgba(66, 133, 244, 45)
                };

                _chamViTriNguoiDung = new Circle
                {
                    Center = viTriNguoiDung,
                    Radius = Distance.FromMeters(4),
                    StrokeColor = Colors.White,
                    StrokeWidth = 2,
                    FillColor = Color.FromArgb("#4285F4")
                };

                BanDo.MapElements.Add(_vungSangViTriNguoiDung);
                BanDo.MapElements.Add(_chamViTriNguoiDung);
            }
            else
            {
                _vungSangViTriNguoiDung.Center = viTriNguoiDung;
                _chamViTriNguoiDung.Center = viTriNguoiDung;
            }

            CapNhatTuyenDuong(lat, lng);

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

    private void CapNhatTuyenDuong(double latNguoiDung, double lngNguoiDung)
    {
        if (_danhSachPoiHienTai.Count == 0)
            return;

        var viTriNguoiDung = new Location(latNguoiDung, lngNguoiDung);
        var diemDich = _poiDangTracking ?? TimPoiGanNhat(latNguoiDung, lngNguoiDung);

        if (diemDich == null)
            return;

        if (_poiDangTracking != null && !_danhSachPoiHienTai.Any(p => p.Id == _poiDangTracking.Id))
        {
            _poiDangTracking = null;
            diemDich = TimPoiGanNhat(latNguoiDung, lngNguoiDung);

            if (diemDich == null)
                return;
        }

        VeTuyenDuongDenPoi(viTriNguoiDung, diemDich);
    }

    private void VeTuyenDuongDenPoi(Location viTriNguoiDung, PoiModel poi)
    {
        if (_tuyenDuongNen != null)
            BanDo.MapElements.Remove(_tuyenDuongNen);

        if (_tuyenDuongChinh != null)
            BanDo.MapElements.Remove(_tuyenDuongChinh);

        var viTriPoi = new Location(poi.Lat, poi.Lng);

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

        BanDo.MapElements.Add(_tuyenDuongNen);
        BanDo.MapElements.Add(_tuyenDuongChinh);
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

    private PoiModel? TimPoiTheoPin(Pin pin)
    {
        const double nguongSaiSoDo = 0.00002;
        return _danhSachPoiHienTai.FirstOrDefault(p =>
            Math.Abs(p.Lat - pin.Location.Latitude) < nguongSaiSoDo &&
            Math.Abs(p.Lng - pin.Location.Longitude) < nguongSaiSoDo &&
            string.Equals(p.Ten, pin.Label, StringComparison.OrdinalIgnoreCase));
    }

    private PoiModel? TimPoiGanNhat(double latNguoiDung, double lngNguoiDung)
    {
        return _danhSachPoiHienTai
            .OrderBy(p => GeofenceService.TinhKhoangCachMetres(latNguoiDung, lngNguoiDung, p.Lat, p.Lng))
            .FirstOrDefault();
    }

    private void CanhKhungTheoNguoiDungVaPoi(Location viTriNguoiDung, PoiModel poi)
    {
        var minLat = Math.Min(viTriNguoiDung.Latitude, poi.Lat);
        var maxLat = Math.Max(viTriNguoiDung.Latitude, poi.Lat);
        var minLng = Math.Min(viTriNguoiDung.Longitude, poi.Lng);
        var maxLng = Math.Max(viTriNguoiDung.Longitude, poi.Lng);

        const double minSpan = 0.0035;
        var latSpan = Math.Max(maxLat - minLat, minSpan) * 1.4;
        var lngSpan = Math.Max(maxLng - minLng, minSpan) * 1.4;

        var center = new Location((minLat + maxLat) / 2, (minLng + maxLng) / 2);
        BanDo.MoveToRegion(new MapSpan(center, latSpan, lngSpan));
    }

    private void HienThiPopupPoi(PoiModel poi)
    {
        _poiDangMoPopup = poi;
        PoiPopupTitle.Text = poi.Ten;
        PoiPopupDescription.Text = ChonMoTaTheoNgonNgu(poi);

        var urlAnh = TaoUrlAnhMinhHoa(poi.TenFileAnhMinhHoa);
        if (!string.IsNullOrWhiteSpace(urlAnh))
        {
            PoiPopupImage.Source = ImageSource.FromUri(new Uri(urlAnh));
            PoiPopupImage.IsVisible = true;
        }
        else
        {
            PoiPopupImage.Source = null;
            PoiPopupImage.IsVisible = false;
        }

        PoiPopupOverlay.IsVisible = true;
    }

    private static string? TaoUrlAnhMinhHoa(string? tenFileAnh)
    {
        if (string.IsNullOrWhiteSpace(tenFileAnh))
            return null;

        var tenDaChuanHoa = tenFileAnh.Trim();
        if (Uri.TryCreate(tenDaChuanHoa, UriKind.Absolute, out var uri))
            return uri.ToString();

        var baseUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? BaseImageUrls.First()
            : BaseImageUrls.Skip(1).First();

        return $"{baseUrl.TrimEnd('/')}/images/poi/{tenDaChuanHoa}";
    }

    private void DongPopup_Clicked(object? sender, EventArgs e)
    {
        PoiPopupOverlay.IsVisible = false;
    }

    private void TrackingPopup_Clicked(object? sender, EventArgs e)
    {
        if (_poiDangMoPopup == null)
            return;

        _poiDangTracking = _poiDangMoPopup;
        PoiPopupOverlay.IsVisible = false;

        if (_viTriNguoiDungHienTai != null)
        {
            VeTuyenDuongDenPoi(_viTriNguoiDungHienTai, _poiDangTracking);
            CanhKhungTheoNguoiDungVaPoi(_viTriNguoiDungHienTai, _poiDangTracking);
        }
    }
}
