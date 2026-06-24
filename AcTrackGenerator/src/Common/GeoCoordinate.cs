namespace AcTrackGenerator.Common;

/// <summary>
/// A WGS84 geographic coordinate, as used by OSM and elevation APIs. Distinct
/// from <see cref="Vec2"/>/<see cref="Vec3"/>, which are local Cartesian
/// meters - geo coordinates only ever exist before a <c>LocalMapProjection</c>
/// (in <c>Geometry.RoadNetwork</c>) converts them into the local space the
/// rest of the pipeline works in.
/// </summary>
public readonly record struct GeoCoordinate(double Latitude, double Longitude);
