namespace AcTrackGenerator.Geometry.TrackLayout;

/// <summary>
/// A drivable line through the middle of the road, sampled at regular
/// intervals. In later phases this is derived from an OSM way; for the
/// Phase 0 spike it comes from a synthetic generator instead.
/// </summary>
public sealed class TrackCenterline
{
    public IReadOnlyList<CenterlinePoint> Points { get; }
    public bool IsClosedLoop { get; }

    public TrackCenterline(IReadOnlyList<CenterlinePoint> points, bool isClosedLoop)
    {
        if (points.Count < 3) throw new ArgumentException("A centerline needs at least 3 points", nameof(points));
        Points = points;
        IsClosedLoop = isClosedLoop;
    }

    public float TotalLength => Points[^1].CumulativeLength;

    /// <summary>Index of the point that follows <paramref name="index"/>, wrapping around for closed loops.</summary>
    public int NextIndex(int index) => IsClosedLoop
        ? (index + 1) % Points.Count
        : Math.Min(index + 1, Points.Count - 1);
}
