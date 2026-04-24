namespace App.Services
{
    public interface ITtsService
    {
        event EventHandler<TtsPlaybackStateChangedEventArgs>? PlaybackStateChanged;

        Task<TtsPlaybackResult> PhatAmAsync(
            string vanBan,
            string maNgonNgu = "vi-VN",
            string? khoaAmThanh = null,
            string? tenNoiDungHienThi = null);

        void DungPhat();

        bool DangPhat { get; }
    }
}
