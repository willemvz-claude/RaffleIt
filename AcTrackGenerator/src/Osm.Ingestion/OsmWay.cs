namespace AcTrackGenerator.Osm.Ingestion;

public sealed record OsmWay(long Id, IReadOnlyList<long> NodeIds, IReadOnlyDictionary<string, string> Tags)
{
    public string? Tag(string key) => Tags.TryGetValue(key, out var value) ? value : null;
}
