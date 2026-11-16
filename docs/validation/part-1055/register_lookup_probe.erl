-module(register_lookup_probe).
-export([lookup/3]).
lookup(Table, Key, Extra) ->
    case ets:lookup(Table, Key) of
        [{Key, Value}] -> {ok, Value};
        [] -> fallback(Key, missing, Extra)
    end.
fallback(Key, Reason, Extra) -> {Key, Reason, Extra}.
