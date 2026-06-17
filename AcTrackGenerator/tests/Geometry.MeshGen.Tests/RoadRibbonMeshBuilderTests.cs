using AcTrackGenerator.Common;
using AcTrackGenerator.Geometry.MeshGen;
using AcTrackGenerator.Geometry.TrackLayout;

namespace AcTrackGenerator.Geometry.MeshGen.Tests;

public class RoadRibbonMeshBuilderTests
{
    private static TrackCenterline BuildOvalCenterline() =>
        StadiumOvalLayoutGenerator.Generate(new StadiumOvalParameters());

    [Fact]
    public void Build_ProducesTwoVerticesAndUvsPerCenterlinePoint()
    {
        var centerline = BuildOvalCenterline();
        var mesh = RoadRibbonMeshBuilder.Build(centerline, new RoadRibbonParameters());

        Assert.Equal(centerline.Points.Count * 2, mesh.Vertices.Count);
        Assert.Equal(centerline.Points.Count * 2, mesh.Uvs.Count);
    }

    [Fact]
    public void Build_ProducesTwoTrianglesPerSegment_ForAClosedLoop()
    {
        var centerline = BuildOvalCenterline();
        Assert.True(centerline.IsClosedLoop);

        var mesh = RoadRibbonMeshBuilder.Build(centerline, new RoadRibbonParameters());

        // Closed loop: one segment per point (wrapping), 2 triangles (6 indices) per segment.
        Assert.Equal(centerline.Points.Count * 6, mesh.TriangleIndices.Count);
    }

    [Fact]
    public void Build_EdgeVerticesAreHalfRoadWidthFromTheCenterline()
    {
        var centerline = BuildOvalCenterline();
        var parameters = new RoadRibbonParameters(RoadWidth: 10f);
        var mesh = RoadRibbonMeshBuilder.Build(centerline, parameters);

        for (var i = 0; i < centerline.Points.Count; i++)
        {
            var center = centerline.Points[i].Position;
            var left = mesh.Vertices[i * 2];
            var right = mesh.Vertices[i * 2 + 1];

            Assert.Equal(parameters.RoadWidth / 2f, Vec3.Distance(center, left), precision: 4);
            Assert.Equal(parameters.RoadWidth / 2f, Vec3.Distance(center, right), precision: 4);
        }
    }

    [Fact]
    public void Build_EveryTriangleWindsWithANonDegenerateUpwardNormal()
    {
        var centerline = BuildOvalCenterline();
        var mesh = RoadRibbonMeshBuilder.Build(centerline, new RoadRibbonParameters());

        for (var i = 0; i < mesh.TriangleIndices.Count; i += 3)
        {
            var v0 = mesh.Vertices[mesh.TriangleIndices[i]];
            var v1 = mesh.Vertices[mesh.TriangleIndices[i + 1]];
            var v2 = mesh.Vertices[mesh.TriangleIndices[i + 2]];

            var normal = Vec3.Cross(v1 - v0, v2 - v0);

            // Not degenerate: the three vertices aren't collinear.
            Assert.True(normal.Length() > 1e-6f);
            // Wound so the face normal points along AC's +Y (up).
            Assert.True(normal.Y > 0f);
        }
    }
}
