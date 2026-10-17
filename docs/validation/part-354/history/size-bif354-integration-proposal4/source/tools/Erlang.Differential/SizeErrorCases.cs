namespace Erlang.Differential;

public static class SizeErrorCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            SizeErrorCaseNames.SizeErrorTupleSize,
            SizeErrorCaseSources.SizeErrorTupleSize,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeImportTupleSize,
            SizeErrorCaseSources.SizeImportTupleSize,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeErrorMapSize,
            SizeErrorCaseSources.SizeErrorMapSize,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeImportMapSize,
            SizeErrorCaseSources.SizeImportMapSize,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeErrorBitSize,
            SizeErrorCaseSources.SizeErrorBitSize,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeImportBitSize,
            SizeErrorCaseSources.SizeImportBitSize,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeErrorByteSize,
            SizeErrorCaseSources.SizeErrorByteSize,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeImportByteSize,
            SizeErrorCaseSources.SizeImportByteSize,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeGuardMatchspec,
            SizeErrorCaseSources.SizeGuardMatchspec,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeValidValues,
            SizeErrorCaseSources.SizeValidValues,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeCustomTracePreservation,
            SizeErrorCaseSources.SizeCustomTracePreservation,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeDepthControls,
            SizeErrorCaseSources.SizeDepthControls,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.BacktraceDepthFlag,
            SizeErrorCaseSources.BacktraceDepthFlag,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.SizeDepthImportControls,
            SizeErrorCaseSources.SizeDepthImportControls,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.GenericDepthError,
            SizeErrorCaseSources.GenericDepthError,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        ),
        new(
            SizeErrorCaseNames.GenericDepthHd,
            SizeErrorCaseSources.GenericDepthHd,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))
        )
    ];
}
