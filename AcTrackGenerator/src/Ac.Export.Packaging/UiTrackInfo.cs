namespace AcTrackGenerator.Ac.Export.Packaging;

/// <summary>
/// The minimal set of <c>ui/ui_track.json</c> fields AC's track list reads.
/// There's no official schema; this mirrors what's commonly present in
/// shipped/community tracks. Purely informational metadata - none of it
/// affects whether the track loads or drives correctly.
/// </summary>
public sealed record UiTrackInfo(
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> GeoTags,
    string Country,
    string City,
    string LengthMeters,
    string WidthMeters,
    int Pitboxes,
    string Run);
