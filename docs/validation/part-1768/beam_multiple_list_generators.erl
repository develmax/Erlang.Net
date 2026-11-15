-module(beam_multiple_list_generators).
-export([both/2, branches/2]).

both(List, Key) ->
    {[pair(Key, X) || X <- List], [{right, V} || {tag, V} <- List]}.

branches(List, yes) ->
    [X || X <- List, is_atom(X)];
branches(List, _) ->
    [pair(fallback, X) || X <- List].

pair(Key, Value) -> {Key, Value}.