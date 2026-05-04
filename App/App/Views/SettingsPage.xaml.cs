using App.Services;
using Microsoft.Maui.Storage;

namespace App.Views;

public partial class SettingsPage : ContentPage
{
    private readonly LocalDatabase _db;
    private readonly IOfflineAudioCacheService _offlineAudioCache;
    private bool _isAudioCacheBusy;

    public SettingsPage(LocalDatabase db, IOfflineAudioCacheService offlineAudioCache)
    {
        InitializeComponent();
        _db = db;
        _offlineAudioCache = offlineAudioCache;
        NgonNguPicker.Items.Add("VN - Tieng Viet");
        NgonNguPicker.Items.Add("EN - English");
        NgonNguPicker.Items.Add("ZH - \u4e2d\u6587");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TaiCaiDatAsync();
        await CapNhatTrangThaiAudioOfflineAsync();
    }

    private async Task TaiCaiDatAsync()
    {
        string ngonNgu = await _db.LayCaiDatAsync("app_language")
                         ?? Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));

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
        BanKinhLabel.Text = $"{banKinh}m";
        OfflineSwitch.IsToggled = offlineMode;
    }

    private async Task CapNhatTrangThaiAudioOfflineAsync()
    {
        try
        {
            var cacheSize = await _offlineAudioCache.GetCacheSizeBytesAsync();
            AudioOfflineStatusLabel.Text = cacheSize > 0
                ? T("SettingsPage_AudioOfflineStatusCachedFormat", DinhDangDungLuong(cacheSize))
                : T("SettingsPage_AudioOfflineStatusNotDownloaded");
        }
        catch
        {
            AudioOfflineStatusLabel.Text = T("SettingsPage_AudioOfflineStatusNotDownloaded");
        }
    }

    private void BanKinhSlider_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        BanKinhLabel.Text = $"{(int)e.NewValue}m";
    }

    private async void DownloadAudioOfflineButton_Clicked(object? sender, EventArgs e)
    {
        if (_isAudioCacheBusy)
            return;

        try
        {
            DatTrangThaiNutAudioCache(false);
            AudioOfflineStatusLabel.Text = T("SettingsPage_AudioOfflineDownloading");

            var progress = new Progress<string>(message =>
            {
                AudioOfflineStatusLabel.Text = message;
            });

            var result = await _offlineAudioCache.DownloadAllPoiAudioAsync(progress);
            var summary = T(
                "SettingsPage_AudioOfflineDownloadSummaryFormat",
                result.Downloaded,
                result.TotalFiles,
                result.Skipped,
                result.Failed);
            var cacheSize = DinhDangDungLuong(result.CacheSizeBytes);

            AudioOfflineStatusLabel.Text = T(
                "SettingsPage_AudioOfflineDownloadSummaryWithSizeFormat",
                result.Downloaded,
                result.TotalFiles,
                result.Skipped,
                result.Failed,
                cacheSize);

            await DisplayAlertAsync(
                T("SettingsPage_AudioOfflineTitle"),
                $"{summary}\n{T("SettingsPage_AudioOfflineCacheSizeFormat", cacheSize)}",
                "OK");
        }
        catch (Exception ex)
        {
            AudioOfflineStatusLabel.Text = T("SettingsPage_AudioOfflineDownloadError");
            await DisplayAlertAsync(T("SettingsPage_AudioOfflineTitle"), ex.Message, "OK");
        }
        finally
        {
            DatTrangThaiNutAudioCache(true);
        }
    }

    private async void ClearAudioOfflineButton_Clicked(object? sender, EventArgs e)
    {
        if (_isAudioCacheBusy)
            return;

        try
        {
            DatTrangThaiNutAudioCache(false);
            AudioOfflineStatusLabel.Text = T("SettingsPage_AudioOfflineClearing");

            await _offlineAudioCache.ClearCacheAsync();
            AudioOfflineStatusLabel.Text = T("SettingsPage_AudioOfflineCleared");

            await DisplayAlertAsync(
                T("SettingsPage_AudioOfflineTitle"),
                T("SettingsPage_AudioOfflineClearedMessage"),
                "OK");
        }
        catch (Exception ex)
        {
            AudioOfflineStatusLabel.Text = T("SettingsPage_AudioOfflineClearError");
            await DisplayAlertAsync(T("SettingsPage_AudioOfflineTitle"), ex.Message, "OK");
        }
        finally
        {
            DatTrangThaiNutAudioCache(true);
        }
    }

    private void DatTrangThaiNutAudioCache(bool enabled)
    {
        _isAudioCacheBusy = !enabled;
        DownloadAudioOfflineButton.IsEnabled = enabled;
        ClearAudioOfflineButton.IsEnabled = enabled;
    }

    private async void LuuButton_Clicked(object? sender, EventArgs e)
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
        Preferences.Set("app_language", maNgonNgu);
        Preferences.Set("geofence_radius", banKinh);
        Preferences.Set("offline_mode", offlineMode);

        await _db.LuuCaiDatAsync("tts_language", maNgonNgu);
        await _db.LuuCaiDatAsync("app_language", maNgonNgu);
        await _db.LuuCaiDatAsync("geofence_radius", banKinh.ToString());
        await _db.LuuCaiDatAsync("offline_mode", offlineMode.ToString());

        LocalizationResourceManager.Instance.SetLanguage(maNgonNgu);
        await CapNhatTrangThaiAudioOfflineAsync();

        await DisplayAlertAsync(
            LocalizationResourceManager.Instance["SettingsPage_SaveSuccessTitle"],
            LocalizationResourceManager.Instance["SettingsPage_SaveSuccessMessage"],
            "OK");
    }

    private async void MacDinhButton_Clicked(object? sender, EventArgs e)
    {
        NgonNguPicker.SelectedIndex = 0;
        BanKinhSlider.Value = 50;
        OfflineSwitch.IsToggled = false;

        Preferences.Set("tts_language", "vi-VN");
        Preferences.Set("app_language", "vi-VN");
        Preferences.Set("geofence_radius", 50);
        Preferences.Set("offline_mode", false);

        await _db.LuuCaiDatAsync("tts_language", "vi-VN");
        await _db.LuuCaiDatAsync("app_language", "vi-VN");
        await _db.LuuCaiDatAsync("geofence_radius", "50");
        await _db.LuuCaiDatAsync("offline_mode", "False");

        LocalizationResourceManager.Instance.SetLanguage("vi-VN");
        await CapNhatTrangThaiAudioOfflineAsync();

        await DisplayAlertAsync(
            LocalizationResourceManager.Instance["SettingsPage_ResetSuccessTitle"],
            LocalizationResourceManager.Instance["SettingsPage_ResetSuccessMessage"],
            "OK");
    }

    private static string DinhDangDungLuong(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        var kb = bytes / 1024d;
        if (kb < 1024)
            return $"{kb:0.#} KB";

        var mb = kb / 1024d;
        return $"{mb:0.#} MB";
    }

    private static string T(string key, params object[] args)
        => LocalizationResourceManager.Instance.Translate(key, args);
}
