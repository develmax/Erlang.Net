namespace Erlang;

internal sealed record BeamStructure(
    IReadOnlyList<BeamChunk> Chunks,
    IReadOnlyList<string?> Atoms,
    IReadOnlyList<BeamImport> Imports,
    IReadOnlyList<BeamExport> Exports,
    BeamCodeHeader Code,
    IReadOnlyList<Term> Literals,
    int BytesOutsideForm
);
