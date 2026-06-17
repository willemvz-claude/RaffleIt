using System.Globalization;
using AcTrackGenerator.Ac.Domain;
using AcTrackGenerator.Ac.Export.AiLine;
using AcTrackGenerator.Ac.Export.Kn5Pipeline;
using AcTrackGenerator.Ac.Export.Packaging;
using AcTrackGenerator.Geometry.MeshGen;
using AcTrackGenerator.Geometry.TrackLayout;

namespace AcTrackGenerator.Pipeline.Orchestration;

public sealed record Phase0SpikeOptions(
    string OutputDirectory,
    string AddonSourceDirectory,
    string TrackId = "spike_oval",
    string? BlenderExecutablePathOverride = null);

public sealed record Phase0SpikeResult(
    string TrackDirectory,
    string Kn5Path,
    IReadOnlyList<string> BlenderWarnings,
    int CenterlinePointCount,
    float TrackLengthMeters);

/// <summary>
/// Wires every Phase 0 piece - a synthetic oval centerline, a flat road mesh,
/// a headless Blender KN5 export, a hand-rolled AI line, and minimal track
/// packaging - into the single end-to-end run the spike needs to de-risk
/// "can a generated track be loaded and AI-driven at all". None of the
/// individual stages know about each other; this is the only place that does.
/// </summary>
public static class Phase0SpikePipeline
{
    public static Phase0SpikeResult Run(Phase0SpikeOptions options)
    {
        var centerline = StadiumOvalLayoutGenerator.Generate(new StadiumOvalParameters());

        var roadParameters = new RoadRibbonParameters();
        var roadMesh = RoadRibbonMeshBuilder.Build(centerline, roadParameters);
        var material = new AcMaterial { Name = roadParameters.MaterialName };

        var startPosition = centerline.Points[0].Position;
        var markers = new AcMarkerNode[]
        {
            new() { Name = "AC_START_0", Position = startPosition, YawRadians = 0f },
            new() { Name = "AC_PIT_0", Position = startPosition, YawRadians = 0f },
        };

        var scene = new TrackScene
        {
            TrackId = options.TrackId,
            Meshes = [roadMesh],
            Materials = [material],
            MarkerNodes = markers,
        };

        var exportWorkingDirectory = Path.Combine(Path.GetTempPath(), $"ac_track_generator_{Guid.NewGuid():N}");
        Directory.CreateDirectory(exportWorkingDirectory);
        var kn5Path = Path.Combine(exportWorkingDirectory, $"{options.TrackId}.kn5");

        var exportResult = BlenderKn5Exporter.Export(new BlenderKn5ExportRequest(
            scene,
            options.AddonSourceDirectory,
            kn5Path,
            options.BlenderExecutablePathOverride));

        var fastLane = AiSplineBuilder.Build(centerline, new AiSplineBuildParameters(
            SideLeft: roadParameters.RoadWidth / 2f,
            SideRight: roadParameters.RoadWidth / 2f));

        var uiTrackInfo = new UiTrackInfo(
            Name: options.TrackId,
            Description: "Phase 0 spike track: a synthetic flat oval generated to prove the KN5/AI export pipeline end to end.",
            Tags: ["oval", "generated"],
            GeoTags: [],
            Country: "",
            City: "",
            LengthMeters: centerline.TotalLength.ToString("F0", CultureInfo.InvariantCulture),
            WidthMeters: roadParameters.RoadWidth.ToString("F0", CultureInfo.InvariantCulture),
            Pitboxes: 1,
            Run: "Road");

        var packageResult = MinimalTrackPackager.Package(new MinimalTrackPackageRequest(
            options.TrackId,
            exportResult.Kn5Path,
            fastLane,
            PitLane: null,
            uiTrackInfo,
            options.OutputDirectory));

        Directory.Delete(exportWorkingDirectory, recursive: true);

        return new Phase0SpikeResult(
            packageResult.TrackDirectory,
            Path.Combine(packageResult.TrackDirectory, $"{options.TrackId}.kn5"),
            exportResult.Warnings,
            centerline.Points.Count,
            centerline.TotalLength);
    }
}
