namespace AcTrackGenerator.Terrain.Elevation.Tests;

public class OpenElevationResponseParserTests
{
    [Fact]
    public void Parse_ExtractsElevationSamples()
    {
        const string json = """
            {
              "results": [
                { "latitude": 41.161758, "longitude": -8.583933, "elevation": 117.0 },
                { "latitude": 41.161759, "longitude": -8.583934, "elevation": 119.5 }
              ]
            }
            """;

        var samples = OpenElevationResponseParser.Parse(json);

        Assert.Equal(2, samples.Count);
        Assert.Equal(41.161758, samples[0].Location.Latitude, precision: 5);
        Assert.Equal(-8.583933, samples[0].Location.Longitude, precision: 5);
        Assert.Equal(117.0f, samples[0].ElevationMeters, precision: 1);
        Assert.Equal(119.5f, samples[1].ElevationMeters, precision: 1);
    }

    [Fact]
    public void Parse_NoResults_ReturnsEmptyList()
    {
        const string json = """{ "results": [] }""";

        var samples = OpenElevationResponseParser.Parse(json);

        Assert.Empty(samples);
    }
}
