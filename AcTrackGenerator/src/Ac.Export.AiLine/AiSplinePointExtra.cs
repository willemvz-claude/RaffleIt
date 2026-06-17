using AcTrackGenerator.Common;

namespace AcTrackGenerator.Ac.Export.AiLine;

/// <summary>Mirrors AcTools' AiPointExtra struct (the "driving metadata" entry in a .ai file's second array).</summary>
public readonly record struct AiSplinePointExtra(
    float Speed,
    float Gas,
    float Brake,
    float ObsoleteLatG,
    float Radius,
    float SideLeft,
    float SideRight,
    float Camber,
    float Direction,
    Vec3 Normal,
    float Length,
    Vec3 ForwardVector,
    float Tag,
    float Grade);
