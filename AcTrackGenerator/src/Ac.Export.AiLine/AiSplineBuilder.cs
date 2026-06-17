using AcTrackGenerator.Common;
using AcTrackGenerator.Geometry.TrackLayout;

namespace AcTrackGenerator.Ac.Export.AiLine;

public sealed record AiSplineBuildParameters(
    float SideLeft = 5f,
    float SideRight = 5f,
    float MaxSpeedMetersPerSecond = 60f, // ~216 km/h
    float MaxLateralAccelerationMetersPerSecondSquared = 11f, // ~1.1g - conservative for a first hand-rolled line
    float AssumedBrakingDecelerationMetersPerSecondSquared = 9f,
    int BrakingLookaheadPasses = 3);

/// <summary>
/// Synthesizes the simplest AI line that can plausibly work: follow the
/// centerline exactly (no racing line offset toward apexes), with a speed
/// target derived from each point's curvature and a crude backward
/// braking-distance pass so the AI doesn't try to carry full speed into a
/// corner. This is deliberately not a "good" line - the goal of the Phase 0
/// spike is only to prove that a hand-written .ai file gets the AI to drive
/// the track at all.
/// </summary>
public static class AiSplineBuilder
{
    public static AiSpline Build(TrackCenterline centerline, AiSplineBuildParameters parameters)
    {
        var pointCount = centerline.Points.Count;
        var points = new AiSplinePoint[pointCount];
        var targetSpeeds = new float[pointCount];

        for (var i = 0; i < pointCount; i++)
        {
            var centerlinePoint = centerline.Points[i];
            points[i] = new AiSplinePoint(centerlinePoint.Position, centerlinePoint.CumulativeLength, i);

            var curvature = MathF.Abs(centerlinePoint.SignedCurvature);
            var corneringSpeed = curvature > 1e-6f
                ? MathF.Sqrt(parameters.MaxLateralAccelerationMetersPerSecondSquared / curvature)
                : parameters.MaxSpeedMetersPerSecond;
            targetSpeeds[i] = MathF.Min(corneringSpeed, parameters.MaxSpeedMetersPerSecond);
        }

        ApplyBrakingLookahead(centerline, targetSpeeds, parameters);

        var extras = new AiSplinePointExtra[pointCount];
        for (var i = 0; i < pointCount; i++)
        {
            var centerlinePoint = centerline.Points[i];
            var nextIndex = centerline.NextIndex(i);
            var accelerating = targetSpeeds[nextIndex] >= targetSpeeds[i];
            var curvature = MathF.Abs(centerlinePoint.SignedCurvature);

            extras[i] = new AiSplinePointExtra(
                Speed: targetSpeeds[i],
                Gas: accelerating ? 1f : 0f,
                Brake: accelerating ? 0f : 1f,
                ObsoleteLatG: 0f,
                Radius: curvature > 1e-6f ? 1f / curvature : 0f,
                SideLeft: parameters.SideLeft,
                SideRight: parameters.SideRight,
                Camber: 0f,
                Direction: 0f,
                Normal: Vec3.Up,
                Length: centerlinePoint.CumulativeLength,
                ForwardVector: centerlinePoint.Forward,
                Tag: 0f,
                Grade: 0f);
        }

        return new AiSpline { LapTime = 0, SampleCount = 0, Points = points, PointsExtra = extras };
    }

    /// <summary>
    /// Caps each point's target speed so it's reachable under constant braking before the next,
    /// slower point. One sweep doesn't fully converge around a closed loop, so this runs a few
    /// passes - cheap, and enough for the spike's single-corner-shape oval.
    /// </summary>
    private static void ApplyBrakingLookahead(TrackCenterline centerline, float[] targetSpeeds, AiSplineBuildParameters parameters)
    {
        var pointCount = targetSpeeds.Length;
        for (var pass = 0; pass < parameters.BrakingLookaheadPasses; pass++)
        {
            for (var i = pointCount - 1; i >= 0; i--)
            {
                var current = centerline.Points[i];
                var nextIndex = centerline.NextIndex(i);
                var next = centerline.Points[nextIndex];
                var segmentLength = Vec3.Distance(current.Position, next.Position);

                var maxReachableSpeed = MathF.Sqrt(
                    targetSpeeds[nextIndex] * targetSpeeds[nextIndex]
                    + 2f * parameters.AssumedBrakingDecelerationMetersPerSecondSquared * segmentLength);
                targetSpeeds[i] = MathF.Min(targetSpeeds[i], maxReachableSpeed);
            }
        }
    }
}
