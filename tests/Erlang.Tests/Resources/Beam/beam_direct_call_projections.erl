-module(beam_direct_call_projections).
-export([run/1, keep/1]).

run(Tuple = {tag, Left, Right}) ->
    Joined = join(Left, Right),
    {Tuple, Joined}.

keep({tag, Left, Right}) ->
    {kept, join(Left, Right)}.

join(Left, Right) ->
    erlang:garbage_collect(),
    {ok, {Left, Right}}.
