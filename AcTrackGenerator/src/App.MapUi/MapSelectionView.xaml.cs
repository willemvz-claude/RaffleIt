using System.Windows.Controls;

namespace AcTrackGenerator.App.MapUi;

/// <summary>
/// Hosts the Leaflet.js/OSM area-selection map inside a WebView2 control.
/// The actual Leaflet page and the JS-to-.NET bridge for the drawn selection
/// box are built out in Phase 1; this control currently just wires up the
/// WebView2 host so the rest of the app can reference it.
/// </summary>
public partial class MapSelectionView : UserControl
{
    public MapSelectionView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        await Browser.EnsureCoreWebView2Async();
        Browser.CoreWebView2.NavigateToString(PlaceholderHtml);
    }

    private const string PlaceholderHtml = """
        <html>
          <body style="font-family: sans-serif; padding: 2rem;">
            <h2>Map selection UI</h2>
            <p>Leaflet.js + OSM tile layer and the area-selection box wiring land in Phase 1.</p>
          </body>
        </html>
        """;
}
