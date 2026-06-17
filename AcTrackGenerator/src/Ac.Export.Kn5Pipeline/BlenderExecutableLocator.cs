namespace AcTrackGenerator.Ac.Export.Kn5Pipeline;

/// <summary>Finds a Blender executable to drive headlessly, without requiring the caller to know where it's installed.</summary>
public static class BlenderExecutableLocator
{
    public const string EnvironmentVariableName = "AC_TRACK_GENERATOR_BLENDER_PATH";

    /// <summary>
    /// Resolution order: an explicit path (if given), then the
    /// <see cref="EnvironmentVariableName"/> environment variable, then PATH,
    /// then well-known Windows install locations.
    /// </summary>
    public static string Locate(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return ExistingFileOrThrow(explicitPath);
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return ExistingFileOrThrow(fromEnvironment);
        }

        var fromPath = FindOnPath();
        if (fromPath is not null)
        {
            return fromPath;
        }

        foreach (var candidate in WindowsInstallCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"Could not locate a Blender executable. Checked an explicit override, the " +
            $"{EnvironmentVariableName} environment variable, PATH, and common Windows install " +
            "locations. Pass an explicit path or set the environment variable.");
    }

    private static string ExistingFileOrThrow(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Blender executable not found at '{path}'.", path);
        }

        return path;
    }

    private static string? FindOnPath()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        var executableName = OperatingSystem.IsWindows() ? "blender.exe" : "blender";
        foreach (var directory in pathVariable.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            var candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> WindowsInstallCandidates()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var blenderRoot = Path.Combine(programFiles, "Blender Foundation");
        if (!Directory.Exists(blenderRoot))
        {
            yield break;
        }

        foreach (var versionDirectory in Directory.EnumerateDirectories(blenderRoot, "Blender*"))
        {
            yield return Path.Combine(versionDirectory, "blender.exe");
        }
    }
}
