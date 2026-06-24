namespace AcTrackGenerator.Ac.Export.Kn5Pipeline;

/// <summary>
/// The moppius/blender-assetto-corsa-tools checkout has its own <c>__init__.py</c>
/// at the repository root, so the repository root itself is the importable Python
/// package - but a plain git checkout is named "blender-assetto-corsa-tools", and
/// Python's <c>import</c> statement cannot reference an identifier containing
/// hyphens. This copies the add-on into a temp directory under a valid identifier
/// so <see cref="SceneBuilder.BlenderScriptBuilder"/>'s generated
/// <c>import &lt;module&gt;</c> statement works regardless of how the source
/// checkout happens to be named on disk.
/// </summary>
public static class AddonStaging
{
    public const string StagedModuleName = "blender_assetto_corsa_tools";

    /// <summary>Copies the add-on source tree into a fresh temp directory, returning that directory's path (the value to use as <c>AddonParentDirectory</c>).</summary>
    public static string StageForImport(string addonSourceDirectory)
    {
        if (!Directory.Exists(addonSourceDirectory))
        {
            throw new DirectoryNotFoundException($"Add-on source directory not found: '{addonSourceDirectory}'.");
        }

        var stagingParent = Path.Combine(Path.GetTempPath(), $"ac_track_generator_addon_{Guid.NewGuid():N}");
        var stagedModuleDirectory = Path.Combine(stagingParent, StagedModuleName);
        CopyDirectory(addonSourceDirectory, stagedModuleDirectory);
        return stagingParent;
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var sourceFilePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFilePath);
            if (IsExcludedFromStaging(relativePath))
            {
                continue;
            }

            var destinationFilePath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);
            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
        }
    }

    private static bool IsExcludedFromStaging(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains(".git") || segments.Contains("__pycache__");
    }
}
