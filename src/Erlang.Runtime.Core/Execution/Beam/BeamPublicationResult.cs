namespace Erlang;

public sealed record BeamPublicationResult(BeamPublicationStatus Status, IReadOnlyList<string> Modules);
