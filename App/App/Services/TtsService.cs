using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class TtsService : ITtsService
    {
        private CancellationTokenSource? _cts;
        private readonly ConcurrentQueue<(string VanBan, string MaNgonNgu)> _hangDoi = new();
        private readonly SemaphoreSlim _khoaXuLyHangDoi = new(1, 1);
        private Locale[]? _boNhoGiongNoi;

        public bool DangPhat { get; private set; } = false;

        public async Task PhatAmAsync(string vanBan, string maNgonNgu = "vi-VN")
        {
            if (string.IsNullOrWhiteSpace(vanBan)) return;

            if (string.IsNullOrWhiteSpace(maNgonNgu))
                maNgonNgu = Preferences.Get("tts_language", "vi-VN");

            _hangDoi.Enqueue((vanBan, maNgonNgu));

            if (DangPhat) return;

            await XuLyHangDoi();
        }

        private async Task XuLyHangDoi()
        {
            await _khoaXuLyHangDoi.WaitAsync();
            try
            {
                DangPhat = true;
                _cts = new CancellationTokenSource();

                while (_hangDoi.TryDequeue(out var item))
                {
                    try
                    {
                        var giongPhuHop = await TimGiongNoiPhuHopAsync(item.MaNgonNgu);
                        var vanBanDaLamSach = LamSachVanBan(item.VanBan);

                        if (string.IsNullOrWhiteSpace(vanBanDaLamSach))
                            continue;

                        var tuyChinh = new SpeechOptions
                        {
                            // Đưa về mức an toàn để tránh méo tiếng/rè trên loa ngoài.
                            Volume = 0.72f,
                            Pitch = 1.0f,
                            Locale = giongPhuHop
                        };

                        foreach (var doan in TachDoanVanBan(vanBanDaLamSach))
                        {
                            await TextToSpeech.SpeakAsync(doan, tuyChinh, _cts.Token);
                            await Task.Delay(180, _cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TTS] Lỗi: {ex.Message}");
                    }
                }
            }
            finally
            {
                DangPhat = false;
                _khoaXuLyHangDoi.Release();
            }
        }

        public void DungPhat()
        {
            _hangDoi.Clear();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            DangPhat = false;
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
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en-US";
            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
            return "vi-VN";
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
    }
}
