namespace Erlang.Differential;

/// <summary>Independent expected diagnostic contracts for variable scope fixtures.</summary>
public static class CompiledDiagnosticExpectations
{
    public const string VariableBindingCode = "ERL006";
    public const string UnboundVariableTag = "unbound_var";
    public const string UnsafeVariableTag = "unsafe_var";
    public const string ReferenceMode = "compile:forms/error reasons without annotations";

    public static string Message(string variable) => $"Unbound or unsafe variable '{variable}'";

    public static Term ReferenceOutcome(string variable, string? unsafeConstruct = null) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(unsafeConstruct is null
            ? Term.Tuple(Term.A(UnboundVariableTag), Term.A(variable))
            : Term.Tuple(Term.A(UnsafeVariableTag), Term.A(variable), Term.Tuple(Term.A(unsafeConstruct), Term.I(1))))
    );
}
