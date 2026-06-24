using AcTrackGenerator.Common;
using AcTrackGenerator.Osm.Ingestion;

namespace AcTrackGenerator.Geometry.RoadNetwork.Tests;

public class RoadNetworkBuilderTests
{
    private static readonly GeoCoordinate Origin = new(51.5, -0.1);

    [Fact]
    public void Build_IncludesDrivableHighwayTypes()
    {
        var extract = new OsmExtract(
            Nodes:
            [
                new OsmNode(1, 51.5, -0.1),
                new OsmNode(2, 51.501, -0.099),
            ],
            Ways:
            [
                new OsmWay(10, [1, 2], new Dictionary<string, string> { ["highway"] = "residential" }),
            ]);

        var network = AcTrackGenerator.Geometry.RoadNetwork.RoadNetworkBuilder.Build(extract, Origin);

        var segment = Assert.Single(network.Segments);
        Assert.Equal(10, segment.OsmWayId);
        Assert.Equal("residential", segment.HighwayType);
        Assert.Equal(2, segment.Points.Count);
    }

    [Fact]
    public void Build_ExcludesNonDrivableHighwayTypes()
    {
        var extract = new OsmExtract(
            Nodes: [new OsmNode(1, 51.5, -0.1), new OsmNode(2, 51.501, -0.099)],
            Ways: [new OsmWay(10, [1, 2], new Dictionary<string, string> { ["highway"] = "footway" })]);

        var network = AcTrackGenerator.Geometry.RoadNetwork.RoadNetworkBuilder.Build(extract, Origin);

        Assert.Empty(network.Segments);
    }

    [Fact]
    public void Build_ExcludesWaysWithoutAHighwayTag()
    {
        var extract = new OsmExtract(
            Nodes: [new OsmNode(1, 51.5, -0.1), new OsmNode(2, 51.501, -0.099)],
            Ways: [new OsmWay(10, [1, 2], new Dictionary<string, string>())]);

        var network = AcTrackGenerator.Geometry.RoadNetwork.RoadNetworkBuilder.Build(extract, Origin);

        Assert.Empty(network.Segments);
    }

    [Fact]
    public void Build_SkipsWaysMissingTwoOrMoreResolvableNodes()
    {
        var extract = new OsmExtract(
            Nodes: [new OsmNode(1, 51.5, -0.1)],
            Ways: [new OsmWay(10, [1, 999], new Dictionary<string, string> { ["highway"] = "residential" })]);

        var network = AcTrackGenerator.Geometry.RoadNetwork.RoadNetworkBuilder.Build(extract, Origin);

        Assert.Empty(network.Segments);
    }

    [Fact]
    public void Build_WidthVariesByHighwayType()
    {
        var extract = new OsmExtract(
            Nodes: [new OsmNode(1, 51.5, -0.1), new OsmNode(2, 51.501, -0.099), new OsmNode(3, 51.502, -0.098)],
            Ways:
            [
                new OsmWay(10, [1, 2], new Dictionary<string, string> { ["highway"] = "motorway" }),
                new OsmWay(11, [2, 3], new Dictionary<string, string> { ["highway"] = "service" }),
            ]);

        var network = AcTrackGenerator.Geometry.RoadNetwork.RoadNetworkBuilder.Build(extract, Origin);

        var motorway = Assert.Single(network.Segments, s => s.OsmWayId == 10);
        var service = Assert.Single(network.Segments, s => s.OsmWayId == 11);
        Assert.True(motorway.WidthMeters > service.WidthMeters);
    }

    [Fact]
    public void Build_ProjectsPointsConsistentlyWithLocalMapProjection()
    {
        var extract = new OsmExtract(
            Nodes: [new OsmNode(1, 51.5, -0.1), new OsmNode(2, 51.501, -0.099)],
            Ways: [new OsmWay(10, [1, 2], new Dictionary<string, string> { ["highway"] = "residential" })]);

        var network = AcTrackGenerator.Geometry.RoadNetwork.RoadNetworkBuilder.Build(extract, Origin);
        var projection = new LocalMapProjection(Origin);

        var segment = Assert.Single(network.Segments);
        Assert.Equal(projection.Project(new GeoCoordinate(51.5, -0.1)), segment.Points[0]);
        Assert.Equal(projection.Project(new GeoCoordinate(51.501, -0.099)), segment.Points[1]);
    }
}
