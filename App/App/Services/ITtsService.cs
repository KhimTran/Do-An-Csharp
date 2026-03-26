// Services/ITtsService.cs
namespace App.Services
{
    public interface ITtsService
    {
        // Phát văn bản với ngôn ngữ chỉ định
        Task PhatAmAsync(string vanBan, string maNgonNgu = "vi-VN");

        // Dừng phát âm đang chạy
        void DungPhat();

        // Kiểm tra có đang phát không
        bool DangPhat { get; }
    }
}