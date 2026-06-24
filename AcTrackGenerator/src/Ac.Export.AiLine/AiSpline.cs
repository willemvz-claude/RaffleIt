using AcTrackGenerator.Common;

namespace AcTrackGenerator.Ac.Export.AiLine;

/// <summary>
/// Reads and writes Assetto Corsa's binary .ai spline format (used for both
/// fast_lane.ai and pit_lane.ai), version 7. The layout below was taken from
/// the reference implementation in Content Manager's AcTools library
/// (AcTools/AiFile/AiSpline.cs, AiPoint.cs, AiPointExtra.cs, Ms-PL) — there is
/// no official Kunos documentation for this format.
///
/// File layout (all little-endian):
///   int32   version            (must be 7)
///   int32   pointCount (N)
///   int32   lapTime             (informational; AC recalculates lap time itself)
///   int32   sampleCount         (informational, usually 0)
///   N  x AiPoint:    float x, float y, float z, float length, int32 id
///   int32   extraCount (M, == N for every file seen in practice)
///   M  x AiPointExtra: float speed, gas, brake, obsoleteLatG, radius, sideLeft,
///                       sideRight, camber, direction, float normalX/Y/Z, float length,
///                       float forwardX/Y/Z, float tag, float grade
///   int32   hasGrid             (0 or 1)
///   [grid]  only present when hasGrid != 0 - a spatial lookup acceleration
///            structure AC can apparently regenerate; every spline this
///            project writes omits it (hasGrid = 0).
/// </summary>
public sealed class AiSpline
{
    public const int FormatVersion = 7;

    public int LapTime { get; init; }
    public int SampleCount { get; init; }
    public required IReadOnlyList<AiSplinePoint> Points { get; init; }
    public required IReadOnlyList<AiSplinePointExtra> PointsExtra { get; init; }

    public void WriteTo(Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write(FormatVersion);
        writer.Write(Points.Count);
        writer.Write(LapTime);
        writer.Write(SampleCount);

        foreach (var point in Points)
        {
            WriteVec3(writer, point.Position);
            writer.Write(point.Length);
            writer.Write(point.Id);
        }

        writer.Write(PointsExtra.Count);
        foreach (var extra in PointsExtra)
        {
            writer.Write(extra.Speed);
            writer.Write(extra.Gas);
            writer.Write(extra.Brake);
            writer.Write(extra.ObsoleteLatG);
            writer.Write(extra.Radius);
            writer.Write(extra.SideLeft);
            writer.Write(extra.SideRight);
            writer.Write(extra.Camber);
            writer.Write(extra.Direction);
            WriteVec3(writer, extra.Normal);
            writer.Write(extra.Length);
            WriteVec3(writer, extra.ForwardVector);
            writer.Write(extra.Tag);
            writer.Write(extra.Grade);
        }

        // hasGrid = 0: no spatial lookup grid written.
        writer.Write(0);
    }

    public static AiSpline ReadFrom(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        var version = reader.ReadInt32();
        if (version != FormatVersion)
        {
            throw new InvalidDataException($"Unsupported .ai file version {version}, expected {FormatVersion}.");
        }

        var pointCount = reader.ReadInt32();
        var lapTime = reader.ReadInt32();
        var sampleCount = reader.ReadInt32();

        var points = new AiSplinePoint[pointCount];
        for (var i = 0; i < pointCount; i++)
        {
            var position = ReadVec3(reader);
            var length = reader.ReadSingle();
            var id = reader.ReadInt32();
            points[i] = new AiSplinePoint(position, length, id);
        }

        var extraCount = reader.ReadInt32();
        var extras = new AiSplinePointExtra[extraCount];
        for (var i = 0; i < extraCount; i++)
        {
            var speed = reader.ReadSingle();
            var gas = reader.ReadSingle();
            var brake = reader.ReadSingle();
            var obsoleteLatG = reader.ReadSingle();
            var radius = reader.ReadSingle();
            var sideLeft = reader.ReadSingle();
            var sideRight = reader.ReadSingle();
            var camber = reader.ReadSingle();
            var direction = reader.ReadSingle();
            var normal = ReadVec3(reader);
            var length = reader.ReadSingle();
            var forwardVector = ReadVec3(reader);
            var tag = reader.ReadSingle();
            var grade = reader.ReadSingle();
            extras[i] = new AiSplinePointExtra(speed, gas, brake, obsoleteLatG, radius, sideLeft, sideRight,
                camber, direction, normal, length, forwardVector, tag, grade);
        }

        // hasGrid follows; this reader intentionally does not parse the optional grid structure
        // since nothing this project writes ever sets it.

        return new AiSpline
        {
            LapTime = lapTime,
            SampleCount = sampleCount,
            Points = points,
            PointsExtra = extras,
        };
    }

    private static void WriteVec3(BinaryWriter writer, Vec3 v)
    {
        writer.Write(v.X);
        writer.Write(v.Y);
        writer.Write(v.Z);
    }

    private static Vec3 ReadVec3(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
}
