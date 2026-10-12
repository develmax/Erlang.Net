namespace Erlang.Differential;

public static class RecordPredicateDiagnosticExpectations
{
    public const string GuardCode = "ERL007";
    public const string GuardMessage = "Illegal guard call 'is_record/2'";
    public const string ThreeGuardMessage = "Illegal guard call 'is_record/3'";
    public const string GuardNativeTag = "illegal_guard_expr";

    public static Term GuardOutcome => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.A(GuardNativeTag)));
}
