using System.Globalization;
using System.Text.Json;
using AcTrackGenerator.Ac.Export.AiLine;

namespace AcTrackGenerator.Ac.Export.Packaging;

public sealed record MinimalTrackPackageRequest(
    string TrackId,
    string Kn5SourcePath,
    AiSpline FastLane,
    AiSpline? PitLane,
    UiTrackInfo UiTrackInfo,
    string OutputRootDirectory);

public sealed record MinimalTrackPackageResult(string TrackDirectory);

/// <summary>
/// Assembles the smallest <c>content/tracks/&lt;id&gt;</c> folder AC will load:
/// a KN5, <c>ai/fast_lane.ai</c> (+ optional <c>pit_lane.ai</c>), a minimal
/// <c>ui/ui_track.json</c>, placeholder preview/outline images, and a
/// <c>data/surfaces.ini</c> that explicitly maps the "ROAD" key the road
/// mesh's "1ROAD" name already implies under AC's built-in default - belt and
/// suspenders, not strictly required. <c>data/map.png</c>/<c>map.ini</c> are
/// intentionally omitted; AC regenerates them itself once an AI line exists.
/// </summary>
public static class MinimalTrackPackager
{
    public static MinimalTrackPackageResult Package(MinimalTrackPackageRequest request)
    {
        var trackDirectory = Path.Combine(request.OutputRootDirectory, request.TrackId);
        var uiDirectory = Path.Combine(trackDirectory, "ui");
        var aiDirectory = Path.Combine(trackDirectory, "ai");
        var dataDirectory = Path.Combine(trackDirectory, "data");

        Directory.CreateDirectory(uiDirectory);
        Directory.CreateDirectory(aiDirectory);
        Directory.CreateDirectory(dataDirectory);

        File.Copy(request.Kn5SourcePath, Path.Combine(trackDirectory, $"{request.TrackId}.kn5"), overwrite: true);

        WriteAiSpline(Path.Combine(aiDirectory, "fast_lane.ai"), request.FastLane);
        if (request.PitLane is not null)
        {
            WriteAiSpline(Path.Combine(aiDirectory, "pit_lane.ai"), request.PitLane);
        }

        File.WriteAllText(Path.Combine(uiDirectory, "ui_track.json"), BuildUiTrackJson(request.UiTrackInfo));
        File.WriteAllText(Path.Combine(dataDirectory, "surfaces.ini"), DefaultSurfacesIni);

        WritePlaceholderPng(Path.Combine(uiDirectory, "preview.png"));
        WritePlaceholderPng(Path.Combine(uiDirectory, "outline.png"));

        return new MinimalTrackPackageResult(trackDirectory);
    }

    private static void WriteAiSpline(string path, AiSpline spline)
    {
        using var stream = File.Create(path);
        spline.WriteTo(stream);
    }

    private static string BuildUiTrackJson(UiTrackInfo info)
    {
        var document = new
        {
            name = info.Name,
            description = info.Description,
            tags = info.Tags,
            geotags = info.GeoTags,
            country = info.Country,
            city = info.City,
            length = info.LengthMeters,
            width = info.WidthMeters,
            pitboxes = info.Pitboxes.ToString(CultureInfo.InvariantCulture),
            run = info.Run,
        };
        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    // A single transparent pixel: AC's UI shows this instead of failing to load the
    // track. A real screenshot/outline is a Phase 4 "track quality" concern.
    private static readonly byte[] PlaceholderPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    private static void WritePlaceholderPng(string path) => File.WriteAllBytes(path, PlaceholderPngBytes);

    private const string DefaultSurfacesIni = """
        [SURFACE_0]
        KEY=ROAD
        FRICTION=1.0
        DAMPING=0
        WAV=
        WAV_PIT=
        FF_EFFECT=
        DIRT_ADDITIVE=0
        IS_PITLANE=0
        IS_VALID_TRACK=1
        SIN_HEIGHT=0
        SIN_LENGTH=0
        VIBRATION_GAIN=0
        VIBRATION_LENGTH=0

        [SURFACE_1]
        KEY=KERB
        FRICTION=0.9
        DAMPING=0
        WAV=
        WAV_PIT=
        FF_EFFECT=
        DIRT_ADDITIVE=0
        IS_PITLANE=0
        IS_VALID_TRACK=1
        SIN_HEIGHT=0.02
        SIN_LENGTH=0.3
        VIBRATION_GAIN=0.2
        VIBRATION_LENGTH=0.3

        [SURFACE_2]
        KEY=GRASS
        FRICTION=0.6
        DAMPING=0
        WAV=
        WAV_PIT=
        FF_EFFECT=
        DIRT_ADDITIVE=0
        IS_PITLANE=0
        IS_VALID_TRACK=0
        SIN_HEIGHT=0
        SIN_LENGTH=0
        VIBRATION_GAIN=0
        VIBRATION_LENGTH=0

        """;
}
