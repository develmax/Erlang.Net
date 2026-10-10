namespace Erlang.Differential;

/// <summary>Independent expected diagnostic contract for construction-scope fixtures.</summary>
public static class CompiledDiagnosticExpectations
{
    public const string VariableBindingCode = "ERL006";
    public const string UnboundVariableTag = "unbound_var";
    public const string ReferenceMode = "compile:forms/error reasons without annotations";

    public static string Message(string variable) => $"Unbound or unsafe variable '{variable}'";

    public static Term ReferenceOutcome(string variable) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(Term.Tuple(Term.A(UnboundVariableTag), Term.A(variable)))
    );
}
