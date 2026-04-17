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
        private readonly ConcurrentQueue<(string VanBan, string MaNgonNgu)> _hangDoi = new();
        private readonly SemaphoreSlim _khoaXuLyHangDoi = new(1, 1);

        public bool DangPhat { get; private set; } = false;

        public async Task PhatAmAsync(string vanBan, string maNgonNgu = "vi-VN")
        {
            if (string.IsNullOrWhiteSpace(vanBan)) return;

            // Nếu nơi gọi không truyền hoặc truyền rỗng thì lấy từ Preferences
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
                        var tatCaGiong = await TextToSpeech.GetLocalesAsync();
                        var giongPhuHop = tatCaGiong.FirstOrDefault(g =>
                            g.Language.StartsWith(
                                item.MaNgonNgu.Split('-')[0],
                                StringComparison.OrdinalIgnoreCase));

                        var tuyChinh = new SpeechOptions
                        {
                            Volume = 1.0f,
                            Pitch = 1.0f,
                            Locale = giongPhuHop
                        };

                        await TextToSpeech.SpeakAsync(item.VanBan, tuyChinh, _cts.Token);
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
    }
}