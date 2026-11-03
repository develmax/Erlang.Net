namespace Erlang.Tests;

internal sealed record OriginalCounters610Observation(
    OriginalCounters610Profile Profile,
    IReadOnlyDictionary<string, ReferenceTerm> Roles
);
