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
        ),
        new("operator-left-binding-not-in-right", "-module(scope_op_right). -export([run/0]). run()->(X=1)+X.","X"),
        new("operator-right-binding-not-in-left", "-module(scope_op_left). -export([run/0]). run()->X+(X=1).","X"),
        new("operator-band-sibling-binding", "-module(scope_op_band). -export([run/0]). run()->(X=1) band X.","X"),
        new(
            "andalso-binding-unsafe",
            "-module(scope_andalso). -export([run/0]). run()->true andalso (X=1),X.",
            "X",
            "andalso"
        ),
        new(
            "orelse-binding-unsafe",
            "-module(scope_orelse). -export([run/0]). run()->false orelse (X=1),X.",
            "X",
            "orelse"
        ),
        new(
            "catch-binding-unsafe",
            "-module(scope_catch). -export([run/0]). run()->catch (X=1),X.",
            "X",
            "catch"
        ),
        new("begin-unbound-variable", "-module(scope_begin). -export([run/0]). run()->begin U end.","U")
    ];
}
