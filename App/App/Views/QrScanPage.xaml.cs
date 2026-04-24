using App.Services;
using App.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace App.Views
{
    public partial class QrScanPage : ContentPage
    {
        private readonly QrScanViewModel _vm;
        private CameraBarcodeReaderView? _qrCamera;

        public QrScanPage(QrScanViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = vm;
            _vm.AlertRequested += Vm_AlertRequested;
#if !DEBUG
            CameraDebugHintLabel.IsVisible = false;
#endif
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnsureCameraReady();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            DisposeCamera();
        }

        private void EnsureCameraReady()
        {
            if (_qrCamera != null || DeviceRuntimeProfile.IsVirtualDevice)
            {
                CameraFallbackOverlay.IsVisible = DeviceRuntimeProfile.IsVirtualDevice;
                return;
            }

            var qrCamera = new CameraBarcodeReaderView();
            qrCamera.SetBinding(CameraBarcodeReaderView.IsDetectingProperty, nameof(QrScanViewModel.DangQuet));
            qrCamera.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            };
            qrCamera.BarcodesDetected += BarcodeReader_BarcodesDetected;

            CameraHost.Children.Clear();
            CameraHost.Children.Add(qrCamera);
            CameraFallbackOverlay.IsVisible = false;
            _qrCamera = qrCamera;
        }

        private void DisposeCamera()
        {
            if (_qrCamera == null)
                return;

            _qrCamera.BarcodesDetected -= BarcodeReader_BarcodesDetected;
            _qrCamera.IsDetecting = false;
            CameraHost.Children.Clear();
            _qrCamera.Handler?.DisconnectHandler();
            _qrCamera = null;
        }

        private async void BarcodeReader_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            var ketQua = e.Results.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(ketQua))
                return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _vm.XuLyQrCommand.ExecuteAsync(ketQua);
            });
        }

        private async void Vm_AlertRequested(object? sender, QrAlertRequestedEventArgs e)
        {
            await DisplayAlertAsync(e.Title, e.Message, "OK");
        }
    }
}
