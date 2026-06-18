using AcTrackGenerator.Common;
using AcTrackGenerator.Osm.Ingestion;

namespace AcTrackGenerator.Geometry.RoadNetwork;

public static class RoadNetworkBuilder
{
    /// <summary>
    /// Projects every drivable OSM way in <paramref name="extract"/> into local meters
    /// around <paramref name="origin"/>, dropping non-drivable highway types (footways,
    /// cycleways, etc., per <see cref="HighwayClassification"/>) and any way left with
    /// fewer than two resolvable points (e.g. a way whose nodes fell outside the bbox
    /// the extract was fetched for).
    /// </summary>
    public static RoadNetwork Build(OsmExtract extract, GeoCoordinate origin)
    {
        var projection = new LocalMapProjection(origin);
        var nodesById = extract.Nodes.ToDictionary(n => n.Id);

        var segments = new List<RoadSegment>();
        foreach (var way in extract.Ways)
        {
            var highwayType = way.Tag("highway");
            if (highwayType is null || !HighwayClassification.TryGetWidthMeters(highwayType, out var widthMeters))
            {
                continue;
            }

            var points = new List<Vec2>(way.NodeIds.Count);
            foreach (var nodeId in way.NodeIds)
            {
                if (nodesById.TryGetValue(nodeId, out var node))
                {
                    points.Add(projection.Project(new GeoCoordinate(node.Latitude, node.Longitude)));
                }
            }

            if (points.Count >= 2)
            {
                segments.Add(new RoadSegment(way.Id, highwayType, widthMeters, points));
            }
        }

        return new RoadNetwork(segments);
    }
}
