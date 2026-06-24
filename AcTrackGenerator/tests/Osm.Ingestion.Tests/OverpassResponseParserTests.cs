namespace AcTrackGenerator.Osm.Ingestion.Tests;

public class OverpassResponseParserTests
{
    private const string SampleResponse = """
        {
          "version": 0.6,
          "generator": "Overpass API",
          "elements": [
            {
              "type": "node",
              "id": 100,
              "lat": 51.50000,
              "lon": -0.20000
            },
            {
              "type": "node",
              "id": 101,
              "lat": 51.50100,
              "lon": -0.20050
            },
            {
              "type": "node",
              "id": 102,
              "lat": 51.50200,
              "lon": -0.20100
            },
            {
              "type": "way",
              "id": 200,
              "nodes": [100, 101, 102],
              "tags": {
                "highway": "residential",
                "name": "Test Street"
              }
            }
          ]
        }
        """;

    [Fact]
    public void Parse_ExtractsNodesWithCoordinates()
    {
        var extract = OverpassResponseParser.Parse(SampleResponse);

        Assert.Equal(3, extract.Nodes.Count);
        var first = Assert.Single(extract.Nodes, n => n.Id == 100);
        Assert.Equal(51.50000, first.Latitude, precision: 5);
        Assert.Equal(-0.20000, first.Longitude, precision: 5);
    }

    [Fact]
    public void Parse_ExtractsWaysWithNodeIdsAndTags()
    {
        var extract = OverpassResponseParser.Parse(SampleResponse);

        var way = Assert.Single(extract.Ways);
        Assert.Equal(200, way.Id);
        Assert.Equal([100L, 101L, 102L], way.NodeIds);
        Assert.Equal("residential", way.Tag("highway"));
        Assert.Equal("Test Street", way.Tag("name"));
    }

    [Fact]
    public void Parse_WayWithoutTags_ReturnsEmptyTagsRatherThanThrowing()
    {
        const string json = """
            {
              "elements": [
                { "type": "way", "id": 300, "nodes": [1, 2] }
              ]
            }
            """;

        var extract = OverpassResponseParser.Parse(json);

        var way = Assert.Single(extract.Ways);
        Assert.Empty(way.Tags);
        Assert.Null(way.Tag("highway"));
    }

    [Fact]
    public void Parse_IgnoresUnknownElementTypes()
    {
        const string json = """
            {
              "elements": [
                { "type": "relation", "id": 1, "members": [] }
              ]
            }
            """;

        var extract = OverpassResponseParser.Parse(json);

        Assert.Empty(extract.Nodes);
        Assert.Empty(extract.Ways);
    }
}
