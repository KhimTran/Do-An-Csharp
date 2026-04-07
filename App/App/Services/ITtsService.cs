namespace App.Services
{
    public interface ITtsService
    {
        // Nếu không truyền mã ngôn ngữ, service sẽ tự đọc từ Preferences.
        Task PhatAmAsync(string vanBan, string maNgonNgu = "");
        void DungPhat();
    }
}
