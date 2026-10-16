-module(apply1023_improper_badarg).
-export([run/0]).
run() ->
    Args = args(1023, improper),
    try erlang:apply(missing_apply_boundary_target, missing_export, Args) of
        Value -> {unexpected_return, Value}
    catch
        error:badarg -> ok;
        Class:Reason -> {unexpected_exception, Class, Reason}
    end.
args(0, Tail) -> Tail;
args(N, Tail) -> args(N - 1, [item | Tail]).
