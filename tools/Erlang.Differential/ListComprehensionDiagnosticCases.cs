namespace Erlang.Differential;

public static class ListComprehensionDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new("lc-generator-not-exported", "-module(scope_lc_export). -export([run/0]). run()->[X || X <- [1]],X.", "X"),
        new(
            "lc-source-binding-not-visible",
            "-module(scope_lc_source). -export([run/0]). run()->[Y || X <- begin Y=[1],Y end].",
            "Y"
        ),
        new("lc-template-not-visible-in-filter", "-module(scope_lc_template). -export([run/0]). run()->[Y=X || X <- [1],Y>0].", "Y"),
        new(
            "lc-filter-binding-not-exported",
            "-module(scope_lc_filter). -export([run/0]). run()->[Y || X <- [1],begin Y=X,true end],Y.",
            "Y"
        ),
        new("lc-source-unbound", "-module(scope_lc_unbound). -export([run/0]). run()->[X || X <- Missing].", "Missing")
    ];
}
