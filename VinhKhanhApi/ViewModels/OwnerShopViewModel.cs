using Microsoft.AspNetCore.Http;

namespace VinhKhanhApi.ViewModels
{
    public class OwnerShopViewModel
    {
        public int PoiId { get; set; }
        public string Ten { get; set; } = string.Empty;

        public string MoTa_Vi { get; set; } = string.Empty;
        public string MoTa_En { get; set; } = string.Empty;
        public string MoTa_Zh { get; set; } = string.Empty;

        public string? TenFileAudio_Vi { get; set; }
        public string? TenFileAudio_En { get; set; }
        public string? TenFileAudio_Zh { get; set; }
        public string? TenFileAnhMinhHoa { get; set; }

        public IFormFile? AudioVi { get; set; }
        public IFormFile? AudioEn { get; set; }
        public IFormFile? AudioZh { get; set; }
        public IFormFile? AnhMinhHoa { get; set; }
    }
}
