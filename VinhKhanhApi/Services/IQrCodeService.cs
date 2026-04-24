namespace VinhKhanhApi.Services
{
    public interface IQrCodeService
    {
        string GenerateQrPngBase64(string content);
    }
}
