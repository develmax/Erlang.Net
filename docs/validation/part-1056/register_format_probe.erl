-module(register_format_probe).
-export([format/2, with_head/3, callback/3]).
format(F, A) ->
    io_lib:format("prefix " ++ F ++ "~n", A).
with_head(P, F, A) ->
    io_lib:format("prefix ~p " ++ F ++ "~n", [P|A]).
callback(A, B, C) ->
    remember(format_args, [A,B,C]),
    done.
remember(Key, Value) -> erlang:put(Key, Value).
