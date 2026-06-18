using System.Text.Json;
using AcTrackGenerator.Common;

namespace AcTrackGenerator.Terrain.Elevation;

public static class OpenElevationResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<ElevationSample> Parse(string json)
    {
        var document = JsonSerializer.Deserialize<ResponseDocument>(json, SerializerOptions)
            ?? throw new FormatException("Open-Elevation response could not be parsed as JSON.");

        return document.Results
            .Select(r => new ElevationSample(new GeoCoordinate(r.Latitude, r.Longitude), (float)r.Elevation))
            .ToList();
    }

    private sealed class ResponseDocument
    {
        public List<ResultEntry> Results { get; set; } = [];
    }

    private sealed class ResultEntry
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Elevation { get; set; }
    }
}
