namespace Erlang.Differential;

public static class FunctionResolutionDiagnosticExpectations
{
    public const string Code = "ERL005";
    public const string NativeTag = "undefined_function";

    public static string Message(CompiledDiagnosticCase fixture) => $"Undefined function {fixture.Variable}/{fixture.UnsafeConstruct}";

    public static Term Outcome(CompiledDiagnosticCase fixture) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(Term.Tuple(
            Term.A(NativeTag),
            Term.Tuple(Term.A(fixture.Variable), Term.I(int.Parse(fixture.UnsafeConstruct!, System.Globalization.CultureInfo.InvariantCulture)))
        ))
    );
}
