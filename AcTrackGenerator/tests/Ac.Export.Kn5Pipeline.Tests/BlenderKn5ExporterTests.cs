using AcTrackGenerator.Ac.Domain;
using AcTrackGenerator.Common;
using Xunit.Abstractions;

namespace AcTrackGenerator.Ac.Export.Kn5Pipeline.Tests;

/// <summary>
/// End-to-end check that headless Blender + the moppius add-on actually produce a
/// KN5 file. Skips (rather than fails) when either Blender or the add-on checkout
/// isn't available in the current environment, since neither is guaranteed on every
/// machine this test suite runs on.
/// </summary>
public class BlenderKn5ExporterTests
{
    private readonly ITestOutputHelper _output;

    public BlenderKn5ExporterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Export_WithATrivialScene_ProducesAValidKn5File()
    {
        string blenderExecutable;
        try
        {
            blenderExecutable = BlenderExecutableLocator.Locate();
        }
        catch (FileNotFoundException)
        {
            _output.WriteLine("Skipping: no Blender executable found in this environment.");
            return;
        }

        var addonSourceDirectory = TryLocateAddonSourceDirectory();
        if (addonSourceDirectory is null)
        {
            _output.WriteLine("Skipping: could not find the blender-assetto-corsa-tools submodule checkout.");
            return;
        }

        var scene = new TrackScene
        {
            TrackId = "kn5_exporter_test",
            Materials = [new AcMaterial { Name = "test_material" }],
            Meshes =
            [
                new AcMesh
                {
                    Name = "1ROAD",
                    MaterialName = "test_material",
                    Vertices = [new Vec3(0f, 0f, 0f), new Vec3(1f, 0f, 0f), new Vec3(0f, 0f, 1f)],
                    Uvs = [new Vec2(0f, 0f), new Vec2(1f, 0f), new Vec2(0f, 1f)],
                    TriangleIndices = [0, 1, 2],
                },
            ],
            MarkerNodes = [],
        };

        var outputDirectory = Path.Combine(Path.GetTempPath(), $"kn5_exporter_test_{Guid.NewGuid():N}");
        var outputPath = Path.Combine(outputDirectory, "test.kn5");

        try
        {
            var result = BlenderKn5Exporter.Export(new BlenderKn5ExportRequest(
                scene,
                addonSourceDirectory,
                outputPath,
                blenderExecutable));

            Assert.True(File.Exists(result.Kn5Path));

            using var stream = File.OpenRead(result.Kn5Path);
            var header = new byte[6];
            stream.ReadExactly(header);
            Assert.Equal("sc6969", System.Text.Encoding.ASCII.GetString(header));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static string? TryLocateAddonSourceDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "third_party", "blender-assetto-corsa-tools");
            if (File.Exists(Path.Combine(candidate, "__init__.py")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
