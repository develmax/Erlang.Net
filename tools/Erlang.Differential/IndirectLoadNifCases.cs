namespace Erlang.Differential;

public static class IndirectLoadNifCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        Term rejection = Term.Tuple(
            Term.A(IndirectLoadNifCaseNames.Error),
            Term.Tuple(Term.A(IndirectLoadNifCaseNames.BadLibrary), Term.String(IndirectLoadNifCaseNames.Message))
        );

        void Add(
            string name,
            string body,
            Term? expected = null,
            bool funHelper = false
        )
        {
            string invocation = body.Replace(
                IndirectLoadNifCaseNames.ApplyInvocationPrefix,
                IndirectLoadNifCaseNames.Module + IndirectLoadNifCaseNames.QualifiedApplyInvocationPrefix
            );
            cases.Add(new(
                IndirectLoadNifCaseNames.Prefix + name,
                IndirectLoadNifCaseNames.ModuleDeclarationPrefix + IndirectLoadNifCaseNames.Module + IndirectLoadNifCaseNames.RunAndHelperExportPrefix
                    + IndirectLoadNifCaseNames.ApplyFunctionSignature + (funHelper ? IndirectLoadNifCaseNames.FunFunctionSignature : string.Empty) + IndirectLoadNifCaseNames.RunBodyPrefix + invocation
                    + IndirectLoadNifCaseNames.HelperSeparator + IndirectLoadNifCaseNames.ApplyHelper
                    + (funHelper ? IndirectLoadNifCaseNames.FunHelperSeparator + IndirectLoadNifCaseNames.FunHelper : string.Empty),
                Term.Tuple(Term.A(OracleOutcomeTags.Success), expected ?? rejection)
            ));
        }
        Add(IndirectLoadNifCaseNames.ApplyInvalidPathCase, IndirectLoadNifCaseNames.ApplyInvalidPathBody);
        Add(IndirectLoadNifCaseNames.ApplyMissingPathCase, IndirectLoadNifCaseNames.ApplyMissingPathBody);
        Add(IndirectLoadNifCaseNames.OpaqueArgumentsCase, IndirectLoadNifCaseNames.OpaqueArgumentsBody);
        Add(IndirectLoadNifCaseNames.ImproperPathCase, IndirectLoadNifCaseNames.ImproperPathBody);
        Term order = Term.Tuple(Term.A(IndirectLoadNifCaseNames.FirstEffectAtom), Term.A(IndirectLoadNifCaseNames.SecondEffectAtom));
        Add(IndirectLoadNifCaseNames.ApplyOrderCase, IndirectLoadNifCaseNames.ApplyOrderBody, expected: Term.Tuple(rejection, order));
        Add(
            IndirectLoadNifCaseNames.ApplyFailureCase,
            IndirectLoadNifCaseNames.ApplyFailureBody,
            expected: Term.Tuple(Term.A(IndirectLoadNifCaseNames.FailedBeforeBifAtom), order)
        );
        Add(IndirectLoadNifCaseNames.FunReferenceCase, IndirectLoadNifCaseNames.FunReferenceBody, funHelper: true);

        return cases;
    }
}
