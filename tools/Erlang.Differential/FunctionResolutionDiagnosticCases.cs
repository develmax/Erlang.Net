namespace Erlang.Differential;

public static class FunctionResolutionDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } =
    [
        new(
            "function-resolution-aborted-loaded-source",
            "-module(module_loading_contract). -export([run/0]). run()->false=module_loaded(module_loading_not_present),L=loaded(),false=lists:member(module_loading_not_present,L),false=module_loaded(module_loading_not_present),done.",
            "loaded",
            "0",
            DiagnosticCaseKind.UndefinedFunction
        ),
        Reject(
            "nested-loaded",
            "F=fun()->loaded() end,F()",
            "loaded",
            0
        ),
        Reject(
            "iterator-not-auto-imported",
            "processes_iterator()",
            "processes_iterator",
            0
        ),
        Reject(
            "next-not-auto-imported",
            "processes_next({0,[]})",
            "processes_next",
            1
        ),
        Reject(
            "loaded-wrong-arity",
            "loaded(1)",
            "loaded",
            1
        ),
        Reject(
            "unknown-local",
            "qzx_unknown()",
            "qzx_unknown",
            0
        ),
        Reject(
            "wrong-arity-bif",
            "self(1)",
            "self",
            1
        )
    ];

    private static CompiledDiagnosticCase Reject(
        string name,
        string body,
        string function,
        int arity
    ) => new(
        "function-resolution-" + name,
        "-module(function_resolution_rejection). -export([run/0]). run()->" + body + ".",
        function,
        arity.ToString(System.Globalization.CultureInfo.InvariantCulture),
        DiagnosticCaseKind.UndefinedFunction
    );
}
