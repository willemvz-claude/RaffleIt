using AcTrackGenerator.Common;

namespace AcTrackGenerator.Ac.Domain;

/// <summary>
/// A non-mesh "logical" node AC parses by name (grid slots, pit boxes, time
/// gates, ...). Exported as an empty Blender object so the KN5 node tree
/// carries it through unchanged, matching Kunos' own track convention (see
/// the ASSETTO_CORSA_OBJECTS regex list in the export add-on).
/// </summary>
public sealed class AcMarkerNode
{
    public required string Name { get; init; }
    public required Vec3 Position { get; init; }

    /// <summary>Heading around the world up axis, in radians, 0 = facing +X.</summary>
    public required float YawRadians { get; init; }
}
