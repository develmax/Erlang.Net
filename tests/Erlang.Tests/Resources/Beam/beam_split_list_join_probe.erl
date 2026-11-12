-module(beam_split_list_join_probe).
-export([upper/1, get_mem_info/0]).

get_mem_info() ->
    {ok, Info, Warning} = memory_view:read(),
    {[{"Memory Information", fields(Info)}], Info, Warning}.

fields([{Key, _} | Rest]) ->
    [{upper(atom_to_list(Key)), {bytes, Key}} | fields(Rest)];
fields([]) ->
    [].

upper(Key) ->
    lists:join(" ", [string:titlecase(Word) || Word <- string:split(Key, "_", all)]).
