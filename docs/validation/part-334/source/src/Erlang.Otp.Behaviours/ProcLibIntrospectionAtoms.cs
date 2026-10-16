namespace Erlang.Otp;

internal static class ProcLibIntrospectionAtoms
{
    public static readonly Atom ProcessLabel = Term.A(ProcLibIntrospectionNames.ProcessLabel);
    public static readonly Atom Dictionary = Term.A(ProcessInfoNames.Dictionary);
    public static readonly Atom False = Term.A(ErlangBooleanAtoms.False);
    public static readonly TupleTerm InitialCallQuery = Term.Tuple(Dictionary, ProcLibAtoms.InitialCall);
    public static readonly TupleTerm ProcessLabelQuery = Term.Tuple(Dictionary, ProcessLabel);
    public static readonly TupleTerm DefaultInitialCall = Term.Tuple(ProcLibAtoms.Module, ProcLibAtoms.Initializer, Term.I(ProcLibIntrospectionNames.DefaultInitializerArity));
}
