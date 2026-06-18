namespace AcTrackGenerator.Osm.Ingestion;

public sealed record OsmExtract(IReadOnlyList<OsmNode> Nodes, IReadOnlyList<OsmWay> Ways);
