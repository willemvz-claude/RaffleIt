namespace AcTrackGenerator.Ac.Export.Kn5Pipeline.Tests;

public class AddonStagingTests
{
    [Fact]
    public void StageForImport_CopiesFilesUnderTheValidModuleName_AndExcludesGitAndPycache()
    {
        var sourceDirectory = Path.Combine(Path.GetTempPath(), $"addon_staging_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(sourceDirectory, ".git"));
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "__pycache__"));
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "exporter"));
        File.WriteAllText(Path.Combine(sourceDirectory, "__init__.py"), "# addon root");
        File.WriteAllText(Path.Combine(sourceDirectory, "exporter", "exporter.py"), "# exporter module");
        File.WriteAllText(Path.Combine(sourceDirectory, ".git", "config"), "should not be copied");
        File.WriteAllText(Path.Combine(sourceDirectory, "__pycache__", "cached.pyc"), "should not be copied");

        string stagingParent;
        try
        {
            stagingParent = AddonStaging.StageForImport(sourceDirectory);
        }
        finally
        {
            Directory.Delete(sourceDirectory, recursive: true);
        }

        try
        {
            var stagedModuleDirectory = Path.Combine(stagingParent, AddonStaging.StagedModuleName);
            Assert.True(File.Exists(Path.Combine(stagedModuleDirectory, "__init__.py")));
            Assert.True(File.Exists(Path.Combine(stagedModuleDirectory, "exporter", "exporter.py")));
            Assert.False(Directory.Exists(Path.Combine(stagedModuleDirectory, ".git")));
            Assert.False(Directory.Exists(Path.Combine(stagedModuleDirectory, "__pycache__")));
        }
        finally
        {
            Directory.Delete(stagingParent, recursive: true);
        }
    }

    [Fact]
    public void StageForImport_ThrowsWhenSourceDirectoryDoesNotExist()
    {
        Assert.Throws<DirectoryNotFoundException>(() => AddonStaging.StageForImport("/no/such/addon/source"));
    }
}
