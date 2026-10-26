namespace Erlang.Differential;

public static class SourceCallBindingDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new(
            SourceCallBindingCaseNames.CompilerBindingImportModuleNameNonLatin1,
            SourceCallBindingCaseSources.ImportModuleNameNonLatin1,
            "",
            Kind: DiagnosticCaseKind.NonLatin1Module
        ),
        new(
            SourceCallBindingCaseNames.CompilerBindingImportModuleNameEmpty,
            SourceCallBindingCaseSources.ImportModuleNameEmpty,
            "",
            Kind: DiagnosticCaseKind.EmptyModule
        ),
        new(
            SourceCallBindingCaseNames.CompilerBindingImportModuleNameBlank,
            SourceCallBindingCaseSources.ImportModuleNameBlank,
            "",
            Kind: DiagnosticCaseKind.BlankModule
        ),
        new(
            SourceCallBindingCaseNames.CompilerBindingImportModuleNameControl,
            SourceCallBindingCaseSources.ImportModuleNameControl,
            "",
            Kind: DiagnosticCaseKind.ControlModule
        )
    ];
}
