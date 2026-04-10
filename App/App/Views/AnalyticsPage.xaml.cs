using App.ViewModels;

namespace App.Views
{
    public partial class AnalyticsPage : ContentPage
    {
        public AnalyticsPage(AnalyticsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ((AnalyticsViewModel)BindingContext).TaiThongKeCommand.ExecuteAsync(null);
        }
    }
}