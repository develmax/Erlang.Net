namespace Erlang.Differential;

public static class FormBoundaryExpectations
{
    public const string Code = "ERL002";
    public const string Message = "Expected a form-terminating dot";
    public const string NativePrefix = "syntax error before: ";
    public const string NativeToken = "'.'";

    public static Term Outcome { get; } = Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(Term.List(Term.String(NativePrefix), Term.String(NativeToken)))
    );
}
