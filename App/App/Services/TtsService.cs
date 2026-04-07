using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class TtsService : ITtsService
    {
        private CancellationTokenSource? _cts;

        // Hàng đợi (Queue) chống phát trùng, đọc lần lượt các câu thuyết minh
        private readonly ConcurrentQueue<string> _hangDoi = new();

        public bool DangPhat { get; private set; } = false;

        public async Task PhatAmAsync(string vanBan, string maNgonNgu = "")
        {
            if (string.IsNullOrWhiteSpace(vanBan)) return;

            // Nếu caller không truyền, tự đọc từ Preferences để Settings có tác dụng toàn app
            string ngonNguDaChon = LayNgonNguTuPreferences(maNgonNgu);

            // Cho đoạn văn bản vào hàng đợi thay vì phát ngay lập tức
            _hangDoi.Enqueue(vanBan);

            // Nếu hệ thống đang bận phát âm rồi thì thôi, vòng lặp tự động sẽ lấy ra đọc sau
            if (DangPhat) return;

            // Nếu đang rảnh thì kích hoạt tiến trình xử lý hàng đợi
            await XuLyHangDoi(ngonNguDaChon);
        }

        private static string LayNgonNguTuPreferences(string maNgonNgu)
        {
            if (!string.IsNullOrWhiteSpace(maNgonNgu))
                return maNgonNgu;

            // Ưu tiên key mới "ngon_ngu", fallback key cũ để tương thích ngược
            var ngonNguMoi = Preferences.Get("ngon_ngu", string.Empty);
            if (!string.IsNullOrWhiteSpace(ngonNguMoi))
                return ngonNguMoi;

            return Preferences.Get("tts_language", "vi-VN");
        }

        private async Task XuLyHangDoi(string maNgonNgu)
        {
            DangPhat = true;

            var tatCaGiong = await TextToSpeech.GetLocalesAsync();
            var giongPhuHop = tatCaGiong.FirstOrDefault(g =>
                g.Language.StartsWith(maNgonNgu.Split('-')[0], StringComparison.OrdinalIgnoreCase));

            _cts = new CancellationTokenSource();

            // Vòng lặp lấy từng câu trong hàng đợi ra đọc (FIFO - Vào trước ra trước)
            while (_hangDoi.TryDequeue(out string? textCanDoc))
            {
                if (string.IsNullOrEmpty(textCanDoc)) continue;

                try
                {
                    var tuyChinh = new SpeechOptions
                    {
                        Volume = 1.0f,
                        Pitch = 1.0f,
                        Locale = giongPhuHop
                    };

                    await TextToSpeech.SpeakAsync(textCanDoc, tuyChinh, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Bị hủy chủ động -> Thoát vòng lặp
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TTS] Lỗi: {ex.Message}");
                }
            }

            // Đọc xong toàn bộ hàng đợi
            DangPhat = false;
        }

        public void DungPhat()
        {
            // Xóa trắng hàng đợi nếu muốn dừng hẳn
            _hangDoi.Clear();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            DangPhat = false;
        }
    }
}
