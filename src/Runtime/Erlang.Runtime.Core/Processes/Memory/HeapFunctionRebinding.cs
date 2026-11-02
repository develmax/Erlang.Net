namespace Erlang;

internal sealed record HeapFunctionRebinding(
    FunctionTerm Source,
    FunctionTerm Result,
    Term[] Environment,
    IReadOnlySet<Term> StableGlobals
);
