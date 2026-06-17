namespace AcTrackGenerator.Common;

/// <summary>
/// A position or direction in the canonical AC world coordinate space: Y-up,
/// meters, matching the axes Assetto Corsa itself uses at runtime (and the
/// axes a KN5 file's vertex data is in after export). All pipeline stages
/// from <c>Geometry.TrackLayout</c> onward operate in this space; the
/// Blender/Z-up swizzle is applied only at the very end, inside the
/// generated Blender build script.
/// </summary>
public readonly record struct Vec3(float X, float Y, float Z)
{
    public static readonly Vec3 Zero = new(0f, 0f, 0f);
    public static readonly Vec3 Up = new(0f, 1f, 0f);

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);

    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public Vec3 Normalized()
    {
        var length = Length();
        return length < 1e-9f ? Zero : new Vec3(X / length, Y / length, Z / length);
    }

    /// <summary>Cross product, treating both vectors as direction vectors (not positions).</summary>
    public static Vec3 Cross(Vec3 a, Vec3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public static float Distance(Vec3 a, Vec3 b) => (a - b).Length();
}
