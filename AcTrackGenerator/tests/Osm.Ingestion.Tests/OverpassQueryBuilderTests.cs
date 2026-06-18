using AcTrackGenerator.Common;

namespace AcTrackGenerator.Osm.Ingestion.Tests;

public class OverpassQueryBuilderTests
{
    [Fact]
    public void BuildHighwaysQuery_IncludesBoundingBoxAndHighwayFilter()
    {
        var boundingBox = new GeoBoundingBox(51.5, -0.2, 51.6, -0.1);

        var query = OverpassQueryBuilder.BuildHighwaysQuery(boundingBox);

        Assert.Contains("[out:json]", query);
        Assert.Contains("""way["highway"](51.5,-0.2,51.6,-0.1);""", query);
        Assert.Contains("(._;>;);", query);
        Assert.Contains("out body;", query);
    }

    [Fact]
    public void BuildHighwaysQuery_FormatsCoordinatesWithInvariantCultureRegardlessOfThreadCulture()
    {
        var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var boundingBox = new GeoBoundingBox(1.5, 2.5, 3.5, 4.5);

            var query = OverpassQueryBuilder.BuildHighwaysQuery(boundingBox);

            Assert.Contains("""way["highway"](1.5,2.5,3.5,4.5);""", query);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void BuildHighwaysQuery_UsesGivenTimeout()
    {
        var boundingBox = new GeoBoundingBox(0, 0, 1, 1);

        var query = OverpassQueryBuilder.BuildHighwaysQuery(boundingBox, timeoutSeconds: 60);

        Assert.Contains("[timeout:60]", query);
    }
}
