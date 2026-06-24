using AcTrackGenerator.Common;

namespace AcTrackGenerator.Geometry.RoadNetwork.Tests;

public class LocalMapProjectionTests
{
    [Fact]
    public void Project_OriginMapsToZero()
    {
        var origin = new GeoCoordinate(51.5, -0.1);
        var projection = new LocalMapProjection(origin);

        var result = projection.Project(origin);

        Assert.Equal(0f, result.X, precision: 3);
        Assert.Equal(0f, result.Y, precision: 3);
    }

    [Fact]
    public void Project_OneDegreeNorth_IsApproximatelyOneHundredElevenKilometers()
    {
        var origin = new GeoCoordinate(0.0, 0.0);
        var projection = new LocalMapProjection(origin);

        var result = projection.Project(new GeoCoordinate(1.0, 0.0));

        Assert.True(result.X is > -1f and < 1f, $"expected ~0 east offset, got {result.X}");
        Assert.InRange(result.Y, 110_000f, 112_000f);
    }

    [Fact]
    public void Project_PointEastOfOrigin_HasPositiveX()
    {
        var origin = new GeoCoordinate(51.5, -0.1);
        var projection = new LocalMapProjection(origin);

        var result = projection.Project(new GeoCoordinate(51.5, 0.0));

        Assert.True(result.X > 0f);
        Assert.Equal(0f, result.Y, precision: 3);
    }

    [Fact]
    public void Project_LongitudeDegreeShrinksAwayFromTheEquator()
    {
        var equatorProjection = new LocalMapProjection(new GeoCoordinate(0.0, 0.0));
        var highLatitudeProjection = new LocalMapProjection(new GeoCoordinate(60.0, 0.0));

        var equatorOffset = equatorProjection.Project(new GeoCoordinate(0.0, 1.0));
        var highLatitudeOffset = highLatitudeProjection.Project(new GeoCoordinate(60.0, 1.0));

        Assert.True(Math.Abs(highLatitudeOffset.X) < Math.Abs(equatorOffset.X));
    }
}
