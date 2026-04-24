using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class TtsService : ITtsService
    {
        private const int GioiHanHangDoi = 3;

        private readonly object _khoaHangDoi = new();
        private readonly ConcurrentQueue<YeuCauPhatAm> _hangDoi = new();
        private readonly SemaphoreSlim _khoaXuLyHangDoi = new(1, 1);
        private readonly Dictionary<string, YeuCauPhatAm> _yeuCauTheoKhoa = new(StringComparer.Ordinal);

        private CancellationTokenSource? _cts;
        private Locale[]? _boNhoGiongNoi;
        private bool _dangXuLyHangDoi;

        public event EventHandler<TtsPlaybackStateChangedEventArgs>? PlaybackStateChanged;

        public bool DangPhat { get; private set; }

        public async Task<TtsPlaybackResult> PhatAmAsync(
            string vanBan,
            string maNgonNgu = "vi-VN",
            string? khoaAmThanh = null,
            string? tenNoiDungHienThi = null)
        {
            if (string.IsNullOrWhiteSpace(vanBan))
                return TtsPlaybackResult.Rejected("empty");

            if (string.IsNullOrWhiteSpace(maNgonNgu))
                maNgonNgu = Preferences.Get("tts_language", "vi-VN");

            var yeuCau = ThemHoacNhapYeuCau(vanBan, maNgonNgu, khoaAmThanh, tenNoiDungHienThi, out bool canKhoiDongXuLy, out bool laYeuCauGop);
            if (yeuCau == null)
                return TtsPlaybackResult.Rejected("queue-full");

            if (canKhoiDongXuLy)
                _ = XuLyHangDoiAsync();

            var ketQua = await yeuCau.ChoHoanTat.Task;
            if (!laYeuCauGop)
                return ketQua;

            return ketQua.Completed
                ? TtsPlaybackResult.CompletedMergedSession()
                : ketQua.Status == "cancelled"
                    ? TtsPlaybackResult.Cancelled(createdNewSession: false)
                    : ketQua.Status == "failed"
                        ? TtsPlaybackResult.Failed(createdNewSession: false)
                        : TtsPlaybackResult.Rejected(ketQua.Status);
        }

        public void DungPhat()
        {
            List<YeuCauPhatAm> yeuCauDangCho = [];

            lock (_khoaHangDoi)
            {
                while (_hangDoi.TryDequeue(out var item))
                {
                    _yeuCauTheoKhoa.Remove(item.Khoa);
                    yeuCauDangCho.Add(item);
                }
            }

            foreach (var item in yeuCauDangCho)
                item.ChoHoanTat.TrySetResult(TtsPlaybackResult.Cancelled(createdNewSession: true));

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            DangPhat = false;
        }

        private YeuCauPhatAm? ThemHoacNhapYeuCau(
            string vanBan,
            string maNgonNgu,
            string? khoaAmThanh,
            string? tenNoiDungHienThi,
            out bool canKhoiDongXuLy,
            out bool laYeuCauGop)
        {
            canKhoiDongXuLy = false;
            laYeuCauGop = false;
            string khoa = TaoKhoaAmThanh(vanBan, maNgonNgu, khoaAmThanh);

            lock (_khoaHangDoi)
            {
                if (_yeuCauTheoKhoa.TryGetValue(khoa, out var yeuCauDaCo))
                {
                    laYeuCauGop = true;
                    return yeuCauDaCo;
                }

                if (_hangDoi.Count >= GioiHanHangDoi)
                    return null;

                var yeuCauMoi = new YeuCauPhatAm(khoa, vanBan, maNgonNgu, tenNoiDungHienThi);
                _hangDoi.Enqueue(yeuCauMoi);
                _yeuCauTheoKhoa[khoa] = yeuCauMoi;

                if (!_dangXuLyHangDoi)
                {
                    _dangXuLyHangDoi = true;
                    canKhoiDongXuLy = true;
                }

                return yeuCauMoi;
            }
        }

        private async Task XuLyHangDoiAsync()
        {
            await _khoaXuLyHangDoi.WaitAsync();
            try
            {
                while (true)
                {
                    YeuCauPhatAm? item;
                    lock (_khoaHangDoi)
                    {
                        if (!_hangDoi.TryDequeue(out item))
                        {
                            DangPhat = false;
                            _dangXuLyHangDoi = false;
                            break;
                        }

                        DangPhat = true;
                    }

                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();
                    PlaybackStateChanged?.Invoke(this, new TtsPlaybackStateChangedEventArgs(TtsPlaybackState.Started, item.TenNoiDungHienThi));

                    TtsPlaybackResult ketQua;
                    try
                    {
                        ketQua = await PhatTuanTuAsync(item, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        ketQua = TtsPlaybackResult.Cancelled(createdNewSession: true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TTS] Loi: {ex.Message}");
                        ketQua = TtsPlaybackResult.Failed(createdNewSession: true);
                    }
                    finally
                    {
                        lock (_khoaHangDoi)
                        {
                            _yeuCauTheoKhoa.Remove(item.Khoa);
                        }
                    }

                    var state = ketQua.Completed
                        ? TtsPlaybackState.Completed
                        : ketQua.Status == "failed"
                            ? TtsPlaybackState.Failed
                            : TtsPlaybackState.Cancelled;
                    PlaybackStateChanged?.Invoke(this, new TtsPlaybackStateChangedEventArgs(state, item.TenNoiDungHienThi));
                    item.ChoHoanTat.TrySetResult(ketQua);
                }
            }
            finally
            {
                DangPhat = false;
                _cts?.Dispose();
                _cts = null;
                _khoaXuLyHangDoi.Release();
            }
        }

        private async Task<TtsPlaybackResult> PhatTuanTuAsync(YeuCauPhatAm item, CancellationToken token)
        {
            var giongPhuHop = await TimGiongNoiPhuHopAsync(item.MaNgonNgu);
            var vanBanDaLamSach = LamSachVanBan(item.VanBan);
            if (string.IsNullOrWhiteSpace(vanBanDaLamSach))
                return TtsPlaybackResult.Rejected("empty");

            var tuyChinh = new SpeechOptions
            {
                // Muc am luong vua phai de tranh re tieng tren loa ngoai.
                Volume = 0.72f,
                Pitch = 1.0f,
                Locale = giongPhuHop
            };

            foreach (var doan in TachDoanVanBan(vanBanDaLamSach))
            {
                await TextToSpeech.SpeakAsync(doan, tuyChinh, token);
                await Task.Delay(180, token);
            }

            return TtsPlaybackResult.CompletedNewSession();
        }

        private async Task<Locale?> TimGiongNoiPhuHopAsync(string maNgonNgu)
        {
            _boNhoGiongNoi ??= (await TextToSpeech.GetLocalesAsync()).ToArray();

            string maDayDu = ChuanHoaMaNgonNgu(maNgonNgu);
            string maNgan = maDayDu.Split('-')[0];

            return _boNhoGiongNoi.FirstOrDefault(g =>
                       string.Equals(g.Language, maDayDu, StringComparison.OrdinalIgnoreCase))
                   ?? _boNhoGiongNoi.FirstOrDefault(g =>
                       g.Language.StartsWith(maNgan, StringComparison.OrdinalIgnoreCase));
        }

        private static string ChuanHoaMaNgonNgu(string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return "en-US";

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return "zh-CN";

            return "vi-VN";
        }

        private static string TaoKhoaAmThanh(string vanBan, string maNgonNgu, string? khoaAmThanh)
        {
            if (!string.IsNullOrWhiteSpace(khoaAmThanh))
                return khoaAmThanh.Trim();

            string vanBanDaLamSach = LamSachVanBan(vanBan);
            return $"{ChuanHoaMaNgonNgu(maNgonNgu)}::{vanBanDaLamSach}";
        }

        private static string LamSachVanBan(string vanBan)
        {
            string ketQua = vanBan.Trim();
            ketQua = ketQua.Replace("\r\n", ". ").Replace('\n', ' ');
            ketQua = Regex.Replace(ketQua, @"\s+", " ");
            ketQua = Regex.Replace(ketQua, @"[^\p{L}\p{N}\p{P}\p{Zs}]", string.Empty);
            return ketQua.Trim();
        }

        private static IEnumerable<string> TachDoanVanBan(string vanBan, int gioiHanKyTu = 140)
        {
            if (string.IsNullOrWhiteSpace(vanBan))
                yield break;

            var cau = Regex.Split(vanBan, @"(?<=[\.\!\?。！？])\s+")
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim());

            string doanHienTai = string.Empty;
            foreach (var item in cau)
            {
                if (string.IsNullOrEmpty(doanHienTai))
                {
                    doanHienTai = item;
                    continue;
                }

                if (doanHienTai.Length + item.Length + 1 <= gioiHanKyTu)
                {
                    doanHienTai = $"{doanHienTai} {item}";
                }
                else
                {
                    yield return doanHienTai;
                    doanHienTai = item;
                }
            }

            if (!string.IsNullOrWhiteSpace(doanHienTai))
                yield return doanHienTai;
        }

        private sealed class YeuCauPhatAm
        {
            public YeuCauPhatAm(string khoa, string vanBan, string maNgonNgu, string? tenNoiDungHienThi)
            {
                Khoa = khoa;
                VanBan = vanBan;
                MaNgonNgu = maNgonNgu;
                TenNoiDungHienThi = tenNoiDungHienThi;
                ChoHoanTat = new TaskCompletionSource<TtsPlaybackResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public string Khoa { get; }

            public string VanBan { get; }

            public string MaNgonNgu { get; }

            public string? TenNoiDungHienThi { get; }

            public TaskCompletionSource<TtsPlaybackResult> ChoHoanTat { get; }
        }
    }
}
