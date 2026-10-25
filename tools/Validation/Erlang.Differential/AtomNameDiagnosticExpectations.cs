namespace Erlang.Differential;

public static class AtomNameDiagnosticExpectations
{
    public const string Code = "ERL001";
    public const string IllegalAtomMessage = "Illegal atom name";
    public const string IllegalVariableMessage = "Illegal variable name";
    public const string IllegalTag = "illegal";
    public const string AtomTag = "atom";
    public const string VariableTag = "var";

    public static string Message(CompiledDiagnosticCase fixture) => fixture.Kind == DiagnosticCaseKind.IllegalVariableName
        ? IllegalVariableMessage : IllegalAtomMessage;

    public static int Offset(CompiledDiagnosticCase fixture) => fixture.Source.IndexOf(fixture.Variable, StringComparison.Ordinal);

    public static Term Outcome(CompiledDiagnosticCase fixture) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(Term.Tuple(
            Term.A(IllegalTag),
            Term.A(fixture.Kind == DiagnosticCaseKind.IllegalVariableName ? VariableTag : AtomTag)
        ))
    );
}
