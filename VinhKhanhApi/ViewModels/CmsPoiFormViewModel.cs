using System.ComponentModel.DataAnnotations;

namespace VinhKhanhApi.ViewModels
{
    public class CmsPoiFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Ten { get; set; } = string.Empty;

        [Required]
        public string MoTa_Vi { get; set; } = string.Empty;
        public string MoTa_En { get; set; } = string.Empty;
        public string MoTa_Zh { get; set; } = string.Empty;

        public double Lat { get; set; }
        public double Lng { get; set; }
        public double BanKinh { get; set; } = 50;
        public int UuTien { get; set; } = 5;

        public string? TenFileAudio_Vi { get; set; }
        public string? TenFileAudio_En { get; set; }
        public string? TenFileAudio_Zh { get; set; }

        public IFormFile? AudioVi { get; set; }
        public IFormFile? AudioEn { get; set; }
        public IFormFile? AudioZh { get; set; }
    }
}
