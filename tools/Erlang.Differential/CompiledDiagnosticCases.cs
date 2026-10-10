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
        new("begin-unbound-variable", "-module(scope_begin). -export([run/0]). run()->begin U end.","U"),
        new("tuple-sibling-forward", "-module(scope_tuple_forward). -export([run/0]). run()->{X=1,X}.","X"),
        new("tuple-sibling-backward", "-module(scope_tuple_backward). -export([run/0]). run()->{X,X=1}.","X"),
        new("call-sibling-forward", "-module(scope_call_forward). -export([run/0]). run()->erlang:'+'(X=1,X).","X"),
        new("call-sibling-backward", "-module(scope_call_backward). -export([run/0]). run()->erlang:'+'(X,X=1).","X"),
        new("apply-sibling-forward", "-module(scope_apply_forward). -export([run/0]). run()->F=fun(A,B)->A+B end,F(X=1,X).","X"),
        new("tuple-nested-sibling", "-module(scope_tuple_nested). -export([run/0]). run()->{X=1,{X}}.","X"),
        new("list-sibling-forward", "-module(scope_list_forward). -export([run/0]). run()->[X=1,X].","X"),
        new("list-head-not-in-tail", "-module(scope_list_tail). -export([run/0]). run()->[X=1|X].","X"),
        new("map-key-not-visible-in-value", "-module(scope_map_key). -export([run/0]). run()->#{(X=a)=>X}.","X"),
        new("map-value-not-visible-in-key", "-module(scope_map_value). -export([run/0]). run()->#{X=>(X=a)}.","X"),
        new("map-field-not-visible-in-next", "-module(scope_map_next). -export([run/0]). run()->#{a=>(X=1),b=>X}.","X"),
        new("map-base-not-visible-in-field", "-module(scope_map_base). -export([run/0]). run()->(X=#{})#{a=>X}.","X"),
        new("map-field-not-visible-in-base", "-module(scope_map_back). -export([run/0]). run()->X#{a=>(X=#{})}.","X"),
        new("map-key-not-visible-in-next-key", "-module(scope_map_keys). -export([run/0]). run()->#{(X=a)=>1,X=>2}.","X"),
        new("bits-value-not-visible-in-own-size", "-module(scope_bits_size). -export([run/0]). run()-><<(X=1):X>>.","X"),
        new("bits-value-not-visible-in-next-value", "-module(scope_bits_value). -export([run/0]). run()-><<(X=1),X>>.","X"),
        new("bits-size-not-visible-in-next-value", "-module(scope_bits_next). -export([run/0]). run()-><<1:(S=4),S>>.","S"),
        new("bits-later-value-not-visible-in-size", "-module(scope_bits_later). -export([run/0]). run()-><<1:X,(X=1)>>.","X")
        , new(
            "try-body-export-unsafe",
            "-module(scope_try_export). -export([run/0]). run()->try X=1 after ok end,X.",
            "X",
            "try"
        )
        , new(
            "try-of-export-unsafe",
            "-module(scope_try_of). -export([run/0]). run()->try 1 of X -> X after ok end,X.",
            "X",
            "try"
        )
        , new(
            "try-handler-export-unsafe",
            "-module(scope_try_handler). -export([run/0]). run()->try throw(1) catch X -> X end,X.",
            "X",
            "try"
        )
        , new(
            "try-after-export-unsafe",
            "-module(scope_try_after). -export([run/0]). run()->try ok after X=1 end,X.",
            "X",
            "try"
        )
        , new(
            "try-body-not-visible-in-handler",
            "-module(scope_try_catch). -export([run/0]). run()->try X=1 catch _ -> X end.",
            "X",
            "try"
        )
        , new(
            "try-body-not-visible-in-after",
            "-module(scope_try_after_body). -export([run/0]). run()->try X=1 after X end.",
            "X",
            "try"
        )
        , new(
            "stacktrace-direct-catch-guard",
            "-module(scope_stack_guard). -export([run/0]). run()->try error(reason) catch error:R:S when is_list(S) -> R end.",
            "S",
            Kind:DiagnosticCaseKind.StacktraceGuard
        )
        , new(
            "stacktrace-prebound-variable",
            "-module(scope_stack_bound). -export([run/0]). run()->S=[],try error(reason) catch error:R:S -> R end.",
            "S",
            Kind:DiagnosticCaseKind.StacktraceBound
        )
        , new(
            "stacktrace-reason-variable-reused",
            "-module(scope_stack_reason). -export([run/0]). run()->try throw(reason) catch C:S:S -> C end.",
            "S",
            Kind:DiagnosticCaseKind.StacktraceBound
        )
        , .. MaybeDiagnosticCases.All, .. AliasDiagnosticCases.All, .. ListComprehensionDiagnosticCases.All, .. BinaryComprehensionDiagnosticCases.All
    ];
}
