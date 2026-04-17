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
        private readonly SyncService _sync;

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

                ThongBao = LocalizationResourceManager.Instance["PoiSync_Start"];
                bool daDongBo = await _sync.DongBoPoisAsync();

                var ds = await _db.LayTatCaPoiAsync();
                DanhSachPoi = new ObservableCollection<PoiModel>(ds);
                ThongBao = daDongBo
                    ? LocalizationResourceManager.Instance.Translate("PoiSync_Done", DanhSachPoi.Count)
                    : LocalizationResourceManager.Instance.Translate("PoiSync_Offline", DanhSachPoi.Count, _sync.LastError);
            }
            catch (Exception ex)
            {
                ThongBao = LocalizationResourceManager.Instance.Translate("Common_Error", ex.Message);
            }
            finally
            {
                DangTai = false;
            }
        }
    }
}
