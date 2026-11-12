-module(entry_try_token_probe).
-export([f/1, body/1]).
body(error) -> erlang:error(expected);
body(Value) -> erlang:garbage_collect(), {ok, Value}.
f(Value) ->
    try body(Value)
    catch error:expected:_ -> matched
    end.
