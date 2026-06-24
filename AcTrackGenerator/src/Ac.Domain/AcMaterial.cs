namespace AcTrackGenerator.Ac.Domain;

/// <summary>
/// Maps directly onto the moppius/blender-assetto-corsa-tools material
/// settings (<c>material.assettoCorsa.*</c>). "ksPerPixel" with no explicit
/// shader properties is the addon's own default and is enough for a flat,
/// untextured surface.
/// </summary>
public sealed class AcMaterial
{
    public required string Name { get; init; }
    public string ShaderName { get; init; } = "ksPerPixel";
}
