using SQLite;

namespace App.Models
{
    [Table("AppSettings")]
    public class AppSettingModel
    {
        [PrimaryKey]
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
