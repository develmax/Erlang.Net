namespace Erlang.Differential;

public static class SystemAllocatorQueryCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    // Deliberate backend adaptation: this observation must remain outside Modules.
    // Original OTP returns libc; the SDK identifies its actual NativeMemory API.
    public static CompiledModuleCase FullInfoBackendObservation { get; } = new(
        SystemAllocatorQueryCaseNames.FullOptionsObservationName,
        SystemAllocatorQueryCaseNames.FullOptionsSource,
        Term.Tuple(
            Term.A(OracleOutcomeTags.Success),
            Term.Tuple(
                Term.A(SystemAllocatorQueryCaseNames.FullOptionsTag),
                Term.Tuple(Term.A(SystemAllocatorQueryCaseNames.Enabled), Term.A(SystemAllocatorQueryCaseNames.True)),
                Term.Tuple(Term.A(SystemAllocatorQueryCaseNames.Backend), Term.A(SystemAllocatorQueryCaseNames.SdkBackend)),
                Nil.Value,
                Term.Tuple(Term.A(SystemAllocatorQueryCaseNames.Enabled), Term.A(SystemAllocatorQueryCaseNames.True)),
                Term.Tuple(Term.A(SystemAllocatorQueryCaseNames.Backend), Term.A(SystemAllocatorQueryCaseNames.SdkBackend)),
                Nil.Value
            )
        )
    );

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        Term instance = Term.List(Term.Tuple(Term.A(SystemAllocatorQueryCaseNames.Instance), Term.I(SystemAllocatorQueryCaseNames.GlobalInstance), Nil.Value));

        void Add(
            string name,
            string body,
            Term expected
        )
        {
            cases.Add(new(
                SystemAllocatorQueryCaseNames.Prefix + name,
                SystemAllocatorQueryCaseNames.ModulePrefix + body + SystemAllocatorQueryCaseNames.ModuleSuffix,
                expected
            ));
        }

        Term Failure(string reason) => Term.Tuple(
            Term.A(OracleOutcomeTags.Failure),
            Term.A(SystemAllocatorQueryCaseNames.ErrorClass),
            Term.A(reason)
        );

        Add(
            SystemAllocatorQueryCaseNames.SizesAtomName,
            SystemAllocatorQueryCaseNames.SizesAtomBody,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), instance)
        );
        Add(
            SystemAllocatorQueryCaseNames.SizesListName,
            SystemAllocatorQueryCaseNames.SizesListBody,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.List(Term.Tuple(Term.A(SystemAllocatorQueryCaseNames.SystemAllocator), instance)))
        );
        Add(
            SystemAllocatorQueryCaseNames.InfoEmptyName,
            SystemAllocatorQueryCaseNames.InfoEmptyBody,
            Failure(SystemAllocatorQueryCaseNames.BadArgument)
        );
        Add(
            SystemAllocatorQueryCaseNames.SizesDuplicateName,
            SystemAllocatorQueryCaseNames.SizesDuplicateBody,
            Failure(SystemAllocatorQueryCaseNames.BadArgument)
        );
        Add(
            SystemAllocatorQueryCaseNames.SizesImproperName,
            SystemAllocatorQueryCaseNames.SizesImproperBody,
            Failure(SystemAllocatorQueryCaseNames.BadArgument)
        );
        Add(
            SystemAllocatorQueryCaseNames.InfoOuterName,
            SystemAllocatorQueryCaseNames.InfoOuterBody,
            Failure(SystemAllocatorQueryCaseNames.FunctionClause)
        );
        Add(
            SystemAllocatorQueryCaseNames.SizesUnknownLaterName,
            SystemAllocatorQueryCaseNames.SizesUnknownLaterBody,
            Failure(SystemAllocatorQueryCaseNames.BadArgument)
        );
        Add(
            SystemAllocatorQueryCaseNames.InfoUnknownName,
            SystemAllocatorQueryCaseNames.InfoUnknownBody,
            Failure(SystemAllocatorQueryCaseNames.BadArgument)
        );
        Add(
            SystemAllocatorQueryCaseNames.InfoNonatomName,
            SystemAllocatorQueryCaseNames.InfoNonatomBody,
            Failure(SystemAllocatorQueryCaseNames.BadArgument)
        );

        return cases;
    }
}
