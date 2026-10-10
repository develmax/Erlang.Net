namespace Erlang.Differential;

public static class CompiledDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new("bit-size-to-next-size", "-module(scope_size_size). -export([run/0]). run()->B = <<1:(S=4),2:S>>,{B,S}.", "S"),
        new("bit-value-to-own-size", "-module(scope_value_size). -export([run/0]). run()-><<(S=4):S>>.", "S"),
        new("bit-value-to-next-value", "-module(scope_value_value). -export([run/0]). run()-><<(V=1),V>>.", "V"),
        new("bit-size-to-next-value", "-module(scope_size_value). -export([run/0]). run()-><<1:(S=4),S>>.", "S"),
        new("bit-string-size-to-next-size", "-module(scope_string_size). -export([run/0]). run()-><<\"ab\":(S=8),42:S>>.", "S"),
        new("if-unbound-guard", "-module(scope_if_guard). -export([run/0]). run()->if U=:=1 -> ok; true -> no end.", "U"),
        new(
            "if-body-not-visible-in-other-guard",
            "-module(scope_if_guard_body). -export([run/0]). run()->if true -> X=1; X=:=1 -> no end.",
            "X"
        ),
        new(
            "if-body-not-visible-in-other-body",
            "-module(scope_if_bodies). -export([run/0]). run()->if true -> X=1; false -> X end.",
            "X"
        ),
        new(
            "if-partial-binding-unsafe",
            "-module(scope_if_unsafe). -export([run/0]). run()->if true -> X=1; false -> no end,X.",
            "X",
            "if"
        ),
        new(
            "if-nested-partial-binding-unsafe",
            "-module(scope_if_nested). -export([run/0]). run()->if true -> if true -> X=1; false -> no end; false -> X=2 end,X.",
            "X",
            "if"
        )
    ];
}
