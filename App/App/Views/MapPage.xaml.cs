using System.Text.Json;
using App.Models;
using App.Services;
using App.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace App.Views;

public partial class MapPage : ContentPage
{
    private const string BridgeScheme = "appbridge";
    private const string MapTemplateAsset = "map/map-template.html";
    private const string MapAppScriptAsset = "map/map-app.js";
    private const string LeafletCssAsset = "map/leaflet.css";
    private const string LeafletJsAsset = "map/leaflet.js";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly MapViewModel _vm;
    private bool _daTaiHtmlBanDo;
    private bool _mapReady;
    private string? _pendingMapStateJson;

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        _vm.MapStateChanged += OnMapStateChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LocalizationResourceManager.Instance.PropertyChanged += OnLocalizationChanged;

        await DamBaoDaTaiHtmlBanDoAsync();
        await _vm.KhoiDongAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        LocalizationResourceManager.Instance.PropertyChanged -= OnLocalizationChanged;
        _vm.DungGps();
    }

    private void OnLocalizationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = _vm.LamMoiNoiDungHienThiAsync();
    }

    private async Task DamBaoDaTaiHtmlBanDoAsync()
    {
        if (_daTaiHtmlBanDo)
            return;

        var html = await TaoHtmlBanDoAsync();
        _mapReady = false;
        BanDoWebView.Source = new HtmlWebViewSource { Html = html };
        _daTaiHtmlBanDo = true;
    }

    private async Task<string> TaoHtmlBanDoAsync()
    {
        var htmlTemplate = await DocAssetRawAsync(MapTemplateAsset);
        var leafletCss = await DocAssetRawAsync(LeafletCssAsset);
        var leafletJs = await DocAssetRawAsync(LeafletJsAsset);
        var mapAppJs = await DocAssetRawAsync(MapAppScriptAsset);

        return htmlTemplate
            .Replace("/*__LEAFLET_CSS__*/", leafletCss, StringComparison.Ordinal)
            .Replace("//__LEAFLET_JS__", leafletJs, StringComparison.Ordinal)
            .Replace("//__MAP_APP_JS__", mapAppJs, StringComparison.Ordinal);
    }

    private static async Task<string> DocAssetRawAsync(string fileName)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private void OnMapStateChanged(MapRenderState state)
    {
        _pendingMapStateJson = JsonSerializer.Serialize(state, JsonOptions);
        _ = MainThread.InvokeOnMainThreadAsync(FlushPendingMapStateAsync);
    }

    private async Task FlushPendingMapStateAsync()
    {
        if (!_mapReady || string.IsNullOrWhiteSpace(_pendingMapStateJson))
            return;

        var pendingJson = _pendingMapStateJson;
        _pendingMapStateJson = null;

        try
        {
            var serializedLiteral = JsonSerializer.Serialize(pendingJson);
            var script = $"window.mapBridge && window.mapBridge.setMapState(JSON.parse({serializedLiteral}));";
            await BanDoWebView.EvaluateJavaScriptAsync(script);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage] Loi cap nhat WebView map: {ex.Message}");
            _pendingMapStateJson = pendingJson;
        }
    }

    private async void DongPopup_Clicked(object? sender, EventArgs e)
    {
        await _vm.DongPopupAsync();
    }

    private async void TrackingPopup_Clicked(object? sender, EventArgs e)
    {
        await _vm.BatDauTrackingPoiDangMoAsync();
    }

    private void BanDoWebView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!TryHandleBridgeUrl(e.Url))
            return;

        e.Cancel = true;
    }

    private bool TryHandleBridgeUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Scheme.Equals(BridgeScheme, StringComparison.OrdinalIgnoreCase))
            return false;

        switch (uri.Host.ToLowerInvariant())
        {
            case "ready":
                _mapReady = true;
                _ = MainThread.InvokeOnMainThreadAsync(FlushPendingMapStateAsync);
                return true;

            case "poi-click":
                if (TryGetQueryValue(uri, "poiId", out var poiIdText) &&
                    int.TryParse(poiIdText, out var poiId))
                {
                    _ = MainThread.InvokeOnMainThreadAsync(() => _vm.ChonPoiTuMapAsync(poiId));
                }

                return true;

            case "poi-popup-close":
                if (TryGetQueryValue(uri, "poiId", out var closedPoiIdText) &&
                    int.TryParse(closedPoiIdText, out var closedPoiId))
                {
                    _ = MainThread.InvokeOnMainThreadAsync(() => _vm.DongPopupNeuTrungAsync(closedPoiId));
                }

                return true;

            default:
                return true;
        }
    }

    private static bool TryGetQueryValue(Uri uri, string key, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(uri.Query))
            return false;

        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;

            var queryKey = Uri.UnescapeDataString(parts[0]);
            if (!queryKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            return true;
        }

        return false;
    }
}
