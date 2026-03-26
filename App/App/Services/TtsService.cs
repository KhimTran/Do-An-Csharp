// Services/TtsService.cs
namespace App.Services
{
    public class TtsService : ITtsService
    {
        private CancellationTokenSource? _cts;
        public bool DangPhat { get; private set; } = false;

        public async Task PhatAmAsync(string vanBan, string maNgonNgu = "vi-VN")
        {
            if (string.IsNullOrWhiteSpace(vanBan)) return;

            // Hủy phát âm cũ nếu đang chạy
            DungPhat();

            try
            {
                DangPhat = true;
                _cts = new CancellationTokenSource();

                var tatCaGiong = await TextToSpeech.GetLocalesAsync();
                var giongPhuHop = tatCaGiong.FirstOrDefault(g =>
                    g.Language.StartsWith(maNgonNgu.Split('-')[0],
                    StringComparison.OrdinalIgnoreCase));

                var tuyChinh = new SpeechOptions
                {
                    Volume = 1.0f,
                    Pitch = 1.0f,
                    Locale = giongPhuHop
                };

                await TextToSpeech.SpeakAsync(vanBan, tuyChinh, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Bị hủy chủ động → bình thường
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Lỗi: {ex.Message}");
            }
            finally
            {
                DangPhat = false;
            }
        }

        public void DungPhat()
        {
            _cts?.Cancel();       // ← hủy qua token
            _cts?.Dispose();
            _cts = null;
            DangPhat = false;
        }
    }
}