-module(entry_case_fallback_probe).
-export([read/2]).
read(Value, Other) ->
    case check(Value) of
        true -> ok;
        _ -> Previous = erlang:put(entry_fallback_key, Other),
             finish([Value|Previous])
    end.
check(ok) -> true;
check(_) -> false.
finish(Value) -> {done, Value}.
