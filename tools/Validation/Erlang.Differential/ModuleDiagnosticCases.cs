namespace Erlang.Differential;

public static class ModuleDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new(
            "module-non-latin1-greek",
            "-module('λ'). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.NonLatin1Module
        ),
        new(
            "module-non-latin1-supplementary",
            "-module('😀'). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.NonLatin1Module
        ),
        new(
            "module-non-latin1-boundary",
            "-module('Ā'). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.NonLatin1Module
        ),
        new(
            "module-empty",
            "-module(''). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.EmptyModule
        ),
        new(
            "module-blank-space",
            "-module(' '). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.BlankModule
        ),
        new(
            "module-blank-latin1",
            "-module(' ­ '). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.BlankModule
        ),
        new(
            "module-control-newline",
            "-module('m\\n'). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.ControlModule
        ),
        new(
            "module-control-del",
            "-module('m\u007f'). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.ControlModule
        ),
        new(
            "module-control-c1",
            "-module('m\u0080'). -export([run/0]). run()->ok.",
            "",
            Kind: DiagnosticCaseKind.ControlModule
        )
    ];
}
