using AcTrackGenerator.Ac.Domain;
using AcTrackGenerator.Common;
using AcTrackGenerator.Geometry.TrackLayout;

namespace AcTrackGenerator.Geometry.MeshGen;

public sealed record RoadRibbonParameters(float RoadWidth = 10f, string MeshName = "1ROAD", string MaterialName = "road_asphalt");

/// <summary>
/// Builds a flat quad-strip mesh straddling a centerline. This is the
/// simplest possible stand-in for the real road mesh generator that Phase 3
/// will build from OSM way geometry plus elevation samples; for Phase 0 it
/// only needs to produce something AC will render and treat as drivable
/// road (see <see cref="RoadRibbonParameters.MeshName"/>).
/// </summary>
public static class RoadRibbonMeshBuilder
{
    public static AcMesh Build(TrackCenterline centerline, RoadRibbonParameters parameters)
    {
        var halfWidth = parameters.RoadWidth / 2f;
        var pointCount = centerline.Points.Count;

        var vertices = new Vec3[pointCount * 2];
        var uvs = new Vec2[pointCount * 2];

        for (var i = 0; i < pointCount; i++)
        {
            var point = centerline.Points[i];
            var right = point.Right;

            // Even indices = left edge, odd indices = right edge, matching the
            // winding order chosen below.
            vertices[i * 2] = point.Position - right * halfWidth;
            vertices[i * 2 + 1] = point.Position + right * halfWidth;

            var v = point.CumulativeLength / parameters.RoadWidth;
            uvs[i * 2] = new Vec2(0f, v);
            uvs[i * 2 + 1] = new Vec2(1f, v);
        }

        var segmentCount = centerline.IsClosedLoop ? pointCount : pointCount - 1;
        var indices = new List<int>(segmentCount * 6);

        for (var i = 0; i < segmentCount; i++)
        {
            var next = centerline.NextIndex(i);

            var left0 = i * 2;
            var right0 = i * 2 + 1;
            var left1 = next * 2;
            var right1 = next * 2 + 1;

            // Wound so cross(v1-v0, v2-v0) points along +Y for a centerline running along +X
            // with Right = cross(Up, Forward) (see CenterlinePoint.Right and the derivation in
            // docs/phase0-spike.md). Verified against AC's "up means visible from above" convention.
            indices.Add(left0); indices.Add(left1); indices.Add(right1);
            indices.Add(left0); indices.Add(right1); indices.Add(right0);
        }

        return new AcMesh
        {
            Name = parameters.MeshName,
            MaterialName = parameters.MaterialName,
            Vertices = vertices,
            Uvs = uvs,
            TriangleIndices = indices,
        };
    }
}
