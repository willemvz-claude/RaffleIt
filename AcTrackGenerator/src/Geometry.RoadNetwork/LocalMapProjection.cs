using AcTrackGenerator.Common;

namespace AcTrackGenerator.Geometry.RoadNetwork;

/// <summary>
/// Flat-earth equirectangular projection from WGS84 lat/lon to local meters,
/// centered on an origin point. Accurate to a fraction of a percent across
/// the few-kilometer extents a single track selection box covers; not a
/// substitute for a proper map projection at larger scale.
/// </summary>
public sealed class LocalMapProjection
{
    private const double EarthRadiusMeters = 6_371_000.0;

    private readonly GeoCoordinate _origin;
    private readonly double _metersPerDegreeLatitude;
    private readonly double _metersPerDegreeLongitude;

    public LocalMapProjection(GeoCoordinate origin)
    {
        _origin = origin;
        _metersPerDegreeLatitude = EarthRadiusMeters * (Math.PI / 180.0);
        _metersPerDegreeLongitude = _metersPerDegreeLatitude * Math.Cos(origin.Latitude * Math.PI / 180.0);
    }

    /// <summary>Projects to local meters: X = east, Y = north.</summary>
    public Vec2 Project(GeoCoordinate point)
    {
        var east = (point.Longitude - _origin.Longitude) * _metersPerDegreeLongitude;
        var north = (point.Latitude - _origin.Latitude) * _metersPerDegreeLatitude;
        return new Vec2((float)east, (float)north);
    }
}
