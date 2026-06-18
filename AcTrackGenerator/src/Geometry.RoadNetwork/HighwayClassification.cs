namespace AcTrackGenerator.Geometry.RoadNetwork;

/// <summary>
/// Maps an OSM <c>highway</c> tag value to a driving-surface width. Highway
/// types not listed here (footway, cycleway, path, steps, pedestrian, etc.)
/// are not drivable and are excluded from the generated road network.
/// </summary>
public static class HighwayClassification
{
    private static readonly Dictionary<string, float> WidthMetersByHighwayType = new()
    {
        ["motorway"] = 11.0f,
        ["motorway_link"] = 8.0f,
        ["trunk"] = 10.0f,
        ["trunk_link"] = 7.5f,
        ["primary"] = 8.0f,
        ["primary_link"] = 6.5f,
        ["secondary"] = 7.0f,
        ["secondary_link"] = 6.0f,
        ["tertiary"] = 6.5f,
        ["tertiary_link"] = 6.0f,
        ["unclassified"] = 5.5f,
        ["residential"] = 5.5f,
        ["living_street"] = 5.0f,
        ["service"] = 4.0f,
        ["track"] = 3.5f,
    };

    public static bool TryGetWidthMeters(string highwayType, out float widthMeters) =>
        WidthMetersByHighwayType.TryGetValue(highwayType, out widthMeters);
}
