namespace Erlang.Differential;

/// <summary>Independent expected diagnostic contracts for variable scope fixtures.</summary>
public static class CompiledDiagnosticExpectations
{
    public const string VariableBindingCode = "ERL006";
    public const string UnboundVariableTag = "unbound_var";
    public const string UnsafeVariableTag = "unsafe_var";
    public const string ReferenceMode = "compile:forms/error reasons without annotations";
    public const string StacktraceFreshnessCode = "ERL006";
    public const string StacktraceGuardCode = "ERL007";
    public const string StacktraceBoundTag = "stacktrace_bound";
    public const string StacktraceGuardTag = "stacktrace_guard";
    public const string InvalidPatternTag = "illegal_pattern";
    public const string InvalidPatternCode = "ERL004";
    public const string InvalidPatternMessage = "Invalid or unsupported pattern";

    public static string Code(CompiledDiagnosticCase fixture) => fixture.Kind switch
    {
        DiagnosticCaseKind.VariableScope => VariableBindingCode,
        DiagnosticCaseKind.StacktraceBound => StacktraceFreshnessCode,
        DiagnosticCaseKind.StacktraceGuard => StacktraceGuardCode,
        DiagnosticCaseKind.InvalidPattern => InvalidPatternCode,
        _ => throw new NotSupportedException()
    };

    public static string Message(CompiledDiagnosticCase fixture) => fixture.Kind switch
    {
        DiagnosticCaseKind.VariableScope => Message(fixture.Variable),
        DiagnosticCaseKind.StacktraceBound => $"Stacktrace variable '{fixture.Variable}' must be fresh",
        DiagnosticCaseKind.StacktraceGuard => $"Stacktrace variable '{fixture.Variable}' is not legal in a guard",
        DiagnosticCaseKind.InvalidPattern => InvalidPatternMessage,
        _ => throw new NotSupportedException()
    };

    public static Term ReferenceOutcome(CompiledDiagnosticCase fixture) => fixture.Kind switch
    {
        DiagnosticCaseKind.VariableScope => ReferenceOutcome(fixture.Variable, fixture.UnsafeConstruct),
        DiagnosticCaseKind.StacktraceBound => StackOutcome(StacktraceBoundTag, fixture.Variable),
        DiagnosticCaseKind.StacktraceGuard => StackOutcome(StacktraceGuardTag, fixture.Variable),
        DiagnosticCaseKind.InvalidPattern => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.A(InvalidPatternTag))),
        _ => throw new NotSupportedException()
    };

    private static Term StackOutcome(string tag, string variable) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.Tuple(Term.A(tag), Term.A(variable))));

    public static string Message(string variable) => $"Unbound or unsafe variable '{variable}'";

    public static Term ReferenceOutcome(string variable, string? unsafeConstruct = null) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(unsafeConstruct is null
            ? Term.Tuple(Term.A(UnboundVariableTag), Term.A(variable))
            : Term.Tuple(Term.A(UnsafeVariableTag), Term.A(variable), Term.Tuple(Term.A(unsafeConstruct), Term.I(1))))
    );
}
