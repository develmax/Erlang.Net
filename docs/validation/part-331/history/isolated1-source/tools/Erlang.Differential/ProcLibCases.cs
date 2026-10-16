namespace Erlang.Differential;

public static class ProcLibCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        Case(
            ProcLibCaseNames.MfaSpawn,
            ProcLibCaseSources.MfaSpawn,
            Term.Tuple(Boolean(true), Term.Tuple(Term.A(ProcLibCaseNames.FixtureModule), Term.A(ProcLibCaseNames.Child), Term.I(1)))
        ),
        Case(ProcLibCaseNames.MfaLink, ProcLibCaseSources.MfaLink, Boolean(true)),
        Case(ProcLibCaseNames.FunSpawn, ProcLibCaseSources.FunSpawn, Term.Tuple(Boolean(true), Boolean(true), Term.I(0))),
        Case(ProcLibCaseNames.FunLink, ProcLibCaseSources.FunLink, Boolean(true)),
        Case(ProcLibCaseNames.AckOne, ProcLibCaseSources.AckOne, Term.Tuple(Ok, Boolean(true))),
        Case(ProcLibCaseNames.AckTwo, ProcLibCaseSources.AckTwo, Term.Tuple(Ok, Boolean(true))),
        Case(ProcLibCaseNames.Registered, ProcLibCaseSources.Registered, Term.A(ProcLibCaseNames.Parent)),
        Case(ProcLibCaseNames.Inherited, ProcLibCaseSources.Inherited, Ok),
        Case(ProcLibCaseNames.InvalidAncestors, ProcLibCaseSources.InvalidAncestors, Ok),
        Case(ProcLibCaseNames.MissingAncestors, ProcLibCaseSources.MissingAncestors, Ok),
        Case(ProcLibCaseNames.EmptyAncestors, ProcLibCaseSources.EmptyAncestors, Ok),
        Case(ProcLibCaseNames.ImproperAncestors, ProcLibCaseSources.ImproperAncestors, Term.Tuple(Ok, Boolean(true))),
        Case(ProcLibCaseNames.InvalidFun, ProcLibCaseSources.InvalidFun, Ok),
        Case(ProcLibCaseNames.InvalidModule, ProcLibCaseSources.InvalidModule, Ok),
        Case(ProcLibCaseNames.InvalidArgs, ProcLibCaseSources.InvalidArgs, Ok),
        Case(ProcLibCaseNames.NonzeroFun, ProcLibCaseSources.NonzeroFun, Ok),
        Case(ProcLibCaseNames.MissingMfa, ProcLibCaseSources.MissingMfa, Ok),
        Case(ProcLibCaseNames.NativeInitialFun, ProcLibCaseSources.NativeInitialFun, Ok),
        Case(ProcLibCaseNames.NativeInitialMfa, ProcLibCaseSources.NativeInitialMfa, Ok),
        Case(ProcLibCaseNames.AtomAncestors, ProcLibCaseSources.AtomAncestors, Ok),
        Case(ProcLibCaseNames.AtomArgs, ProcLibCaseSources.AtomArgs, Ok)
    ];

    private static Atom Ok => Term.A(ProcLibCaseNames.Ok);

    private static Atom Boolean(bool value) => Term.A(value ? ProcLibCaseNames.True : ProcLibCaseNames.False);

    private static CompiledModuleCase Case(string name, string source, Term value) => new(name, source, Term.Tuple(Term.A(OracleOutcomeTags.Success), value));
}
