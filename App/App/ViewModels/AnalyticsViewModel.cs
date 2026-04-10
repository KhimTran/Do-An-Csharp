using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace App.ViewModels
{
    public partial class AnalyticsViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;

        public AnalyticsViewModel(LocalDatabase db)
        {
            _db = db;
        }

        [ObservableProperty]
        private ObservableCollection<ThongKePoiModel> thongKe = new();

        [ObservableProperty]
        private string thongBao = "Chưa có dữ liệu thống kê";

        [RelayCommand]
        public async Task TaiThongKeAsync()
        {
            var ds = await _db.LayThongKePoiAsync();
            ThongKe = new ObservableCollection<ThongKePoiModel>(ds);
            ThongBao = ds.Count == 0
                ? "Chưa có dữ liệu thống kê"
                : $"Có {ds.Count} POI trong thống kê";
        }
    }
}