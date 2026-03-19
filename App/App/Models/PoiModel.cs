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
    }
}