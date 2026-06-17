namespace AcTrackGenerator.Ac.Export.Kn5Pipeline.Tests;

public class BlenderExecutableLocatorTests
{
    [Fact]
    public void Locate_WithExplicitPath_ReturnsItWhenTheFileExists()
    {
        var fakeExecutable = Path.GetTempFileName();
        try
        {
            Assert.Equal(fakeExecutable, BlenderExecutableLocator.Locate(fakeExecutable));
        }
        finally
        {
            File.Delete(fakeExecutable);
        }
    }

    [Fact]
    public void Locate_WithExplicitPath_ThrowsWhenTheFileDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => BlenderExecutableLocator.Locate("/no/such/blender/executable"));
    }
}
