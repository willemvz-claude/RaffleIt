using AcTrackGenerator.Common;

namespace AcTrackGenerator.Osm.Ingestion;

/// <summary>
/// Fetches OSM road data for a bounding box from an Overpass API endpoint.
/// Takes an injected <see cref="HttpClient"/> so tests can substitute a fake
/// handler instead of hitting the real, rate-limited public instance.
/// </summary>
public sealed class OverpassClient
{
    public const string DefaultEndpoint = "https://overpass-api.de/api/interpreter";

    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public OverpassClient(HttpClient httpClient, string endpoint = DefaultEndpoint)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
    }

    public async Task<OsmExtract> FetchHighwaysAsync(GeoBoundingBox boundingBox, CancellationToken cancellationToken = default)
    {
        var query = OverpassQueryBuilder.BuildHighwaysQuery(boundingBox);
        using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("data", query)]);
        using var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return OverpassResponseParser.Parse(json);
    }
}
