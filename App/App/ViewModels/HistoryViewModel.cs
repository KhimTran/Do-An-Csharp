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

        public HistoryViewModel(LocalDatabase db)
        {
            _db = db;
        }

        [ObservableProperty]
        private ObservableCollection<LichSuPhatModel> lichSu = new();

        [ObservableProperty]
        private string thongBao = "";

        [RelayCommand]
        public async Task TaiLichSuAsync()
        {
            var ds = await _db.LayLichSuPhatAsync();
            LichSu = new ObservableCollection<LichSuPhatModel>(ds);
            ThongBao = $"Có {ds.Count} lượt nghe";
        }
    }
}