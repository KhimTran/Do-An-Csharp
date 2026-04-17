namespace VinhKhanhApi.Models
{
    public class PoiModel
    {
        public int Id { get; set; }
        public string Ten { get; set; } = string.Empty;
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

        // --- Dữ liệu chủ quán cập nhật (local user scope) ---
        public string? SoDienThoai { get; set; }
        public string? GioMoCua { get; set; }
        public string? GioDongCua { get; set; }
        public string? MonDacTrung { get; set; }
        public string? GalleryJson { get; set; }
        public string? QrCodeNoiDung { get; set; }

        // --- Dữ liệu cấu hình quản trị (admin scope) ---
        public string? TtsVoiceCode { get; set; } = "vi-VN";
        public string? NguoiCapNhat { get; set; }

        // --- Luồng duyệt nội dung chủ quán submit ---
        public string? NoiDungDeXuat { get; set; }
        public string TrangThaiDuyet { get; set; } = "Approved"; // Pending | Approved | Rejected
        public DateTime? NgayDeXuat { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }
    }
}
