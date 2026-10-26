namespace Erlang.Differential;

public static class NifDeclarationCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string attribute) => cases.Add(new(
            NifDeclarationCaseNames.Prefix + name,
            NifDeclarationCaseNames.ModulePrefix + attribute + NifDeclarationCaseNames.Body,
            Term.Tuple(
                Term.A(OracleOutcomeTags.Success),
                Term.Tuple(Term.A(NifDeclarationCaseNames.Kept), Nil.Value, Term.A(ErlangBooleanAtoms.False))
            )
        ));
        Add("absent-fallback", string.Empty);
        Add("empty-fallback", "-nifs([]). ");
        Add("private-fallback", "-nifs([fallback/0]). ");
        Add("tuple-form-fallback", "-nifs([{fallback,0}]). ");
        Add("duplicate-function-fallback", "-nifs([fallback/0,fallback/0]). ");
        Add("repeated-attribute-fallback", "-nifs([fallback/0]). -nifs([run/0,fallback/0]). ");
        Add("empty-then-nonempty-fallback", "-nifs([]). -nifs([fallback/0]). ");
        Add("nonempty-then-empty-fallback", "-nifs([fallback/0]). -nifs([]). ");

        return cases;
    }
}
