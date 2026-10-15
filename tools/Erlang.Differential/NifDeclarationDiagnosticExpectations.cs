namespace Erlang.Differential;

public static class NifDeclarationDiagnosticExpectations
{
    public const string Code = "ERL005";
    private const string UndefinedTag = "undefined_nif";
    private const string InvalidTag = "invalid_fa_attribute";
    private const string InvalidMessage = "Badly formed nifs function attribute";

    public static string Message(CompiledDiagnosticCase fixture) => fixture.Kind == DiagnosticCaseKind.NifUndefined
        ? $"Undefined nif {fixture.Variable}/{fixture.UnsafeConstruct}"
        : InvalidMessage;

    public static Term Outcome(CompiledDiagnosticCase fixture)
    {
        Term reason = fixture.Kind == DiagnosticCaseKind.NifUndefined
            ? Term.Tuple(
                Term.A(UndefinedTag),
                Term.Tuple(Term.A(fixture.Variable), Term.I(int.Parse(fixture.UnsafeConstruct!, System.Globalization.CultureInfo.InvariantCulture)))
            )
            : Term.Tuple(Term.A(InvalidTag), Term.I(int.Parse(fixture.Variable, System.Globalization.CultureInfo.InvariantCulture)));

        return Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(reason));
    }
}
