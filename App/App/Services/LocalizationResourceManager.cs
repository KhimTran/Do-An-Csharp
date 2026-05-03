using System.ComponentModel;
using System.Globalization;

namespace App.Services;

public sealed class LocalizationResourceManager : INotifyPropertyChanged
{
    public static LocalizationResourceManager Instance { get; } = new();

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["vi"] = new()
        {
            ["Shell_List"] = "Địa điểm",
            ["Shell_Map"] = "Bản đồ",
            ["Shell_QR"] = "QR",
            ["Shell_History"] = "Lịch sử",
            ["Shell_Settings"] = "Cài đặt",

            ["PoiPage_Title"] = "Điểm thuyết minh",
            ["PoiPage_RadiusFormat"] = "Bán kính: {0}m",
            ["PoiPage_LoadButton"] = "Tải danh sách POI",

            ["MapPage_Title"] = "Bản đồ Vĩnh Khánh",
            ["MapPage_LegendTitle"] = "Chú thích bản đồ",
            ["MapPage_LegendUser"] = "Chấm xanh: vị trí của bạn",
            ["MapPage_LegendPoi"] = "Chấm đỏ: điểm ẩm thực",
            ["MapPage_LegendNearest"] = "Chấm vàng: điểm gần nhất",
            ["MapPage_LegendRoute"] = "Đường xanh: tuyến gợi ý",
            ["MapPage_NearestRoute"] = "Gợi ý tham quan gần bạn",
            ["MapPage_NearbyPlaceLabel"] = "Điểm gần bạn nhất",
            ["MapPage_TrackingPlaceLabel"] = "Đang theo dõi",
            ["MapPage_SuggestedRouteLabel"] = "Tuyến tham quan gợi ý",
            ["MapPage_DistanceFormat"] = "Cách bạn {0:F0} m",
            ["MapPage_NoNearest"] = "Chưa tìm thấy điểm tham quan gần bạn",
            ["MapPage_Track"] = "Theo dõi",
            ["MapPage_WaitingGps"] = "Đang xác định vị trí của bạn...",
            ["MapPage_GpsTracking"] = "Đang xác định vị trí của bạn...",
            ["MapPage_GpsSimulated"] = "Đang dùng vị trí thử nghiệm để gợi ý điểm gần bạn.",
            ["MapPage_GpsPermissionDenied"] = "Bạn có thể bật quyền vị trí để xem gợi ý chính xác hơn.",
            ["MapPage_GpsDisabled"] = "Hãy bật định vị để xem các điểm tham quan gần bạn.",
            ["MapPage_GpsUnavailable"] = "Chưa xác định được vị trí của bạn.",
            ["MapPage_GpsError"] = "Chưa xác định được vị trí của bạn: {0}",
            ["MapPage_PlaybackIdle"] = "",
            ["MapPage_PlaybackEmpty"] = "Điểm này chưa có nội dung thuyết minh.",
            ["MapPage_PlaybackCooldown"] = "Bạn vừa nghe thuyết minh cho điểm này. Vui lòng thử lại sau ít phút.",
            ["MapPage_PlaybackStarted"] = "Đang phát thuyết minh: {0}",
            ["MapPage_PlaybackCompleted"] = "Bạn vừa nghe thuyết minh: {0}",
            ["MapPage_PlaybackFailed"] = "Chưa phát được thuyết minh cho {0}.",

            ["QrPage_Title"] = "Quét mã QR",
            ["QrPage_Hint"] = "Đưa mã QR chứa POI ID vào khung hình",
            ["QrPage_TestButton"] = "Test nhanh: POI 1",
            ["QrPage_AimCamera"] = "Hướng camera vào mã QR",
            ["QrPage_CameraUnavailable"] = "Camera scan đang tắt trên emulator để tránh crash. Bạn vẫn có thể nhập nội dung QR bên dưới.",
            ["QrPage_ManualTitle"] = "Nhập nội dung QR để demo",
            ["QrPage_ManualPlaceholder"] = "Ví dụ: 1, poi:1 hoặc URL server",
            ["QrPage_ManualSubmit"] = "Xử lý QR đã nhập",
            ["QrPage_Invalid"] = "Mã QR không hợp lệ: {0}",
            ["QrPage_PoiNotFound"] = "Không tìm thấy điểm số {0}",
            ["QrPage_Playing"] = "Đang phát: {0}",
            ["QrPage_Done"] = "Xong. Quét mã khác?",

            ["HistoryPage_Title"] = "Lịch sử nghe",
            ["HistoryPage_Header"] = "Hành trình của bạn",
            ["HistoryPage_Subtitle"] = "Những quán ăn bạn đã khám phá tại Phố ẩm thực Vĩnh Khánh",
            ["HistoryPage_Top"] = "Top POI được nghe nhiều",
            ["HistoryPage_NoStats"] = "Chưa có dữ liệu thống kê.",
            ["HistoryPage_Recent"] = "Lịch sử gần đây",
            ["HistoryPage_NoPlayback"] = "Chưa có lượt nghe nào.",
            ["HistoryPage_Summary"] = "Bạn đã khám phá {0} địa điểm • {1} lượt nghe",
            ["HistoryPage_SummaryPlaces"] = "Địa điểm đã khám phá",
            ["HistoryPage_SummaryPlays"] = "Lượt nghe thuyết minh",
            ["HistoryPage_SummaryFavorite"] = "Nghe nhiều nhất",
            ["HistoryPage_ReplayButton"] = "Nghe lại",
            ["HistoryPage_SourceFormat"] = "Nguồn: {0}",

            ["SettingsPage_Title"] = "Cài đặt",
            ["SettingsPage_Subtitle"] = "Tùy chỉnh trải nghiệm khám phá dành cho du khách",
            ["SettingsPage_Header"] = "Cài đặt ứng dụng",
            ["SettingsPage_Language"] = "Ngôn ngữ ứng dụng",
            ["SettingsPage_LanguageDescription"] = "Chọn ngôn ngữ hiển thị và thuyết minh",
            ["SettingsPage_LanguagePicker"] = "Chọn ngôn ngữ",
            ["SettingsPage_Radius"] = "Bán kính geofence",
            ["SettingsPage_RadiusDescription"] = "Ứng dụng sẽ tự phát khi bạn ở gần điểm thuyết minh",
            ["SettingsPage_RadiusValueLabel"] = "Bán kính hiện tại",
            ["SettingsPage_RadiusHint"] = "Bán kính nhỏ sẽ phát chính xác hơn. Bán kính lớn sẽ giúp bạn nghe sớm hơn.",
            ["SettingsPage_Offline"] = "Chế độ offline",
            ["SettingsPage_OfflineHint"] = "Chỉ dùng dữ liệu local khi không có mạng",
            ["SettingsPage_ApiBaseUrl"] = "API base URL (máy thật)",
            ["SettingsPage_ApiBaseUrlHint"] = "Ví dụ: http://192.168.1.10:5099",
            ["SettingsPage_Info"] = "Nếu để trống API base URL, app sẽ dùng SQLite hoặc sample data. Khi test máy thật, hãy nhập IP LAN hoặc public URL của server.",
            ["SettingsPage_Save"] = "Lưu",
            ["SettingsPage_Reset"] = "Khôi phục mặc định",
            ["SettingsPage_SaveSuccessTitle"] = "Thành công",
            ["SettingsPage_SaveSuccessMessage"] = "Đã lưu cài đặt.",
            ["SettingsPage_ResetSuccessTitle"] = "Đã khôi phục",
            ["SettingsPage_ResetSuccessMessage"] = "Đã trả về cài đặt mặc định.",

            ["PoiSync_Start"] = "Đang làm mới gợi ý tham quan...",
            ["PoiSync_Done"] = "Đã tìm thấy {0} điểm tham quan lân cận.",
            ["PoiSync_Offline"] = "Đang dùng dữ liệu đã lưu ({0} điểm tham quan).",
            ["Common_Error"] = "Lỗi: {0}",
            ["Common_Close"] = "Đóng"
        },
        ["en"] = new()
        {
            ["Shell_List"] = "Places",
            ["Shell_Map"] = "Map",
            ["Shell_QR"] = "QR",
            ["Shell_History"] = "History",
            ["Shell_Settings"] = "Settings",

            ["PoiPage_Title"] = "Audio points",
            ["PoiPage_RadiusFormat"] = "Radius: {0}m",
            ["PoiPage_LoadButton"] = "Load POIs",

            ["MapPage_Title"] = "Vinh Khanh map",
            ["MapPage_LegendTitle"] = "Map legend",
            ["MapPage_LegendUser"] = "Blue dot: your location",
            ["MapPage_LegendPoi"] = "Red dot: food POI",
            ["MapPage_LegendNearest"] = "Amber dot: nearest POI",
            ["MapPage_LegendRoute"] = "Blue line: suggested route",
            ["MapPage_NearestRoute"] = "Suggested visit near you",
            ["MapPage_NearbyPlaceLabel"] = "Nearest place",
            ["MapPage_TrackingPlaceLabel"] = "Tracking",
            ["MapPage_SuggestedRouteLabel"] = "Suggested route",
            ["MapPage_DistanceFormat"] = "{0:F0} m from you",
            ["MapPage_NoNearest"] = "No nearby attraction found",
            ["MapPage_Track"] = "Track",
            ["MapPage_WaitingGps"] = "Finding your location...",
            ["MapPage_GpsTracking"] = "Finding your location...",
            ["MapPage_GpsSimulated"] = "Using a test location to suggest nearby places.",
            ["MapPage_GpsPermissionDenied"] = "Enable location permission for more accurate suggestions.",
            ["MapPage_GpsDisabled"] = "Turn on location to see attractions near you.",
            ["MapPage_GpsUnavailable"] = "Your location is not available yet.",
            ["MapPage_GpsError"] = "Your location is not available yet: {0}",
            ["MapPage_PlaybackIdle"] = "",
            ["MapPage_PlaybackEmpty"] = "This place does not have narration yet.",
            ["MapPage_PlaybackCooldown"] = "You just heard this narration. Please try again in a few minutes.",
            ["MapPage_PlaybackStarted"] = "Playing narration: {0}",
            ["MapPage_PlaybackCompleted"] = "You just heard: {0}",
            ["MapPage_PlaybackFailed"] = "Could not play narration for {0}.",

            ["QrPage_Title"] = "Scan QR code",
            ["QrPage_Hint"] = "Place a QR code with POI ID inside the frame",
            ["QrPage_TestButton"] = "Quick test: POI 1",
            ["QrPage_AimCamera"] = "Point the camera at the QR code",
            ["QrPage_CameraUnavailable"] = "Live camera is disabled on the emulator to avoid crashes. You can still paste QR content below.",
            ["QrPage_ManualTitle"] = "Paste QR content for demo",
            ["QrPage_ManualPlaceholder"] = "Example: 1, poi:1, or a server URL",
            ["QrPage_ManualSubmit"] = "Process pasted QR",
            ["QrPage_Invalid"] = "Invalid QR code: {0}",
            ["QrPage_PoiNotFound"] = "POI #{0} was not found",
            ["QrPage_Playing"] = "Playing: {0}",
            ["QrPage_Done"] = "Done. Scan another code?",

            ["HistoryPage_Title"] = "Playback history",
            ["HistoryPage_Header"] = "Your journey",
            ["HistoryPage_Subtitle"] = "Places you explored around Vinh Khanh food street",
            ["HistoryPage_Top"] = "Most played POIs",
            ["HistoryPage_NoStats"] = "No analytics data yet.",
            ["HistoryPage_Recent"] = "Recent history",
            ["HistoryPage_NoPlayback"] = "No playback recorded yet.",
            ["HistoryPage_Summary"] = "You explored {0} places • {1} plays",
            ["HistoryPage_SummaryPlaces"] = "Places explored",
            ["HistoryPage_SummaryPlays"] = "Narration plays",
            ["HistoryPage_SummaryFavorite"] = "Most played place",
            ["HistoryPage_ReplayButton"] = "Replay",
            ["HistoryPage_SourceFormat"] = "Source: {0}",

            ["SettingsPage_Title"] = "Settings",
            ["SettingsPage_Subtitle"] = "Adjust the travel experience for visitors",
            ["SettingsPage_Header"] = "App settings",
            ["SettingsPage_Language"] = "App language",
            ["SettingsPage_LanguageDescription"] = "Choose the display and narration language",
            ["SettingsPage_LanguagePicker"] = "Choose language",
            ["SettingsPage_Radius"] = "Geofence radius",
            ["SettingsPage_RadiusDescription"] = "The app will play automatically when you get close to a point",
            ["SettingsPage_RadiusValueLabel"] = "Current radius",
            ["SettingsPage_RadiusHint"] = "Shorter distances are more precise. Larger distances let you hear the narration sooner.",
            ["SettingsPage_Offline"] = "Offline mode",
            ["SettingsPage_OfflineHint"] = "Use only local data when internet is unavailable",
            ["SettingsPage_ApiBaseUrl"] = "API base URL (real device)",
            ["SettingsPage_ApiBaseUrlHint"] = "Example: http://192.168.1.10:5099",
            ["SettingsPage_Info"] = "Leave API base URL empty to use SQLite or sample data. On a real phone, enter the server LAN IP or a public URL.",
            ["SettingsPage_Save"] = "Save",
            ["SettingsPage_Reset"] = "Reset to default",
            ["SettingsPage_SaveSuccessTitle"] = "Success",
            ["SettingsPage_SaveSuccessMessage"] = "Settings have been saved.",
            ["SettingsPage_ResetSuccessTitle"] = "Reset completed",
            ["SettingsPage_ResetSuccessMessage"] = "Settings were restored to default.",

            ["PoiSync_Start"] = "Refreshing visit suggestions...",
            ["PoiSync_Done"] = "Found {0} nearby attractions.",
            ["PoiSync_Offline"] = "Using saved data ({0} attractions).",
            ["Common_Error"] = "Error: {0}",
            ["Common_Close"] = "Close"
        },
        ["zh"] = new()
        {
            ["Shell_List"] = "地点",
            ["Shell_Map"] = "地图",
            ["Shell_QR"] = "二维码",
            ["Shell_History"] = "历史",
            ["Shell_Settings"] = "设置",

            ["PoiPage_Title"] = "讲解点",
            ["PoiPage_RadiusFormat"] = "半径：{0}米",
            ["PoiPage_LoadButton"] = "加载 POI",

            ["MapPage_Title"] = "永庆地图",
            ["MapPage_LegendTitle"] = "地图图例",
            ["MapPage_LegendUser"] = "蓝点：您的位置",
            ["MapPage_LegendPoi"] = "红点：美食 POI",
            ["MapPage_LegendNearest"] = "黄点：最近 POI",
            ["MapPage_LegendRoute"] = "蓝线：推荐路线",
            ["MapPage_NearestRoute"] = "您附近的游览建议",
            ["MapPage_NearbyPlaceLabel"] = "离您最近的地点",
            ["MapPage_TrackingPlaceLabel"] = "正在跟踪",
            ["MapPage_SuggestedRouteLabel"] = "推荐游览路线",
            ["MapPage_DistanceFormat"] = "距离您 {0:F0} 米",
            ["MapPage_NoNearest"] = "附近暂未找到景点",
            ["MapPage_Track"] = "跟踪",
            ["MapPage_WaitingGps"] = "正在确定您的位置...",
            ["MapPage_GpsTracking"] = "正在确定您的位置...",
            ["MapPage_GpsSimulated"] = "正在使用测试位置为您推荐附近地点。",
            ["MapPage_GpsPermissionDenied"] = "开启定位权限后，推荐会更准确。",
            ["MapPage_GpsDisabled"] = "请开启定位以查看附近景点。",
            ["MapPage_GpsUnavailable"] = "暂时无法确定您的位置。",
            ["MapPage_GpsError"] = "暂时无法确定您的位置：{0}",
            ["MapPage_PlaybackIdle"] = "",
            ["MapPage_PlaybackEmpty"] = "该地点暂无讲解内容。",
            ["MapPage_PlaybackCooldown"] = "您刚听过这个地点的讲解，请稍后再试。",
            ["MapPage_PlaybackStarted"] = "正在播放讲解：{0}",
            ["MapPage_PlaybackCompleted"] = "您刚听过：{0}",
            ["MapPage_PlaybackFailed"] = "暂时无法播放 {0} 的讲解。",

            ["QrPage_Title"] = "扫描二维码",
            ["QrPage_Hint"] = "请将包含 POI ID 的二维码放入框内",
            ["QrPage_TestButton"] = "快速测试：POI 1",
            ["QrPage_AimCamera"] = "请将摄像头对准二维码",
            ["QrPage_CameraUnavailable"] = "为避免崩溃，模拟器上已关闭实时相机。您仍可在下方粘贴 QR 内容。",
            ["QrPage_ManualTitle"] = "粘贴 QR 内容进行演示",
            ["QrPage_ManualPlaceholder"] = "例如：1、poi:1 或服务器 URL",
            ["QrPage_ManualSubmit"] = "处理已粘贴的 QR",
            ["QrPage_Invalid"] = "无效二维码：{0}",
            ["QrPage_PoiNotFound"] = "未找到编号 {0} 的点位",
            ["QrPage_Playing"] = "正在播放：{0}",
            ["QrPage_Done"] = "完成，继续扫描其他二维码？",

            ["HistoryPage_Title"] = "播放历史",
            ["HistoryPage_Header"] = "您的旅程",
            ["HistoryPage_Subtitle"] = "您在永庆美食街探索过的地点",
            ["HistoryPage_Top"] = "播放最多的 POI",
            ["HistoryPage_NoStats"] = "暂无统计数据。",
            ["HistoryPage_Recent"] = "最近记录",
            ["HistoryPage_NoPlayback"] = "暂无播放记录。",
            ["HistoryPage_Summary"] = "您已探索 {0} 个地点 • 共播放 {1} 次",
            ["HistoryPage_SummaryPlaces"] = "已探索地点",
            ["HistoryPage_SummaryPlays"] = "讲解播放次数",
            ["HistoryPage_SummaryFavorite"] = "播放最多的地点",
            ["HistoryPage_ReplayButton"] = "再次播放",
            ["HistoryPage_SourceFormat"] = "来源：{0}",

            ["SettingsPage_Title"] = "设置",
            ["SettingsPage_Subtitle"] = "为游客调整使用体验",
            ["SettingsPage_Header"] = "应用设置",
            ["SettingsPage_Language"] = "应用语言",
            ["SettingsPage_LanguageDescription"] = "选择界面与讲解语言",
            ["SettingsPage_LanguagePicker"] = "选择语言",
            ["SettingsPage_Radius"] = "地理围栏半径",
            ["SettingsPage_RadiusDescription"] = "当您靠近讲解点时，应用会自动播放",
            ["SettingsPage_RadiusValueLabel"] = "当前半径",
            ["SettingsPage_RadiusHint"] = "距离较小时更精准，距离较大时可以更早听到讲解。",
            ["SettingsPage_Offline"] = "离线模式",
            ["SettingsPage_OfflineHint"] = "无网络时只使用本地数据",
            ["SettingsPage_ApiBaseUrl"] = "API 基础地址（真机）",
            ["SettingsPage_ApiBaseUrlHint"] = "例如：http://192.168.1.10:5099",
            ["SettingsPage_Info"] = "如果 API 基础地址留空，应用会使用 SQLite 或示例数据。真机测试时，请填写服务器的局域网 IP 或公网地址。",
            ["SettingsPage_Save"] = "保存",
            ["SettingsPage_Reset"] = "恢复默认",
            ["SettingsPage_SaveSuccessTitle"] = "成功",
            ["SettingsPage_SaveSuccessMessage"] = "设置已保存。",
            ["SettingsPage_ResetSuccessTitle"] = "已恢复",
            ["SettingsPage_ResetSuccessMessage"] = "已恢复为默认设置。",

            ["PoiSync_Start"] = "正在刷新游览建议...",
            ["PoiSync_Done"] = "已找到 {0} 个附近景点。",
            ["PoiSync_Offline"] = "正在使用已保存的数据（{0} 个景点）。",
            ["Common_Error"] = "错误：{0}",
            ["Common_Close"] = "关闭"
        }
    };

    private string _currentLanguage = "vi";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => Translate(key);

    public string Translate(string key, params object[] args)
    {
        var lang = _translations.TryGetValue(_currentLanguage, out var table)
            ? table
            : _translations["vi"];

        if (!lang.TryGetValue(key, out var value))
            value = _translations["vi"].GetValueOrDefault(key, key);

        return args.Length > 0 ? string.Format(value, args) : value;
    }

    public void SetLanguage(string languageCode)
    {
        var normalized = Normalize(languageCode);
        if (normalized == _currentLanguage)
            return;

        _currentLanguage = normalized;
        CultureInfo.CurrentCulture = new CultureInfo(MapToCultureCode(_currentLanguage));
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    private static string Normalize(string languageCode)
    {
        if (languageCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            return "en";

        if (languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return "zh";

        return "vi";
    }

    private static string MapToCultureCode(string lang) => lang switch
    {
        "en" => "en-US",
        "zh" => "zh-CN",
        _ => "vi-VN"
    };
}
