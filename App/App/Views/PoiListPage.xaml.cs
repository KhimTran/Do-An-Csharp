using App.ViewModels;

namespace App.Views
{
    public partial class PoiListPage : ContentPage
    {
        public PoiListPage(PoiListViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}