namespace Erlang;

internal sealed record EtsFileHeader(
    Term Major,
    Term Minor,
    EtsFileOptions Options,
    Term Digest,
    IReadOnlyList<Term> Fields,
    Term Cursor
);
