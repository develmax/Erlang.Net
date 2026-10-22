namespace Erlang;

public sealed record RuntimeRegexReport411(
    long CodesCreated,
    long CodesFreed,
    long MatchDataCreated,
    long MatchDataFreed,
    long Yields,
    int Active,
    int RetainedCodes,
    bool Closed,
    bool LibraryFreed
);
