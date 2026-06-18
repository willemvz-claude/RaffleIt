using AcTrackGenerator.Common;

namespace AcTrackGenerator.Geometry.RoadNetwork;

public sealed record RoadSegment(long OsmWayId, string HighwayType, float WidthMeters, IReadOnlyList<Vec2> Points);
