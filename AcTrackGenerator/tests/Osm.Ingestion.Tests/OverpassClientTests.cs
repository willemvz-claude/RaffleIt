using System.Net;
using AcTrackGenerator.Common;

namespace AcTrackGenerator.Osm.Ingestion.Tests;

public class OverpassClientTests
{
    [Fact]
    public async Task FetchHighwaysAsync_PostsQueryAndParsesResponse()
    {
        const string responseJson = """
            {
              "elements": [
                { "type": "node", "id": 1, "lat": 10.0, "lon": 20.0 },
                { "type": "node", "id": 2, "lat": 10.1, "lon": 20.1 },
                { "type": "way", "id": 5, "nodes": [1, 2], "tags": { "highway": "service" } }
              ]
            }
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

        var client = new OverpassClient(new HttpClient(handler), "https://example.test/interpreter");
        var boundingBox = new GeoBoundingBox(10.0, 20.0, 10.1, 20.1);

        var extract = await client.FetchHighwaysAsync(boundingBox);

        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("https://example.test/interpreter", capturedRequest.RequestUri!.ToString());
        Assert.Contains("way%5B%22highway%22%5D", capturedBody);
        Assert.Equal(2, extract.Nodes.Count);
        Assert.Equal(1, extract.Ways.Count);
        Assert.Equal("service", extract.Ways[0].Tag("highway"));
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request);
    }
}
