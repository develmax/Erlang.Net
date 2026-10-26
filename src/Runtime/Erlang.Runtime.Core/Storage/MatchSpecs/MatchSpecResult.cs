namespace Erlang;

public sealed record MatchSpecResult(
    bool Matched,
    Term Value,
    bool ReturnTrace,
    bool ExceptionTrace
);
