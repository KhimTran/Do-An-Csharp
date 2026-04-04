using App.ViewModels;
using ZXing.Net.Maui;

namespace App.Views
{
    public partial class QrScanPage : ContentPage
    {
        private readonly QrScanViewModel _vm;

        public QrScanPage(QrScanViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = vm;
        }

        private async void BarcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            var ketQua = e.Results.FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(ketQua)) return;

            // Chạy trên UI thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _vm.XuLyQrCommand.ExecuteAsync(ketQua);
            });
        }
    }
}