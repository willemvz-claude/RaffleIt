using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcTrackGenerator.Osm.Ingestion;

public static class OverpassResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static OsmExtract Parse(string json)
    {
        var document = JsonSerializer.Deserialize<OverpassDocument>(json, SerializerOptions)
            ?? throw new FormatException("Overpass response could not be parsed as JSON.");

        var nodes = new List<OsmNode>();
        var ways = new List<OsmWay>();

        foreach (var element in document.Elements)
        {
            switch (element.Type)
            {
                case "node" when element.Lat is double lat && element.Lon is double lon:
                    nodes.Add(new OsmNode(element.Id, lat, lon));
                    break;
                case "way":
                    ways.Add(new OsmWay(
                        element.Id,
                        element.Nodes ?? [],
                        element.Tags ?? new Dictionary<string, string>()));
                    break;
            }
        }

        return new OsmExtract(nodes, ways);
    }

    private sealed class OverpassDocument
    {
        public List<OverpassElement> Elements { get; set; } = [];
    }

    private sealed class OverpassElement
    {
        public string Type { get; set; } = "";

        public long Id { get; set; }

        public double? Lat { get; set; }

        public double? Lon { get; set; }

        public List<long>? Nodes { get; set; }

        public Dictionary<string, string>? Tags { get; set; }
    }
}
