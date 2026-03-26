// Services/ILocationService.cs
namespace App.Services
{
    public interface ILocationService
    {
        // Bắt đầu theo dõi vị trí, gọi callback mỗi khi có vị trí mới
        Task BatDauTheoDoiAsync(Action<double, double> khiCoViTri);

        // Dừng theo dõi
        void DungTheoDoi();

        // Lấy vị trí một lần
        Task<(double Lat, double Lng)?> LayViTriHienTaiAsync();
    }
}