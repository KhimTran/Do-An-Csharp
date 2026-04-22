namespace App.Services;

public interface ILocationService
{
    Task BatDauTheoDoiAsync(
        Action<LocationSnapshot> khiCoViTri,
        Action<LocationTrackingStatus>? khiTrangThaiThayDoi = null);

    void DungTheoDoi();

    Task<LocationSnapshot?> LayViTriHienTaiAsync();
}
