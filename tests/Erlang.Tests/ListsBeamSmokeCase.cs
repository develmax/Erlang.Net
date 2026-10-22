using Erlang;

namespace Erlang.Tests;

internal sealed record ListsBeamSmokeCase(
    string Function,
    int Arity,
    bool NativePrimitive,
    Term[] Arguments,
    Term Expected
);
