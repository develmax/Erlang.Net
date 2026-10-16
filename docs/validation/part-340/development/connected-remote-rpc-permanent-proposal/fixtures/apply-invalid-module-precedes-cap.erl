-module(apply_invalid_module_precedes_cap).
-export([run/0]).
run() ->
    Args = args(1024, []),
    try erlang:apply(42, missing_export, Args) of
        Value -> {unexpected_return, Value}
    catch
        error:badarg -> ok;
        Class:Reason -> {unexpected_exception, Class, Reason}
    end.
args(0, Tail) -> Tail;
args(N, Tail) -> args(N - 1, [item | Tail]).
