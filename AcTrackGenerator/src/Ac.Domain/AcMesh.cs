using AcTrackGenerator.Common;

namespace AcTrackGenerator.Ac.Domain;

/// <summary>
/// A single renderable mesh destined for one KN5 node. Coordinates are in AC
/// world space (Y-up, meters); the Blender Z-up swizzle is applied only when
/// the scene is materialized into a Blender build script.
/// </summary>
public sealed class AcMesh
{
    /// <summary>
    /// KN5 node / Blender object name. Also drives AC's built-in physical
    /// surface matching (e.g. a name like "1ROAD" gets the default high-grip
    /// asphalt surface with no surfaces.ini needed).
    /// </summary>
    public required string Name { get; init; }

    public required string MaterialName { get; init; }
    public required IReadOnlyList<Vec3> Vertices { get; init; }
    public required IReadOnlyList<Vec2> Uvs { get; init; }

    /// <summary>Three indices per triangle, wound so cross(v1-v0, v2-v0) points along the intended face normal.</summary>
    public required IReadOnlyList<int> TriangleIndices { get; init; }
}
