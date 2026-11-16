-module(cons_argument_probe).
-export([collect/3, accumulate/2, field/3, alias_match/2, find_index/3, plain/3]).

collect([Id | Tail] = Whole, Mode, Acc) when is_atom(Id) ->
    case Mode of
        keep -> {retry, Whole, Acc};
        _ -> collect(Tail, Mode, Acc)
    end;
collect([Head | Tail], Mode, Acc) ->
    collect(Tail, Mode, [{number, Head} | Acc]);
collect([], _, Acc) ->
    {done, lists:reverse(Acc)}.

accumulate([Head | Tail] = Whole, Acc) when is_atom(Head) ->
    accumulate(Tail, [{seen, Whole} | Acc]);
accumulate([], Acc) ->
    {done, lists:reverse(Acc)}.

field(Name, [{entry, Name} | _], N) ->
    N;
field(_, [{entry, Other} | _], _) ->
    Other;
field(_, [], _) ->
    empty.

alias_match(Wanted, [Head | Tail] = Wanted) ->
    {matched, Head, Tail};
alias_match(_, [_ | _]) ->
    different;
alias_match(_, []) ->
    empty.

find_index(Name, [{entry, Name} | _], N) ->
    N;
find_index(Name, [_Unused | Rest], N) ->
    find_index(Name, Rest, N + 1);
find_index(_, [], _) ->
    missing.

plain([Head | Tail], Value, Acc) ->
    plain(Tail, Value, [{Head, Value} | Acc]);
plain([], _, Acc) ->
    Acc.
