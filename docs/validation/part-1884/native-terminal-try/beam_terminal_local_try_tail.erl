-module(beam_terminal_local_try_tail).
-export([run/1, projected/1, wrapped/1, cleanup/1, remote/1]).
run(T) ->
    try probe(T) of
        undefined -> fallback(T);
        Value -> Value
    catch _:_ -> fallback(T)
    end.
projected(T) ->
    try probe(element(2,T)) of
        undefined -> fallback(T);
        Value -> Value
    catch _:_ -> fallback(T)
    end.
wrapped(T) ->
    {wrapped, try probe(T) of
        undefined -> fallback(T);
        Value -> Value
    catch _:_ -> fallback(T)
    end}.
cleanup(T) ->
    try probe(T) of
        undefined -> fallback(T);
        Value -> Value
    catch _:_ -> fallback(T)
    after erlang:put(cleaned,true)
    end.
remote(T) ->
    try probe(T) of
        undefined -> lists:reverse(T);
        Value -> Value
    catch _:_ -> lists:reverse(T)
    end.
probe(throwme) -> erlang:error(bad_probe);
probe(fallback_error) -> undefined;
probe(catch_fallback_error) -> erlang:error(bad_probe);
probe(T) -> T.
fallback(fallback_error) -> erlang:error(unhandled_fallback);
fallback(catch_fallback_error) -> erlang:error(unhandled_fallback);
fallback(T) -> {fallback,T}.
