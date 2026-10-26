namespace Erlang.Differential;

public static class AliasDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new(
            "alias-left-not-visible-in-right-key",
            "-module(scope_alias_key). -export([run/0]). run()->case #{} of K=#{K:=_} -> ok end.",
            "K"
        ),
        new(
            "alias-right-not-visible-in-left-key",
            "-module(scope_alias_back). -export([run/0]). run()->case #{} of #{K:=_}=K -> ok end.",
            "K"
        ),
        new(
            "alias-left-not-visible-in-right-size",
            "-module(scope_alias_size). -export([run/0]). run()->case <<>> of S = <<_:S>> -> ok end.",
            "S"
        ),
        new(
            "prefix-variable-not-literal",
            "-module(scope_prefix_variable). -export([run/0]). run()->P=[1],case [1,2] of P ++ _ -> ok end.",
            "",
            Kind:DiagnosticCaseKind.InvalidPattern
        ),
        new(
            "prefix-atom-not-integer",
            "-module(scope_prefix_atom). -export([run/0]). run()->case [tag] of [tag] ++ _ -> ok end.",
            "",
            Kind:DiagnosticCaseKind.InvalidPattern
        ),
        new(
            "prefix-improper-literal",
            "-module(scope_prefix_improper). -export([run/0]). run()->case [1] of [1|tail] ++ _ -> ok end.",
            "",
            Kind:DiagnosticCaseKind.InvalidPattern
        )
    ];
}
