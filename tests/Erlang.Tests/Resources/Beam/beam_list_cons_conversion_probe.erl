-module(beam_list_cons_conversion_probe).
-export([scan/2, prefixed/1]).

scan([{skip,_}|Rest], Acc) ->
    scan(Rest, Acc);
scan([skip|Rest], Acc) ->
    scan(Rest, Acc);
scan([Value|Rest], Acc) when is_integer(Value) ->
    scan(Rest, ["value:"++integer_to_list(Value)|Acc]);
scan([Value|Rest], Acc) ->
    scan(Rest, [atom_to_list(Value)|Acc]);
scan([], []) ->
    [];
scan([], Acc) ->
    lists:reverse(Acc).

prefixed([_|_]) ->
    nonempty;
prefixed([]) ->
    case "/" ++ value() of
        "/discard" -> [];
        Result -> Result
    end.

value() ->
    persistent_term:get(prefix_probe).
