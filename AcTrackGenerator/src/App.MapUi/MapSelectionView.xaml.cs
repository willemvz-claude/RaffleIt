using System.Text.Json;
using System.Windows.Controls;
using AcTrackGenerator.Common;
using Microsoft.Web.WebView2.Core;

namespace AcTrackGenerator.App.MapUi;

/// <summary>
/// Hosts the Leaflet.js/OSM area-selection map inside a WebView2 control.
/// The user drags a rectangle on the map; the page posts the resulting
/// lat/lon bounds back to .NET via the WebView2 message channel, which this
/// class surfaces as <see cref="AreaSelected"/>.
/// </summary>
public partial class MapSelectionView : UserControl
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public event EventHandler<GeoBoundingBox>? AreaSelected;

    public MapSelectionView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        await Browser.EnsureCoreWebView2Async();
        Browser.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        Browser.CoreWebView2.NavigateToString(MapHtml);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var json = e.TryGetWebMessageAsString();
        var selection = JsonSerializer.Deserialize<SelectionMessage>(json, JsonOptions);
        if (selection is null)
        {
            return;
        }

        AreaSelected?.Invoke(this, new GeoBoundingBox(
            selection.MinLatitude,
            selection.MinLongitude,
            selection.MaxLatitude,
            selection.MaxLongitude));
    }

    private sealed class SelectionMessage
    {
        public double MinLatitude { get; set; }

        public double MinLongitude { get; set; }

        public double MaxLatitude { get; set; }

        public double MaxLongitude { get; set; }
    }

    private const string MapHtml = """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8" />
        <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
        <style>
          html, body, #map { height: 100%; margin: 0; }
          #hint {
            position: absolute; top: 8px; left: 8px; z-index: 1000;
            background: white; padding: 4px 8px; font-family: sans-serif;
            font-size: 13px; border-radius: 4px;
          }
        </style>
        </head>
        <body>
        <div id="hint">Drag to select the area to turn into a track.</div>
        <div id="map"></div>
        <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
        <script>
          const map = L.map('map').setView([51.5074, -0.1278], 15);
          L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors',
          }).addTo(map);

          let startLatLng = null;
          let rectangle = null;

          map.on('mousedown', (e) => {
            startLatLng = e.latlng;
            if (rectangle) {
              map.removeLayer(rectangle);
              rectangle = null;
            }
            map.dragging.disable();
          });

          map.on('mousemove', (e) => {
            if (!startLatLng) return;
            const bounds = L.latLngBounds(startLatLng, e.latlng);
            if (rectangle) {
              rectangle.setBounds(bounds);
            } else {
              rectangle = L.rectangle(bounds, { color: '#ff7800', weight: 2 }).addTo(map);
            }
          });

          map.on('mouseup', (e) => {
            if (!startLatLng) return;
            const bounds = L.latLngBounds(startLatLng, e.latlng);
            startLatLng = null;
            map.dragging.enable();

            const selection = {
              minLatitude: bounds.getSouth(),
              minLongitude: bounds.getWest(),
              maxLatitude: bounds.getNorth(),
              maxLongitude: bounds.getEast(),
            };

            if (window.chrome && window.chrome.webview) {
              window.chrome.webview.postMessage(JSON.stringify(selection));
            }
          });
        </script>
        </body>
        </html>
        """;
}
