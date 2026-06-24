namespace AcTrackGenerator.Ac.Domain;

/// <summary>The full set of objects that need to end up in a track's KN5 file.</summary>
public sealed class TrackScene
{
    public required string TrackId { get; init; }
    public required IReadOnlyList<AcMesh> Meshes { get; init; }
    public required IReadOnlyList<AcMaterial> Materials { get; init; }
    public required IReadOnlyList<AcMarkerNode> MarkerNodes { get; init; }
}
