-module(beam_case_projected_second).
-export([fetch/1]).

fetch(Key) ->
    case application:get_env(kernel, Key) of
        {ok, Value} -> result(Key, Value, [], 0);
        undefined -> missing
    end.

result(Key, Value, A, B) -> {Key, Value, A, B}.
