using System.Text;
using System.Text.Json;
using AcTrackGenerator.Common;

namespace AcTrackGenerator.Terrain.Elevation;

/// <summary>
/// Fetches elevation samples from an Open-Elevation-API-compatible endpoint
/// (the public instance, or a self-hosted one - the request/response shape
/// is the same). Takes an injected <see cref="HttpClient"/> so tests can
/// substitute a fake handler instead of hitting the real service.
/// </summary>
public sealed class OpenElevationClient
{
    public const string DefaultEndpoint = "https://api.open-elevation.com/api/v1/lookup";

    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public OpenElevationClient(HttpClient httpClient, string endpoint = DefaultEndpoint)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
    }

    public async Task<IReadOnlyList<ElevationSample>> GetElevationsAsync(
        IReadOnlyList<GeoCoordinate> locations, CancellationToken cancellationToken = default)
    {
        if (locations.Count == 0)
        {
            return [];
        }

        var requestBody = JsonSerializer.Serialize(new
        {
            locations = locations.Select(l => new { latitude = l.Latitude, longitude = l.Longitude }),
        });

        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return OpenElevationResponseParser.Parse(json);
    }
}
