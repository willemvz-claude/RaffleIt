namespace AcTrackGenerator.Common;

/// <summary>A lat/lon bounding box, e.g. the area a user drew on the map selection UI.</summary>
public readonly record struct GeoBoundingBox(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude)
{
    public GeoCoordinate Center => new((MinLatitude + MaxLatitude) / 2.0, (MinLongitude + MaxLongitude) / 2.0);
}
