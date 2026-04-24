using App.ViewModels;

namespace App.Views
{
    public partial class PoiListPage : ContentPage
    {
        private readonly PoiListViewModel _vm;

        public PoiListPage(PoiListViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.KhoiDongAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.DungGps();
        }
    }
}
