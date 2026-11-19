-module(beam_checked_list_return).
-export([init/1]).

init([]) ->
    Flags = {ordered, 2, 7},
    First = erlang:put(checked_list_stage, first),
    Entries = [First, erlang:put(checked_list_stage, second)],
    {ok, {Flags, Entries}}.
