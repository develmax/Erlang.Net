namespace Erlang.Differential;

public static class MaybeDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new(
            "maybe-body-export-unsafe",
            "-module(scope_maybe_body). -export([run/0]). run()->maybe X=1 end,X.",
            "X",
            "maybe"
        ),
        new(
            "maybe-match-export-unsafe",
            "-module(scope_maybe_match). -export([run/0]). run()->maybe X ?= 1 end,X.",
            "X",
            "maybe"
        ),
        new(
            "maybe-rhs-export-unsafe",
            "-module(scope_maybe_rhs). -export([run/0]). run()->maybe _ ?= (X=1) end,X.",
            "X",
            "maybe"
        ),
        new(
            "maybe-body-not-visible-in-else",
            "-module(scope_maybe_else). -export([run/0]). run()->maybe X=1,ok ?= wrong else _ -> X end.",
            "X",
            "maybe"
        ),
        new(
            "maybe-body-not-visible-in-else-guard",
            "-module(scope_maybe_guard). -export([run/0]). run()->maybe X=1,ok ?= wrong else _ when X==1 -> yes end.",
            "X",
            "maybe"
        ),
        new(
            "maybe-else-export-unsafe",
            "-module(scope_maybe_export). -export([run/0]). run()->maybe ok ?= wrong else Y -> Y end,Y.",
            "Y",
            "else"
        ),
        new(
            "maybe-all-else-branches-export-unsafe",
            "-module(scope_maybe_branches). -export([run/0]). run()->maybe ok ?= wrong else wrong -> Y=1; _ -> Y=2 end,Y.",
            "Y",
            "else"
        ),
        new(
            "maybe-nested-export-unsafe",
            "-module(scope_maybe_nested). -export([run/0]). run()->maybe maybe X ?= 1 end,X end.",
            "X",
            "maybe"
        )
    ];
}
