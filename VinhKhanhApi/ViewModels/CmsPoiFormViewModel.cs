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
        public string? TenFileAnhMinhHoa { get; set; }

        public IFormFile? AudioVi { get; set; }
        public IFormFile? AudioEn { get; set; }
        public IFormFile? AudioZh { get; set; }
        public IFormFile? AnhMinhHoa { get; set; }

        // Chủ quán cập nhật thông tin cơ bản.
        public string? SoDienThoai { get; set; }
        public string? GioMoCua { get; set; }
        public string? GioDongCua { get; set; }
        public string? MonDacTrung { get; set; }
        public string? GalleryJson { get; set; }
        public string? QrCodeNoiDung { get; set; }

        // Admin cấu hình giọng đọc mặc định.
        public string? TtsVoiceCode { get; set; } = "vi-VN";

        // Trạng thái kiểm duyệt.
        public string TrangThaiDuyet { get; set; } = "Approved";
        public string? NoiDungDeXuat { get; set; }
        public DateTime? NgayDeXuat { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }
    }
}
