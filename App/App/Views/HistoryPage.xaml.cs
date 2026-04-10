using App.ViewModels;

namespace App.Views
{
    public partial class HistoryPage : ContentPage
    {
        private readonly HistoryViewModel _vm;

        public HistoryPage(HistoryViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.TaiDuLieuCommand.ExecuteAsync(null);
        }
    }
}
