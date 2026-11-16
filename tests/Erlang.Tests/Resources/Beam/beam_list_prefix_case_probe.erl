-module(beam_list_prefix_case_probe).
-export([walk/2, lookup/1]).

walk([Head|Tail], Flags) ->
    Name = lookup(Head),
    case lists:member(Name, Flags) of
        true ->
            Reversed = lists:reverse(Tail),
            {Head,Tail,Name,Reversed};
        false ->
            {Name,Tail}
    end;
walk([], _) ->
    [].

lookup(Name) ->
    persistent_term:get(Name).
