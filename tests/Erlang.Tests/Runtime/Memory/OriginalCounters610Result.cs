namespace Erlang.Tests;

internal sealed record OriginalCounters610Result(
    bool Passed,
    OriginalCounters610Profile? OriginalProfile,
    OriginalCounters610Profile? ManagedProfile,
    IReadOnlyDictionary<string, OriginalAtomics590Role>? OriginalRoles,
    IReadOnlyDictionary<string, OriginalAtomics590Role>? ManagedRoles,
    IReadOnlyList<string>? OccurrenceRoles,
    string? Failure
);
