-module(list_register_case_probe).
-export([select/3, check/2]).
-record(entry, {key, value}).

select([], _, _) ->
    empty;
select([Head | Tail], Wanted, Saved) ->
    case Head#entry.key of
        Wanted ->
            Value = read_value(Head),
            {found, Saved, Value};
        _ ->
            select(Tail, Wanted, Saved)
    end.

check([], _) ->
    empty;
check([Head | Tail], Wanted) ->
    case Head of
        Wanted -> {yes, Head};
        _ -> check(Tail, Wanted)
    end.

read_value(Entry) ->
    Entry#entry.value.
