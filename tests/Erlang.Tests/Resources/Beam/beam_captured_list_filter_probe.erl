-module(beam_captured_list_filter_probe).
-export([check/2]).

check(Context, Values) ->
    Set0 = sofs:relation(Values),
    Set1 = sofs:relation_to_family(Set0),
    Set = sofs:to_external(Set1),
    case [duplicates(Context, Key, Dup) || {Key,[_,_|_]=Dup} <- Set] of
        [] -> ok;
        [_|_]=Errors -> {error,lists:append(Errors)}
    end.

duplicates(_Context, _Key, Values) ->
    Values.

