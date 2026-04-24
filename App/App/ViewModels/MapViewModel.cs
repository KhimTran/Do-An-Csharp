using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace App.ViewModels
{
    public partial class MapViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly SyncService _sync;
        private readonly ILocationService _gps;
        private readonly GeofenceService _geofence;
        private readonly ITtsService _tts;
        private readonly AnalyticsService _analytics;

        private List<PoiModel> _danhSachPoi = [];
        private PoiModel? _poiDangTracking;
        private PoiModel? _poiDangMoPopup;
        private LocationSnapshot? _viTriNguoiDungHienTai;
        private bool _dangXuLyViTri;
        private bool _daKhoiDong;
        private bool _daCanhKhungTheoPoi;
        private bool _daCanhTheoNguoiDung;
        private int? _poiCanMoSauDieuHuong;
        private int? _poiCanCanhToi;

        public MapViewModel(
            LocalDatabase db,
            SyncService sync,
            ILocationService gps,
            GeofenceService geofence,
            ITtsService tts,
            AnalyticsService analytics)
        {
            _db = db;
            _sync = sync;
            _gps = gps;
            _geofence = geofence;
            _tts = tts;
            _analytics = analytics;

            TenPoiGanNhat = LocalizationResourceManager.Instance["MapPage_NoNearest"];
            TrangThaiGps = LocalizationResourceManager.Instance["MapPage_WaitingGps"];
            TrangThaiPhat = LocalizationResourceManager.Instance["MapPage_PlaybackIdle"];
            _tts.PlaybackStateChanged += Tts_PlaybackStateChanged;
        }

        [ObservableProperty]
        private string tenPoiGanNhat = string.Empty;

        [ObservableProperty]
        private double khoangCachGanNhat;

        [ObservableProperty]
        private bool coPoiGanNhat;

        [ObservableProperty]
        private string thongBaoBanDo = string.Empty;

        [ObservableProperty]
        private string trangThaiGps = string.Empty;

        [ObservableProperty]
        private string trangThaiPhat = string.Empty;

        [ObservableProperty]
        private bool hienThiPopupPoi;

        [ObservableProperty]
        private string tieuDePopupPoi = string.Empty;

        [ObservableProperty]
        private string moTaPopupPoi = string.Empty;

        [ObservableProperty]
        private string? urlAnhPopupPoi;

        [ObservableProperty]
        private bool coAnhPopupPoi;

        public event Action<MapRenderState>? MapStateChanged;

        public async Task KhoiDongAsync()
        {
            if (_daKhoiDong)
                return;

            _daKhoiDong = true;
            _geofence.ResetTrangThaiTamThoi();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ThongBaoBanDo = LocalizationResourceManager.Instance["PoiSync_Start"];
                TrangThaiGps = LocalizationResourceManager.Instance["MapPage_WaitingGps"];
            });

            bool daDongBo = await _sync.DongBoPoisAsync();
            _danhSachPoi = await _db.LayTatCaPoiAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ThongBaoBanDo = daDongBo
                    ? LocalizationResourceManager.Instance.Translate("PoiSync_Done", _danhSachPoi.Count)
                    : LocalizationResourceManager.Instance.Translate("PoiSync_Offline", _danhSachPoi.Count, _sync.LastError);
            });

            PhatMapState(fitToPois: !_daCanhKhungTheoPoi);
            _daCanhKhungTheoPoi = _danhSachPoi.Count > 0;

            var viTriHienTai = await _gps.LayViTriHienTaiAsync();
            if (viTriHienTai != null)
            {
                _viTriNguoiDungHienTai = viTriHienTai;
                await XuLyViTriMoiAsync(viTriHienTai, followUser: !_daCanhTheoNguoiDung);
                _daCanhTheoNguoiDung = true;
            }

            await _gps.BatDauTheoDoiAsync(
                khiCoViTri: snapshot =>
                {
                    _viTriNguoiDungHienTai = snapshot;
                    bool canTheoNguoiDung = !_daCanhTheoNguoiDung;
                    if (canTheoNguoiDung)
                        _daCanhTheoNguoiDung = true;

                    _ = XuLyViTriMoiAsync(snapshot, followUser: canTheoNguoiDung);
                },
                khiTrangThaiThayDoi: CapNhatTrangThaiGps);
        }

        public void DungGps()
        {
            _gps.DungTheoDoi();
            _daKhoiDong = false;
            _dangXuLyViTri = false;
            _daCanhKhungTheoPoi = false;
            _daCanhTheoNguoiDung = false;
        }

        public async Task ChonPoiTuMapAsync(int poiId)
        {
            var poi = _danhSachPoi.FirstOrDefault(p => p.Id == poiId);
            if (poi == null)
                return;

            _poiDangMoPopup = poi;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CapNhatPopupPoi(poi);
                HienThiPopupPoi = true;
            });

            PhatMapState();
        }

        public async Task<bool> MoPoiTuDieuHuongAsync(int poiId)
        {
            if (poiId <= 0)
                return false;

            if (_danhSachPoi.Count == 0)
            {
                _poiCanMoSauDieuHuong = poiId;
                return false;
            }

            var poi = _danhSachPoi.FirstOrDefault(p => p.Id == poiId);
            if (poi == null)
            {
                _poiCanMoSauDieuHuong = null;
                return false;
            }

            _poiDangTracking = null;
            _poiDangMoPopup = poi;
            _poiCanMoSauDieuHuong = null;
            _poiCanCanhToi = poi.Id;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CapNhatPopupPoi(poi);
                HienThiPopupPoi = true;
            });

            PhatMapState();
            return true;
        }

        public async Task XuLyPoiDangChoTuDieuHuongAsync()
        {
            int pendingPoiId = Preferences.Get(AppNavigationKeys.PendingMapPoiId, 0);
            if (pendingPoiId > 0)
            {
                Preferences.Remove(AppNavigationKeys.PendingMapPoiId);

                if (!await MoPoiTuDieuHuongAsync(pendingPoiId))
                    _poiCanMoSauDieuHuong = pendingPoiId;

                return;
            }

            if (_poiCanMoSauDieuHuong.HasValue)
                await MoPoiTuDieuHuongAsync(_poiCanMoSauDieuHuong.Value);
        }

        public async Task DongPopupAsync(bool capNhatBanDo = true)
        {
            _poiDangMoPopup = null;
            _poiCanCanhToi = null;
            await MainThread.InvokeOnMainThreadAsync(() => HienThiPopupPoi = false);

            if (capNhatBanDo)
                PhatMapState();
        }

        public Task DongPopupNeuTrungAsync(int poiId)
        {
            if (_poiDangMoPopup?.Id != poiId)
                return Task.CompletedTask;

            return DongPopupAsync();
        }

        public async Task BatDauTrackingPoiDangMoAsync()
        {
            if (_poiDangMoPopup == null)
                return;

            _poiDangTracking = _poiDangMoPopup;
            _poiCanCanhToi = null;
            await DongPopupAsync(capNhatBanDo: false);
            PhatMapState(focusOnRoute: _viTriNguoiDungHienTai != null);
        }

        public Task LamMoiNoiDungHienThiAsync()
        {
            if (_poiDangMoPopup != null)
            {
                CapNhatPopupPoi(_poiDangMoPopup);
            }

            if (string.IsNullOrWhiteSpace(TenPoiGanNhat))
                TenPoiGanNhat = LocalizationResourceManager.Instance["MapPage_NoNearest"];

            PhatMapState();
            return Task.CompletedTask;
        }

        private async Task XuLyViTriMoiAsync(LocationSnapshot snapshot, bool followUser = false)
        {
            if (_dangXuLyViTri)
                return;

            _dangXuLyViTri = true;

            try
            {
                var lat = snapshot.Lat;
                var lng = snapshot.Lng;

                PoiModel? ganNhat = null;
                double minKc = double.MaxValue;

                foreach (var poi in _danhSachPoi)
                {
                    double kc = GeofenceService.TinhKhoangCachMetres(lat, lng, poi.Lat, poi.Lng);
                    if (kc < minKc)
                    {
                        minKc = kc;
                        ganNhat = poi;
                    }
                }

                int banKinhMacDinh = Preferences.Get("geofence_radius", 100);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (ganNhat == null)
                    {
                        TenPoiGanNhat = LocalizationResourceManager.Instance["MapPage_NoNearest"];
                        KhoangCachGanNhat = 0;
                        CoPoiGanNhat = false;
                    }
                    else
                    {
                        TenPoiGanNhat = ganNhat.Ten;
                        KhoangCachGanNhat = minKc;
                        CoPoiGanNhat = minKc <= banKinhMacDinh * 3;
                    }
                });

                _ = _analytics.GuiRoutePingAsync(lat, lng, "GPS");
                _ = _analytics.GuiHeartbeatAsync();

                bool forceReread = Preferences.Get("force_reread_once", false);
                if (forceReread && ganNhat != null && minKc <= banKinhMacDinh)
                {
                    await DocThuyetMinhTheoNgonNguAsync(ganNhat, "GPS");
                    Preferences.Set("force_reread_once", false);
                }
                else
                {
                    var geofenceResult = await _geofence.KiemTraVungChiTietAsync(lat, lng);
                    GhiLogGeofence(geofenceResult);

                    switch (geofenceResult.Status)
                    {
                        case GeofenceService.GeofenceCheckStatus.Triggered when geofenceResult.Poi != null:
                            await DocThuyetMinhTheoNgonNguAsync(geofenceResult.Poi, "GPS");
                            break;
                        case GeofenceService.GeofenceCheckStatus.Cooldown:
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                TrangThaiPhat = LocalizationResourceManager.Instance["MapPage_PlaybackCooldown"];
                            });
                            break;
                    }
                }

                PhatMapState(followUser: followUser);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Loi xu ly vi tri: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    TrangThaiGps = LocalizationResourceManager.Instance.Translate("MapPage_GpsError", ex.Message);
                });
            }
            finally
            {
                _dangXuLyViTri = false;
            }
        }

        private void CapNhatTrangThaiGps(LocationTrackingStatus status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TrangThaiGps = status.State switch
                {
                    LocationTrackingState.Tracking => LocalizationResourceManager.Instance["MapPage_GpsTracking"],
                    LocationTrackingState.Simulated => LocalizationResourceManager.Instance["MapPage_GpsSimulated"],
                    LocationTrackingState.PermissionDenied => LocalizationResourceManager.Instance["MapPage_GpsPermissionDenied"],
                    LocationTrackingState.Disabled => LocalizationResourceManager.Instance["MapPage_GpsDisabled"],
                    LocationTrackingState.Error when !string.IsNullOrWhiteSpace(status.Details)
                        => LocalizationResourceManager.Instance.Translate("MapPage_GpsError", status.Details),
                    LocationTrackingState.Error => LocalizationResourceManager.Instance["MapPage_GpsUnavailable"],
                    _ => LocalizationResourceManager.Instance["MapPage_WaitingGps"]
                };
            });
        }

        private void PhatMapState(bool fitToPois = false, bool focusOnRoute = false, bool followUser = false)
        {
            MapStateChanged?.Invoke(TaoTrangThaiBanDo(fitToPois, focusOnRoute, followUser));
        }

        private MapRenderState TaoTrangThaiBanDo(bool fitToPois, bool focusOnRoute, bool followUser)
        {
            var geofenceRadius = Preferences.Get("geofence_radius", 100);
            var focusPoiId = _poiCanCanhToi;
            _poiCanCanhToi = null;
            var nearestPoi = _viTriNguoiDungHienTai == null
                ? null
                : TimPoiGanNhat(_viTriNguoiDungHienTai.Lat, _viTriNguoiDungHienTai.Lng);

            var targetPoi = LayPoiDich();
            var route = _viTriNguoiDungHienTai == null || targetPoi == null
                ? null
                : new MapRouteRenderModel
                {
                    Origin = new MapLocationRenderModel
                    {
                        Lat = _viTriNguoiDungHienTai.Lat,
                        Lng = _viTriNguoiDungHienTai.Lng
                    },
                    Destination = new MapLocationRenderModel
                    {
                        Lat = targetPoi.Lat,
                        Lng = targetPoi.Lng
                    },
                    Points =
                    [
                        new MapLocationRenderModel
                        {
                            Lat = _viTriNguoiDungHienTai.Lat,
                            Lng = _viTriNguoiDungHienTai.Lng
                        },
                        new MapLocationRenderModel
                        {
                            Lat = targetPoi.Lat,
                            Lng = targetPoi.Lng
                        }
                    ]
                };

            return new MapRenderState
            {
                Pois = _danhSachPoi.Select(poi => new MapPoiRenderModel
                {
                    Id = poi.Id,
                    Ten = poi.Ten,
                    MoTa = ChonMoTaTheoNgonNgu(poi),
                    ImageUrl = BuildPoiImageUrl(poi),
                    Lat = poi.Lat,
                    Lng = poi.Lng,
                    BanKinh = Math.Max(poi.BanKinh, geofenceRadius),
                    IsNearest = nearestPoi?.Id == poi.Id,
                    IsTracking = _poiDangTracking?.Id == poi.Id
                }).ToList(),
                UserLocation = _viTriNguoiDungHienTai == null
                    ? null
                    : new MapLocationRenderModel
                    {
                        Lat = _viTriNguoiDungHienTai.Lat,
                        Lng = _viTriNguoiDungHienTai.Lng
                    },
                Route = route,
                Bounds = TaoBoundsTheoDanhSachPoi(),
                NearestPoiId = nearestPoi?.Id,
                TrackingPoiId = _poiDangTracking?.Id,
                PopupPoiId = _poiDangMoPopup?.Id,
                FocusPoiId = focusPoiId,
                FitToPois = fitToPois,
                FocusOnRoute = focusOnRoute,
                FollowUser = followUser
            };
        }

        private MapBoundsRenderModel? TaoBoundsTheoDanhSachPoi()
        {
            if (_danhSachPoi.Count == 0)
                return null;

            return new MapBoundsRenderModel
            {
                MinLat = _danhSachPoi.Min(p => p.Lat),
                MinLng = _danhSachPoi.Min(p => p.Lng),
                MaxLat = _danhSachPoi.Max(p => p.Lat),
                MaxLng = _danhSachPoi.Max(p => p.Lng)
            };
        }

        private PoiModel? LayPoiDich()
        {
            if (_poiDangTracking != null)
            {
                _poiDangTracking = _danhSachPoi.FirstOrDefault(p => p.Id == _poiDangTracking.Id);
                if (_poiDangTracking != null)
                    return _poiDangTracking;
            }

            if (_viTriNguoiDungHienTai == null)
                return null;

            return TimPoiGanNhat(_viTriNguoiDungHienTai.Lat, _viTriNguoiDungHienTai.Lng);
        }

        private PoiModel? TimPoiGanNhat(double latNguoiDung, double lngNguoiDung)
        {
            return _danhSachPoi
                .OrderBy(p => GeofenceService.TinhKhoangCachMetres(latNguoiDung, lngNguoiDung, p.Lat, p.Lng))
                .FirstOrDefault();
        }

        private void CapNhatPopupPoi(PoiModel poi)
        {
            TieuDePopupPoi = poi.Ten;
            MoTaPopupPoi = ChonMoTaTheoNgonNgu(poi);
            UrlAnhPopupPoi = BuildPoiImageUrl(poi);
            CoAnhPopupPoi = !string.IsNullOrWhiteSpace(UrlAnhPopupPoi);
        }

        private static string? BuildPoiImageUrl(PoiModel poi)
            => ApiEndpointResolver.BuildPoiImageUrl(poi.TenFileAnhMinhHoa);

        private async Task DocThuyetMinhTheoNgonNguAsync(PoiModel poi, string nguon)
        {
            string maNgonNgu = Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));
            string noiDung = PoiDescriptionResolver.GetBestDescription(poi, maNgonNgu);

            if (string.IsNullOrWhiteSpace(noiDung))
            {
                TrangThaiPhat = LocalizationResourceManager.Instance["MapPage_PlaybackEmpty"];
                _geofence.HoanTacLanKichHoat(poi.Id);
                return;
            }

            string khoaAmThanh = $"poi:{poi.Id}:{RutGonMaNgonNgu(maNgonNgu)}";
            TrangThaiPhat = LocalizationResourceManager.Instance.Translate("MapPage_PlaybackStarted", poi.Ten);
            var ketQuaPhat = await _tts.PhatAmAsync(noiDung, maNgonNgu, khoaAmThanh, poi.Ten);
            if (!ketQuaPhat.Completed)
            {
                TrangThaiPhat = LocalizationResourceManager.Instance.Translate("MapPage_PlaybackFailed", poi.Ten);
                _geofence.HoanTacLanKichHoat(poi.Id);
                return;
            }

            TrangThaiPhat = LocalizationResourceManager.Instance.Translate("MapPage_PlaybackCompleted", poi.Ten);
            if (!ketQuaPhat.CreatedNewSession)
                return;

            await _db.GhiLichSuPhatAsync(new LichSuPhatModel
            {
                PoiId = poi.Id,
                TenPoi = poi.Ten,
                NgonNgu = RutGonMaNgonNgu(maNgonNgu),
                ThoiGianPhat = DateTime.Now,
                NguonKichHoat = nguon
            });

            int thoiLuongGiay = AnalyticsService.UocTinhThoiLuongGiay(noiDung);
            await _analytics.GuiLogAsync(poi.Id, poi.Ten, nguon, thoiLuongGiay);
        }

        private static void GhiLogGeofence(GeofenceService.GeofenceCheckResult result)
        {
            var poiTen = result.Poi?.Ten ?? result.NearestPoi?.Ten ?? "none";
            var khoangCach = double.IsFinite(result.DistanceMeters) ? result.DistanceMeters.ToString("0.##") : "n/a";
            var banKinh = double.IsFinite(result.RadiusMeters) ? result.RadiusMeters.ToString("0.##") : "n/a";

            System.Diagnostics.Debug.WriteLine(
                $"[Geofence] poi={poiTen}, distance={khoangCach}m, radius={banKinh}m, status={result.Status}, reason={result.Reason}");
        }

        private void Tts_PlaybackStateChanged(object? sender, TtsPlaybackStateChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.TenNoiDungHienThi))
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TrangThaiPhat = e.State switch
                {
                    TtsPlaybackState.Started => LocalizationResourceManager.Instance.Translate("MapPage_PlaybackStarted", e.TenNoiDungHienThi),
                    TtsPlaybackState.Completed => LocalizationResourceManager.Instance.Translate("MapPage_PlaybackCompleted", e.TenNoiDungHienThi),
                    TtsPlaybackState.Failed => LocalizationResourceManager.Instance.Translate("MapPage_PlaybackFailed", e.TenNoiDungHienThi),
                    _ => TrangThaiPhat
                };
            });
        }

        private static string ChonMoTaTheoNgonNgu(PoiModel poi)
        {
            string maNgonNgu = Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));
            return PoiDescriptionResolver.GetBestDescriptionOrDefault(
                poi,
                maNgonNgu,
                LocalizationResourceManager.Instance["MapPage_PlaybackEmpty"]);
        }

        private static string RutGonMaNgonNgu(string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return "en";

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return "zh";

            return "vi";
        }
    }
}


