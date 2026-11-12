-module(list_case_arithmetic_probe).
-export([encode/3, select/3, swapped/3, add_heads/2]).

encode([Head | Tail], Seen, Count) ->
    case Head of
        $} -> [92, Head | encode(Tail, Seen, Count + 1)];
        _ -> [Head | encode(Tail, Seen, Count + 1)]
    end;
encode([], _, _) ->
    [].

select([Head | Tail], Wanted, Count) ->
    case Head of
        Wanted -> Count;
        _ -> select(Tail, Wanted, Count + 1)
    end;
select([], _, Count) ->
    Count.

swapped([Head | Tail], Left, Right) ->
    case Head of
        first -> [Right | swapped(Tail, Right + 1, Left)];
        _ -> [Left | swapped(Tail, Right + 1, Left)]
    end;
swapped([], _, _) ->
    [].

add_heads([Head | Tail], Sum) ->
    case Head of
        stop -> Sum;
        _ -> add_heads(Tail, Sum + Head)
    end;
add_heads([], Sum) ->
    Sum.
