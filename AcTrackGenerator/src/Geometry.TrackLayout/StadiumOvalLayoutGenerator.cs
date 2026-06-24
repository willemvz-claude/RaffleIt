using AcTrackGenerator.Common;

namespace AcTrackGenerator.Geometry.TrackLayout;

public sealed record StadiumOvalParameters(
    float StraightLength = 200f,
    float TurnRadius = 30f,
    int PointsPerStraight = 40,
    int PointsPerTurn = 48);

/// <summary>
/// Generates a closed-loop "stadium" oval centerline (two straights joined by
/// two 180-degree turns) entirely synthetically. This stands in for a real
/// OSM-derived centerline so the KN5/AI-line export pipeline can be
/// exercised end-to-end before <c>Osm.Ingestion</c> exists.
/// </summary>
public static class StadiumOvalLayoutGenerator
{
    public static TrackCenterline Generate(StadiumOvalParameters parameters)
    {
        var halfStraight = parameters.StraightLength / 2f;
        var radius = parameters.TurnRadius;

        var raw = new List<(Vec3 Position, float Curvature)>();

        // Top straight: from the right turn's exit back to the left turn's entry, at z = +radius,
        // traveling in -X direction.
        AddStraight(raw, new Vec3(halfStraight, 0f, radius), new Vec3(-halfStraight, 0f, radius), parameters.PointsPerStraight);

        // Left turn: half-circle centered at (-halfStraight, 0, 0), sweeping from +90deg to +270deg,
        // i.e. from (x, 0, +radius) around through (x - radius, 0, 0) to (x, 0, -radius).
        AddArc(raw, center: new Vec3(-halfStraight, 0f, 0f), radius, startAngleRad: MathF.PI / 2f, endAngleRad: 3f * MathF.PI / 2f, parameters.PointsPerTurn);

        // Bottom straight: from left turn's exit back to right turn's entry, at z = -radius,
        // traveling in +X direction.
        AddStraight(raw, new Vec3(-halfStraight, 0f, -radius), new Vec3(halfStraight, 0f, -radius), parameters.PointsPerStraight);

        // Right turn: half-circle centered at (halfStraight, 0, 0), sweeping from -90deg to +90deg.
        AddArc(raw, center: new Vec3(halfStraight, 0f, 0f), radius, startAngleRad: -MathF.PI / 2f, endAngleRad: MathF.PI / 2f, parameters.PointsPerTurn);

        return BuildClosedLoop(raw);
    }

    private static void AddStraight(List<(Vec3 Position, float Curvature)> raw, Vec3 from, Vec3 to, int pointCount)
    {
        // Skip the last point of every segment: the next segment's first point picks up there,
        // and the very last point of the whole loop is dropped by BuildClosedLoop's wrap-around.
        for (var i = 0; i < pointCount; i++)
        {
            var t = i / (float)pointCount;
            raw.Add((from + (to - from) * t, 0f));
        }
    }

    private static void AddArc(List<(Vec3 Position, float Curvature)> raw, Vec3 center, float radius, float startAngleRad, float endAngleRad, int pointCount)
    {
        for (var i = 0; i < pointCount; i++)
        {
            var t = i / (float)pointCount;
            var angle = startAngleRad + (endAngleRad - startAngleRad) * t;
            var position = center + new Vec3(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);
            raw.Add((position, 1f / radius));
        }
    }

    private static TrackCenterline BuildClosedLoop(List<(Vec3 Position, float Curvature)> raw)
    {
        var points = new CenterlinePoint[raw.Count];
        var cumulativeLength = 0f;

        for (var i = 0; i < raw.Count; i++)
        {
            var current = raw[i].Position;
            var next = raw[(i + 1) % raw.Count].Position;
            var previous = raw[(i - 1 + raw.Count) % raw.Count].Position;

            // Central difference for a smoother tangent than a forward difference would give.
            var forward = (next - previous).Normalized();

            if (i > 0)
            {
                cumulativeLength += Vec3.Distance(raw[i - 1].Position, current);
            }

            points[i] = new CenterlinePoint(current, forward, cumulativeLength, raw[i].Curvature);
        }

        return new TrackCenterline(points, isClosedLoop: true);
    }
}
