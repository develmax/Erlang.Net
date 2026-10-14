using System.Globalization;

namespace Erlang.Differential;

public static class BasedIntegerDiagnosticExpectations
{
    public const string Code = "ERL001";
    public const string IllegalInteger = "Illegal integer literal";

    public static string Message(CompiledDiagnosticCase fixture) => fixture.Variable == "integer" ? IllegalInteger : "Invalid integer base " + fixture.Variable;

    public static Term Outcome(CompiledDiagnosticCase fixture) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(fixture.Variable == "integer" ? Term.Tuple(Term.A("illegal"), Term.A("integer")) : Term.Tuple(Term.A("base"), Term.I(int.Parse(fixture.Variable, CultureInfo.InvariantCulture))))
    );
}
