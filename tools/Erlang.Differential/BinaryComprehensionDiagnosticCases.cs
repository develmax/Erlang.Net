namespace Erlang.Differential;

public static class BinaryComprehensionDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new("bg-generator-not-exported", "-module(scope_bg_export). -export([run/0]). run()->[X || <<X>> <= <<1>>],X.", "X"),
        new(
            "bg-source-binding-private",
            "-module(scope_bg_source). -export([run/0]). run()->[B || <<X>> <= begin B= <<1>>,B end].",
            "B"
        ),
        new("bc-body-binding-private", "-module(scope_bc_body). -export([run/0]). run()-><< <<(Y=X)>> || X <- [1] >>,Y.", "Y"),
        new("bg-source-unbound", "-module(scope_bg_unbound). -export([run/0]). run()->[X || <<X>> <= Missing].", "Missing")
    ];
}
