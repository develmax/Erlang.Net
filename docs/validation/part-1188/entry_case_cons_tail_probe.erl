-module(entry_case_cons_tail_probe).
-export([read/1]).
read(Value) ->
    case check(Value) of
        true -> ok;
        _ -> Previous = read_tail(entry_fallback_key),
             finish(entry_fallback_key, [Value|Previous])
    end.
check(ok) -> true;
check(_) -> false.
read_tail(Key) -> erlang:get(Key).
finish(Key, Value) -> {Key, Value}.
