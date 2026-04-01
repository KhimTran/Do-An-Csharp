using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using App.Models;
using App.Services;

namespace App.ViewModels
{
    public partial class PoiListViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly SyncService _sync; // 1. Khai báo thêm SyncService

        // 2. Tiêm SyncService vào constructor
        public PoiListViewModel(LocalDatabase db, SyncService sync)
        {
            _db = db;
            _sync = sync;
        }

        [ObservableProperty] private ObservableCollection<PoiModel> danhSachPoi = new();
        [ObservableProperty] private bool dangTai = false;
        [ObservableProperty] private string thongBao = string.Empty;

        [RelayCommand]
        public async Task TaiDanhSachPoi()
        {
            try
            {
                DangTai = true;

                // 3. Gọi đồng bộ dữ liệu từ server về SQLite trước
                ThongBao = "Đang đồng bộ dữ liệu từ Server...";
                await _sync.DongBoPoisAsync();

                // 4. Sau đó mới load dữ liệu từ SQLite lên giao diện
                var ds = await _db.LayTatCaPoiAsync();
                DanhSachPoi = new ObservableCollection<PoiModel>(ds);
                ThongBao = $"Đã tải {DanhSachPoi.Count} điểm thuyết minh";
            }
            catch (Exception ex)
            {
                ThongBao = $"Lỗi: {ex.Message}";
            }
            finally
            {
                DangTai = false;
            }
        }
    }
}