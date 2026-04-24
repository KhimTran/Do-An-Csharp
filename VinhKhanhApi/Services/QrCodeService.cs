using QRCoder;

namespace VinhKhanhApi.Services
{
    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrPngBase64(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var pngBytes = new PngByteQRCode(qrData).GetGraphic(20);
            return Convert.ToBase64String(pngBytes);
        }
    }
}
