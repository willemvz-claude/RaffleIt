using AcTrackGenerator.Ac.Export.AiLine;
using AcTrackGenerator.Geometry.TrackLayout;

namespace AcTrackGenerator.Ac.Export.AiLine.Tests;

public class AiSplineBuilderTests
{
    private static TrackCenterline BuildOvalCenterline() =>
        StadiumOvalLayoutGenerator.Generate(new StadiumOvalParameters());

    [Fact]
    public void Build_ProducesOnePointPerCenterlinePoint_AtMatchingPositions()
    {
        var centerline = BuildOvalCenterline();
        var spline = AiSplineBuilder.Build(centerline, new AiSplineBuildParameters());

        Assert.Equal(centerline.Points.Count, spline.Points.Count);
        Assert.Equal(centerline.Points.Count, spline.PointsExtra.Count);
        for (var i = 0; i < centerline.Points.Count; i++)
        {
            Assert.Equal(centerline.Points[i].Position, spline.Points[i].Position);
        }
    }

    [Fact]
    public void Build_NeverExceedsConfiguredMaxSpeed()
    {
        var centerline = BuildOvalCenterline();
        var parameters = new AiSplineBuildParameters();
        var spline = AiSplineBuilder.Build(centerline, parameters);

        Assert.All(spline.PointsExtra, extra => Assert.True(extra.Speed <= parameters.MaxSpeedMetersPerSecond + 1e-3f));
    }

    [Fact]
    public void Build_SlowsDownForTheTurnsRelativeToTheStraights()
    {
        var centerline = BuildOvalCenterline();
        var parameters = new AiSplineBuildParameters();
        var spline = AiSplineBuilder.Build(centerline, parameters);

        // The synthetic oval's turns are tight enough (see StadiumOvalParameters.TurnRadius)
        // that the cornering-speed formula must pull the target well below top speed somewhere
        // on the lap - otherwise the speed/braking model isn't doing anything.
        var slowestSpeed = spline.PointsExtra.Min(extra => extra.Speed);
        Assert.True(slowestSpeed < parameters.MaxSpeedMetersPerSecond * 0.75f);
    }

    [Fact]
    public void Build_GasAndBrakeAreAlwaysComplementary()
    {
        var centerline = BuildOvalCenterline();
        var spline = AiSplineBuilder.Build(centerline, new AiSplineBuildParameters());

        Assert.All(spline.PointsExtra, extra => Assert.Equal(1f, extra.Gas + extra.Brake));
    }

    [Fact]
    public void Build_RadiusIsInverseOfCurvatureWhereCurvatureIsNonZero()
    {
        var centerline = BuildOvalCenterline();
        var spline = AiSplineBuilder.Build(centerline, new AiSplineBuildParameters());

        for (var i = 0; i < centerline.Points.Count; i++)
        {
            var curvature = MathF.Abs(centerline.Points[i].SignedCurvature);
            if (curvature > 1e-6f)
            {
                Assert.Equal(1f / curvature, spline.PointsExtra[i].Radius, precision: 3);
            }
            else
            {
                Assert.Equal(0f, spline.PointsExtra[i].Radius);
            }
        }
    }
}
