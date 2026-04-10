using App.ViewModels;

namespace App.Views
{
    public partial class HistoryPage : ContentPage
    {
        public HistoryPage(HistoryViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ((HistoryViewModel)BindingContext).TaiLichSuCommand.ExecuteAsync(null);
        }
    }
}