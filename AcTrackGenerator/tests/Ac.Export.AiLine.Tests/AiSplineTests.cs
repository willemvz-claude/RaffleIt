using AcTrackGenerator.Ac.Export.AiLine;
using AcTrackGenerator.Common;

namespace AcTrackGenerator.Ac.Export.AiLine.Tests;

public class AiSplineTests
{
    [Fact]
    public void WriteTo_ThenReadFrom_RoundTripsExactly()
    {
        var points = new[]
        {
            new AiSplinePoint(new Vec3(0f, 0f, 0f), 0f, 0),
            new AiSplinePoint(new Vec3(10f, 0f, 0f), 10f, 1),
            new AiSplinePoint(new Vec3(10f, 0f, 10f), 20f, 2),
        };
        var extras = new[]
        {
            new AiSplinePointExtra(30f, 1f, 0f, 0f, 0f, 5f, 5f, 0f, 0f, Vec3.Up, 0f, new Vec3(1f, 0f, 0f), 0f, 0f),
            new AiSplinePointExtra(25f, 0f, 1f, 0f, 0.1f, 5f, 5f, 0f, 0f, Vec3.Up, 10f, new Vec3(0f, 0f, 1f), 0f, 0f),
            new AiSplinePointExtra(40f, 1f, 0f, 0f, 0f, 5f, 5f, 0f, 0f, Vec3.Up, 20f, new Vec3(-1f, 0f, 0f), 0f, 0f),
        };
        var original = new AiSpline { LapTime = 12345, SampleCount = 0, Points = points, PointsExtra = extras };

        using var stream = new MemoryStream();
        original.WriteTo(stream);
        stream.Position = 0;
        var roundTripped = AiSpline.ReadFrom(stream);

        Assert.Equal(original.LapTime, roundTripped.LapTime);
        Assert.Equal(original.SampleCount, roundTripped.SampleCount);
        Assert.Equal(original.Points.Count, roundTripped.Points.Count);
        for (var i = 0; i < original.Points.Count; i++)
        {
            Assert.Equal(original.Points[i], roundTripped.Points[i]);
            Assert.Equal(original.PointsExtra[i], roundTripped.PointsExtra[i]);
        }
    }

    [Fact]
    public void ReadFrom_RejectsUnsupportedVersion()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(999); // bogus version
        }
        stream.Position = 0;

        Assert.Throws<InvalidDataException>(() => AiSpline.ReadFrom(stream));
    }
}
