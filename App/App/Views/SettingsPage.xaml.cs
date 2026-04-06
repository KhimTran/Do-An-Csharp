using Microsoft.Maui.Storage;

namespace App.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        TaiCaiDat();
    }

    private void TaiCaiDat()
    {
        string ngonNgu = Preferences.Get("tts_language", "vi-VN");
        int banKinh = Preferences.Get("geofence_radius", 50);
        bool offlineMode = Preferences.Get("offline_mode", false);

        NgonNguPicker.SelectedIndex = ngonNgu switch
        {
            "vi-VN" => 0,
            "en-US" => 1,
            "zh-CN" => 2,
            _ => 0
        };

        BanKinhSlider.Value = banKinh;
        BanKinhLabel.Text = $"{banKinh} m";
        OfflineSwitch.IsToggled = offlineMode;
    }

    private void BanKinhSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        BanKinhLabel.Text = $"{(int)e.NewValue} m";
    }

    private async void LuuButton_Clicked(object sender, EventArgs e)
    {
        string maNgonNgu = NgonNguPicker.SelectedIndex switch
        {
            0 => "vi-VN",
            1 => "en-US",
            2 => "zh-CN",
            _ => "vi-VN"
        };

        int banKinh = (int)BanKinhSlider.Value;
        bool offlineMode = OfflineSwitch.IsToggled;

        Preferences.Set("tts_language", maNgonNgu);
        Preferences.Set("geofence_radius", banKinh);
        Preferences.Set("offline_mode", offlineMode);
        Preferences.Set("force_reread_once", true);

        await DisplayAlert("Thành công", "Đã lưu cài đặt.", "OK");
    }

    private void MacDinhButton_Clicked(object sender, EventArgs e)
    {
        NgonNguPicker.SelectedIndex = 0;
        BanKinhSlider.Value = 50;
        OfflineSwitch.IsToggled = false;
    }
}