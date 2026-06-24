using System.Net;
using AcTrackGenerator.Common;

namespace AcTrackGenerator.Terrain.Elevation.Tests;

public class OpenElevationClientTests
{
    [Fact]
    public async Task GetElevationsAsync_PostsLocationsAndParsesResponse()
    {
        const string responseJson = """
            { "results": [ { "latitude": 10.0, "longitude": 20.0, "elevation": 42.0 } ] }
            """;

        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson),
            };
        });

        var client = new OpenElevationClient(new HttpClient(handler), "https://example.test/lookup");

        var samples = await client.GetElevationsAsync([new GeoCoordinate(10.0, 20.0)]);

        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Contains("\"latitude\":10", capturedBody);
        Assert.Contains("\"longitude\":20", capturedBody);
        var sample = Assert.Single(samples);
        Assert.Equal(42.0f, sample.ElevationMeters, precision: 1);
    }

    [Fact]
    public async Task GetElevationsAsync_EmptyLocations_ReturnsEmptyWithoutMakingARequest()
    {
        var requested = false;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            requested = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var client = new OpenElevationClient(new HttpClient(handler));

        var samples = await client.GetElevationsAsync([]);

        Assert.Empty(samples);
        Assert.False(requested);
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request);
    }
}
