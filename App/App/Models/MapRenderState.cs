namespace App.Models;

public sealed class MapRenderState
{
    public string TileUrlTemplate { get; init; } = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    public string TileAttribution { get; init; } = "&copy; OpenStreetMap contributors";
    public List<MapPoiRenderModel> Pois { get; init; } = [];
    public MapLocationRenderModel? UserLocation { get; init; }
    public MapRouteRenderModel? Route { get; init; }
    public MapBoundsRenderModel? Bounds { get; init; }
    public int? NearestPoiId { get; init; }
    public int? TrackingPoiId { get; init; }
    public int? PopupPoiId { get; init; }
    public bool FitToPois { get; init; }
    public bool FocusOnRoute { get; init; }
    public bool FollowUser { get; init; }
}

public sealed class MapPoiRenderModel
{
    public int Id { get; init; }
    public string Ten { get; init; } = string.Empty;
    public string MoTa { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public double Lat { get; init; }
    public double Lng { get; init; }
    public double BanKinh { get; init; }
    public bool IsNearest { get; init; }
    public bool IsTracking { get; init; }
}

public sealed class MapLocationRenderModel
{
    public double Lat { get; init; }
    public double Lng { get; init; }
}

public sealed class MapRouteRenderModel
{
    public MapLocationRenderModel? Origin { get; init; }
    public MapLocationRenderModel? Destination { get; init; }
    public List<MapLocationRenderModel> Points { get; init; } = [];
}

public sealed class MapBoundsRenderModel
{
    public double MinLat { get; init; }
    public double MinLng { get; init; }
    public double MaxLat { get; init; }
    public double MaxLng { get; init; }
}
