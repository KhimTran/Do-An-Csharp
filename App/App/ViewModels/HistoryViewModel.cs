using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace App.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;

        public ObservableCollection<LichSuPhatModel> LichSuGanDay { get; } = new();
        public ObservableCollection<ThongKePoiModel> TopPoi { get; } = new();

        [ObservableProperty] private bool dangTai;

        public HistoryViewModel(LocalDatabase db)
        {
            _db = db;
        }

        [RelayCommand]
        public async Task TaiDuLieuAsync()
        {
            if (DangTai) return;

            try
            {
                DangTai = true;

                var lichSu = await _db.LayLichSuMoiNhatAsync();
                var topPoi = await _db.LayTopPoiDuocNgheNhieuAsync();

                LichSuGanDay.Clear();
                foreach (var item in lichSu)
                    LichSuGanDay.Add(item);

                TopPoi.Clear();
                foreach (var item in topPoi)
                    TopPoi.Add(item);
            }
            finally
            {
                DangTai = false;
            }
        }
    }
}
