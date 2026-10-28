namespace Erlang.Tests;

internal sealed record OriginalAtomics590Result(
    bool Passed,
    int OriginalOccurrences,
    int ManagedOccurrences,
    OriginalAtomics590Role? OriginalRole,
    OriginalAtomics590Role? ManagedRole,
    string? Failure
);
