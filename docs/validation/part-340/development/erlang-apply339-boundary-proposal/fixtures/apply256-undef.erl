-module(apply256_undef).
-export([run/0]).
run() ->
    Args = args(256, []),
    try erlang:apply(missing_apply_boundary_target, missing_export, Args) of
        Value -> {unexpected_return, Value}
    catch
        error:undef -> ok;
        Class:Reason -> {unexpected_exception, Class, Reason}
    end.
args(0, Tail) -> Tail;
args(N, Tail) -> args(N - 1, [item | Tail]).
