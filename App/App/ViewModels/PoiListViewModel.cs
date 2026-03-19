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

        public PoiListViewModel(LocalDatabase db) => _db = db;

        [ObservableProperty]
        private ObservableCollection<PoiModel> danhSachPoi = new();

        [ObservableProperty]
        private bool dangTai = false;

        [ObservableProperty]
        private string thongBao = string.Empty;

        [RelayCommand]
        public async Task TaiDanhSachPoi()
        {
            try
            {
                DangTai = true;
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