using SQLite;

namespace App.Models
{
    [Table("POIs")]
    public class PoiModel
    {
        [PrimaryKey, AutoIncrement]
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
        public string? TenFileAnhMinhHoa { get; set; }

        // Đồng bộ từ CMS/Admin để app hiển thị thông tin chi tiết đầy đủ.
        public string? SoDienThoai { get; set; }
        public string? GioMoCua { get; set; }
        public string? GioDongCua { get; set; }
        public string? MonDacTrung { get; set; }
        public string? GalleryJson { get; set; }
        public string? QrCodeNoiDung { get; set; }
        public string? TtsVoiceCode { get; set; }

        // App chỉ nhận dữ liệu đã duyệt; vẫn lưu trạng thái để debug/sync.
        public string TrangThaiDuyet { get; set; } = "Approved";
        public DateTime? NgayDuyet { get; set; }
    }
}
