-module(local_capture_probe).
-export([run/1]).
run(Input) ->
    Zed = erlang:integer_to_binary(Input),
    Alpha = erlang:integer_to_binary(Input + 1),
    lists:map(fun(X) -> relay(X, Alpha, Zed) end, [sample]).
relay(X, Alpha, Zed) -> {X, Alpha, Zed}.
