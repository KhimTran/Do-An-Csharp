namespace VinhKhanhApi.ViewModels
{
    public class CmsQrCodesViewModel
    {
        public int TongPoi { get; set; }
        public int DaCoQr { get; set; }
        public int ChuaCoQr { get; set; }
        public List<CmsQrCodeItemViewModel> Items { get; set; } = new();
    }

    public class CmsQrCodeItemViewModel
    {
        public int Id { get; set; }
        public string TenPoi { get; set; } = string.Empty;
        public string ViTriHienThi { get; set; } = string.Empty;
        public string QrContent { get; set; } = string.Empty;
        public bool DaCoQr { get; set; }
        public string QrImageBase64 { get; set; } = string.Empty;
    }
}
