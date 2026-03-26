using App.Services;
using App.ViewModels;

namespace App.Views
{
    public partial class PoiListPage : ContentPage
    {
        private readonly ITtsService _tts;

        public PoiListPage(PoiListViewModel vm, ITtsService tts)
        {
            InitializeComponent();
            BindingContext = vm;
            _tts = tts;

            // ===== TEST TẠM THỜI =====
            TestTts();
        }

        private async void TestTts()
        {
            // Test tiếng Việt
            await _tts.PhatAmAsync(
                "Chào mừng bạn đến với phố ẩm thực Vĩnh Khánh.",
                "vi-VN"
            );

            await Task.Delay(3000); // chờ xong mới phát tiếp

            // Test tiếng Anh
            await _tts.PhatAmAsync(
                "Welcome to Vinh Khanh food street.",
                "en-US"
            );
        }
        // ===== HẾT TEST =====
    }
}