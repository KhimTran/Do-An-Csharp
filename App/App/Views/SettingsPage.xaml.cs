using App.Services;
using Microsoft.Maui.Storage;

namespace App.Views;

public partial class SettingsPage : ContentPage
{
    private readonly LocalDatabase _db;

    public SettingsPage(LocalDatabase db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TaiCaiDatAsync();
    }

    private async Task TaiCaiDatAsync()
    {
        string ngonNgu = await _db.LayCaiDatAsync("tts_language")
                         ?? Preferences.Get("tts_language", "vi-VN");

        int banKinh = int.TryParse(await _db.LayCaiDatAsync("geofence_radius"), out var bk)
            ? bk
            : Preferences.Get("geofence_radius", 50);

        bool offlineMode = bool.TryParse(await _db.LayCaiDatAsync("offline_mode"), out var off)
            ? off
            : Preferences.Get("offline_mode", false);


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

        // Lưu cả Preferences (dùng nhanh tại runtime) + SQLite (đáp ứng yêu cầu tuần 5)
        Preferences.Set("tts_language", maNgonNgu);
        Preferences.Set("geofence_radius", banKinh);
        Preferences.Set("offline_mode", offlineMode);
        Preferences.Set("force_reread_once", true);

        await _db.LuuCaiDatAsync("tts_language", maNgonNgu);
        await _db.LuuCaiDatAsync("geofence_radius", banKinh.ToString());
        await _db.LuuCaiDatAsync("offline_mode", offlineMode.ToString());

        await DisplayAlert("Thành công", "Đã lưu cài đặt.", "OK");
    }

    private async void MacDinhButton_Clicked(object sender, EventArgs e)
    {
        NgonNguPicker.SelectedIndex = 0;
        BanKinhSlider.Value = 50;
        OfflineSwitch.IsToggled = false;

        Preferences.Set("tts_language", "vi-VN");
        Preferences.Set("geofence_radius", 50);
        Preferences.Set("offline_mode", false);
        Preferences.Set("force_reread_once", true);

        await _db.LuuCaiDatAsync("tts_language", "vi-VN");
        await _db.LuuCaiDatAsync("geofence_radius", "50");
        await _db.LuuCaiDatAsync("offline_mode", "False");

        await DisplayAlert("Đã khôi phục", "Đã trả về cài đặt mặc định.", "OK");
    }
}
