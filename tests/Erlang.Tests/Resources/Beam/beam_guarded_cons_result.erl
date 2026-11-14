-module(beam_guarded_cons_result).
-export([expand/1]).
expand([Head | Tail]) ->
    if is_atom(Head) -> [{Head, true} | expand(Tail)];
       true -> [Head | expand(Tail)]
    end;
expand([]) -> [].
