namespace AcTrackGenerator.Geometry.RoadNetwork.Tests;

public class HighwayClassificationTests
{
    [Theory]
    [InlineData("motorway")]
    [InlineData("residential")]
    [InlineData("service")]
    [InlineData("track")]
    public void TryGetWidthMeters_KnownDrivableTypes_ReturnsPositiveWidth(string highwayType)
    {
        var found = HighwayClassification.TryGetWidthMeters(highwayType, out var width);

        Assert.True(found);
        Assert.True(width > 0f);
    }

    [Theory]
    [InlineData("footway")]
    [InlineData("cycleway")]
    [InlineData("path")]
    [InlineData("steps")]
    [InlineData("pedestrian")]
    public void TryGetWidthMeters_NonDrivableTypes_ReturnsFalse(string highwayType)
    {
        var found = HighwayClassification.TryGetWidthMeters(highwayType, out _);

        Assert.False(found);
    }
}
