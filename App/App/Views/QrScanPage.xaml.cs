using App.ViewModels;

namespace App.Views
{
    public partial class QrScanPage : ContentPage
    {
        public QrScanPage(QrScanViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}