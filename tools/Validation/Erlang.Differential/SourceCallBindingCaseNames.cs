namespace Erlang.Differential;

public static class SourceCallBindingCaseNames
{
    public const string Done = "done";
    public const string GuardDiagnosticCode = "ERL007";
    public const string FunctionDiagnosticCode = "ERL005";
    public const string ModuleDiagnosticCode = "ERL010";
    public const string MetadataRoundtrip = "compiler/binding/generated-metadata-roundtrip";
    public const string ImportAttribute = "import";
    public const string CompileAttribute = "compile";
    public const string BrokenIdentity = "Binding code identity not retained";
    public const string SourceCallBindingOrdinaryGuard = "source-call-binding-ordinary-guard";
    public const string SourceCallBindingQualifiedGuardLocalShadow = "source-call-binding-qualified-guard-local-shadow";
    public const string SourceCallBindingErlangReimportGuard = "source-call-binding-erlang-reimport-guard";
    public const string SourceCallBindingAllSuppressedQualified = "source-call-binding-all-suppressed-qualified";
    public const string SourceCallBindingAllSuppressedReimport = "source-call-binding-all-suppressed-reimport";
    public const string SourceCallBindingForeignImportQualifiedGuard = "source-call-binding-foreign-import-qualified-guard";
    public const string SourceCallBindingImportedCall = "source-call-binding-imported-call";
    public const string SourceCallBindingDuplicateWithinForm = "source-call-binding-duplicate-within-form";
    public const string SourceCallBindingLocalFilterIsExpression = "source-call-binding-local-filter-is-expression";
    public const string SourceCallBindingLocalFilterFailurePropagates = "source-call-binding-local-filter-failure-propagates";
    public const string SourceCallBindingQualifiedPatternKeysSizes = "source-call-binding-qualified-pattern-keys-sizes";
    public const string CompilerBindingGuardLocalGuardShadow = "compiler/binding/guard/local_guard_shadow";
    public const string CompilerBindingGuardLocalGuardShadowSuppressed = "compiler/binding/guard/local_guard_shadow_suppressed";
    public const string CompilerBindingGuardSuppressedGuard = "compiler/binding/guard/suppressed_guard";
    public const string CompilerBindingGuardSuppressedOrdinary = "compiler/binding/guard/suppressed_ordinary";
    public const string CompilerBindingGuardSuppressedNamedFun = "compiler/binding/guard/suppressed_named_fun";
    public const string CompilerBindingGuardForeignImportGuard = "compiler/binding/guard/foreign_import_guard";
    public const string CompilerBindingGuardImportedFunForbidden = "compiler/binding/guard/imported_fun_forbidden";
    public const string CompilerBindingGuardErlangImportedFunForbidden = "compiler/binding/guard/erlang_imported_fun_forbidden";
    public const string CompilerBindingGuardImportLocalConflict = "compiler/binding/guard/import_local_conflict";
    public const string CompilerBindingGuardDuplicateImport = "compiler/binding/guard/duplicate_import";
    public const string CompilerBindingGuardSuppressedFilter = "compiler/binding/guard/suppressed_filter";
    public const string CompilerBindingGuardSuppressedBitSize = "compiler/binding/guard/suppressed_bit_size";
    public const string CompilerBindingGuardSuppressedMapKey = "compiler/binding/guard/suppressed_map_key";
    public const string CompilerBindingImportModuleNameNonLatin1 = "compiler/binding/import-module-name/non_latin1";
    public const string CompilerBindingImportModuleNameEmpty = "compiler/binding/import-module-name/empty";
    public const string CompilerBindingImportModuleNameBlank = "compiler/binding/import-module-name/blank";
    public const string CompilerBindingImportModuleNameControl = "compiler/binding/import-module-name/control";
    public const string CompilerBindingImportModuleNameLatin1 = "compiler/binding/import-module-name/latin1";
}
