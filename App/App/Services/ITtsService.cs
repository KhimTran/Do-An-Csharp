namespace App.Services
{
    public interface ITtsService
    {
        Task<TtsPlaybackResult> PhatAmAsync(
            string vanBan,
            string maNgonNgu = "vi-VN",
            string? khoaAmThanh = null);

        void DungPhat();

        bool DangPhat { get; }
    }
}
