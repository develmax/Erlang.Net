namespace Erlang;

internal sealed record StringSourceStep(
    string Function,
    int Arity,
    int CallLine,
    int ErrorLine,
    bool CaseClauseOnError = false
);
