using AcTrackGenerator.Common;

namespace AcTrackGenerator.Osm.Ingestion;

public static class OverpassQueryBuilder
{
    /// <summary>
    /// Builds an Overpass QL query for every <c>highway</c>-tagged way in the given
    /// bounding box, plus the nodes those ways reference (<c>(._;>;)</c> recurses
    /// down from the matched ways to their member nodes).
    /// </summary>
    public static string BuildHighwaysQuery(GeoBoundingBox boundingBox, int timeoutSeconds = 25)
    {
        return FormattableString.Invariant($"""
            [out:json][timeout:{timeoutSeconds}];
            way["highway"]({boundingBox.MinLatitude},{boundingBox.MinLongitude},{boundingBox.MaxLatitude},{boundingBox.MaxLongitude});
            (._;>;);
            out body;
            """);
    }
}
