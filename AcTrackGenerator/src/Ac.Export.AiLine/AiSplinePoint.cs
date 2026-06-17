using AcTrackGenerator.Common;

namespace AcTrackGenerator.Ac.Export.AiLine;

/// <summary>Mirrors AcTools' AiPoint struct (the "core" entry in a .ai file's first array).</summary>
public readonly record struct AiSplinePoint(Vec3 Position, float Length, int Id);
