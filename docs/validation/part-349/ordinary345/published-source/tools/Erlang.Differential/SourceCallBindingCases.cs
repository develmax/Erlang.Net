namespace Erlang.Differential;

public static class SourceCallBindingCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            SourceCallBindingCaseNames.SourceCallBindingOrdinaryGuard,
            SourceCallBindingCaseSources.GuardOrdinaryGuard,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingQualifiedGuardLocalShadow,
            SourceCallBindingCaseSources.GuardQualifiedGuardLocalShadow,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingErlangReimportGuard,
            SourceCallBindingCaseSources.GuardErlangReimportGuard,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingAllSuppressedQualified,
            SourceCallBindingCaseSources.GuardAllSuppressedQualified,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingAllSuppressedReimport,
            SourceCallBindingCaseSources.GuardAllSuppressedReimport,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingForeignImportQualifiedGuard,
            SourceCallBindingCaseSources.GuardForeignImportQualifiedGuard,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingImportedCall,
            SourceCallBindingCaseSources.GuardImportedCall,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingDuplicateWithinForm,
            SourceCallBindingCaseSources.GuardDuplicateWithinForm,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingLocalFilterIsExpression,
            SourceCallBindingCaseSources.GuardLocalFilterIsExpression,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingLocalFilterFailurePropagates,
            SourceCallBindingCaseSources.GuardLocalFilterFailurePropagates,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        ),
        new(
            SourceCallBindingCaseNames.SourceCallBindingQualifiedPatternKeysSizes,
            SourceCallBindingCaseSources.GuardQualifiedPatternKeysSizes,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SourceCallBindingCaseNames.Done))
        )
    ];

    public static IReadOnlyList<(string Name, string Source, string? Diagnostic)> Controls { get; } = [
        (SourceCallBindingCaseNames.CompilerBindingGuardLocalGuardShadow, SourceCallBindingCaseSources.GuardLocalGuardShadow, SourceCallBindingCaseNames.GuardDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardLocalGuardShadowSuppressed, SourceCallBindingCaseSources.GuardLocalGuardShadowSuppressed, SourceCallBindingCaseNames.GuardDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardSuppressedGuard, SourceCallBindingCaseSources.GuardSuppressedGuard, SourceCallBindingCaseNames.GuardDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardSuppressedOrdinary, SourceCallBindingCaseSources.GuardSuppressedOrdinary, SourceCallBindingCaseNames.FunctionDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardSuppressedNamedFun, SourceCallBindingCaseSources.GuardSuppressedNamedFun, SourceCallBindingCaseNames.FunctionDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardForeignImportGuard, SourceCallBindingCaseSources.GuardForeignImportGuard, SourceCallBindingCaseNames.GuardDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardImportedFunForbidden, SourceCallBindingCaseSources.GuardImportedFunForbidden, SourceCallBindingCaseNames.FunctionDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardErlangImportedFunForbidden, SourceCallBindingCaseSources.GuardErlangImportedFunForbidden, SourceCallBindingCaseNames.FunctionDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardImportLocalConflict, SourceCallBindingCaseSources.GuardImportLocalConflict, SourceCallBindingCaseNames.FunctionDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardDuplicateImport, SourceCallBindingCaseSources.GuardDuplicateImport, SourceCallBindingCaseNames.FunctionDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardSuppressedFilter, SourceCallBindingCaseSources.GuardSuppressedFilter, SourceCallBindingCaseNames.GuardDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardSuppressedBitSize, SourceCallBindingCaseSources.GuardSuppressedBitSize, SourceCallBindingCaseNames.GuardDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingGuardSuppressedMapKey, SourceCallBindingCaseSources.GuardSuppressedMapKey, SourceCallBindingCaseNames.GuardDiagnosticCode),
        (SourceCallBindingCaseNames.CompilerBindingImportModuleNameLatin1, SourceCallBindingCaseSources.ImportModuleNameLatin1, null)
    ];
}
