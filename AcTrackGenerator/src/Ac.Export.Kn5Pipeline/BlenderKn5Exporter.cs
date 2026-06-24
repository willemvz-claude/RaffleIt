using System.Diagnostics;
using System.Text;
using AcTrackGenerator.Ac.Domain;
using AcTrackGenerator.Ac.Export.SceneBuilder;

namespace AcTrackGenerator.Ac.Export.Kn5Pipeline;

public sealed record BlenderKn5ExportRequest(
    TrackScene Scene,
    string AddonSourceDirectory,
    string OutputKn5Path,
    string? BlenderExecutablePathOverride = null);

public sealed record BlenderKn5ExportResult(
    string Kn5Path,
    string StandardOutput,
    string StandardError,
    IReadOnlyList<string> Warnings);

public sealed class BlenderExportException : Exception
{
    public string StandardOutput { get; }
    public string StandardError { get; }

    public BlenderExportException(string message, string standardOutput, string standardError)
        : base(BuildMessage(message, standardOutput, standardError))
    {
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    private static string BuildMessage(string message, string standardOutput, string standardError) =>
        $"{message}{Environment.NewLine}--- Blender stdout ---{Environment.NewLine}{standardOutput}" +
        $"{Environment.NewLine}--- Blender stderr ---{Environment.NewLine}{standardError}";
}

/// <summary>
/// Drives headless Blender (<c>blender --background --python &lt;script&gt;</c>)
/// to turn a <see cref="TrackScene"/> into a KN5 file via the moppius add-on,
/// per the bypass-the-operator's-popup-report approach described in
/// <see cref="BlenderScriptBuilder"/>.
/// </summary>
public static class BlenderKn5Exporter
{
    private const string SuccessMarkerPrefix = "KN5_EXPORT_OK ";
    private const string WarningLinePrefix = "KN5_EXPORT_WARNING: ";
    private const string Kn5MagicHeader = "sc6969";

    public static BlenderKn5ExportResult Export(BlenderKn5ExportRequest request)
    {
        var blenderExecutable = BlenderExecutableLocator.Locate(request.BlenderExecutablePathOverride);
        var stagingParent = AddonStaging.StageForImport(request.AddonSourceDirectory);

        try
        {
            var script = BlenderScriptBuilder.Build(
                request.Scene,
                new BlenderExportScriptOptions(stagingParent, AddonStaging.StagedModuleName, request.OutputKn5Path));

            var scriptPath = Path.Combine(stagingParent, "export_kn5.py");
            File.WriteAllText(scriptPath, script);

            var outputDirectory = Path.GetDirectoryName(request.OutputKn5Path);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var (exitCode, standardOutput, standardError) = RunBlender(blenderExecutable, scriptPath);

            if (exitCode != 0)
            {
                throw new BlenderExportException($"Blender exited with code {exitCode}.", standardOutput, standardError);
            }

            if (!standardOutput.Contains(SuccessMarkerPrefix, StringComparison.Ordinal))
            {
                throw new BlenderExportException(
                    $"Blender exited successfully but never printed the expected '{SuccessMarkerPrefix.Trim()}' " +
                    "marker, so the export script likely failed before reaching the KN5 writer.",
                    standardOutput,
                    standardError);
            }

            if (!File.Exists(request.OutputKn5Path))
            {
                throw new BlenderExportException(
                    $"Blender reported success but '{request.OutputKn5Path}' does not exist.",
                    standardOutput,
                    standardError);
            }

            ValidateKn5Header(request.OutputKn5Path);

            var warnings = ExtractWarnings(standardError);
            return new BlenderKn5ExportResult(request.OutputKn5Path, standardOutput, standardError, warnings);
        }
        finally
        {
            Directory.Delete(stagingParent, recursive: true);
        }
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunBlender(string blenderExecutable, string scriptPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = blenderExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--background");
        startInfo.ArgumentList.Add("--factory-startup");
        startInfo.ArgumentList.Add("--python");
        startInfo.ArgumentList.Add(scriptPath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start Blender process '{blenderExecutable}'.");

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) standardOutput.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) standardError.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (process.ExitCode, standardOutput.ToString(), standardError.ToString());
    }

    private static void ValidateKn5Header(string kn5Path)
    {
        using var stream = File.OpenRead(kn5Path);
        var buffer = new byte[Kn5MagicHeader.Length];
        var bytesRead = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        if (bytesRead != buffer.Length || Encoding.ASCII.GetString(buffer) != Kn5MagicHeader)
        {
            throw new InvalidDataException($"'{kn5Path}' does not start with the expected KN5 magic header '{Kn5MagicHeader}'.");
        }
    }

    private static List<string> ExtractWarnings(string standardError) =>
        standardError
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.StartsWith(WarningLinePrefix, StringComparison.Ordinal))
            .Select(line => line[WarningLinePrefix.Length..])
            .ToList();
}
