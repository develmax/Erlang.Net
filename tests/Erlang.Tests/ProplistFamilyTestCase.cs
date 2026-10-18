namespace Erlang.Tests;

internal sealed record ProplistFamilyTestCase(
    string Name,
    string Function,
    int Arity,
    string[] Arguments,
    string? Expected,
    string? Error,
    string Policy,
    string Branch
);

