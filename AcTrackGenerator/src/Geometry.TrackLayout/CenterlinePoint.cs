using AcTrackGenerator.Common;

namespace AcTrackGenerator.Geometry.TrackLayout;

/// <summary>One sample along a track centerline, in AC world space (Y-up, meters).</summary>
public readonly record struct CenterlinePoint(Vec3 Position, Vec3 Forward, float CumulativeLength, float SignedCurvature)
{
    /// <summary>Local "right" direction (90 degrees clockwise from Forward, viewed from above).</summary>
    public Vec3 Right => Vec3.Cross(Vec3.Up, Forward).Normalized();
}
