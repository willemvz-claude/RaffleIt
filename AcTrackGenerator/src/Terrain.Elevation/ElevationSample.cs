using AcTrackGenerator.Common;

namespace AcTrackGenerator.Terrain.Elevation;

public sealed record ElevationSample(GeoCoordinate Location, float ElevationMeters);
