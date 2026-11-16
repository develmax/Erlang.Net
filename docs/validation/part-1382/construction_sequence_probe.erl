-module(construction_sequence_probe).
-export([spec/1, nil_init/1, shared/1]).

spec(Args) ->
    Strategy = simple_one_for_one,
    Max = 0,
    Period = 3600,
    Start = {worker, start_link, Args},
    Kind = worker,
    Child = {undefined, Start, temporary, 4000, Kind, [worker]},
    {ok, {{Strategy, Max, Period}, [Child]}}.

nil_init([]) ->
    note(started),
    Flags = #{strategy => simple_one_for_one, intensity => 1},
    Child = #{id => worker, start => {worker, start_link, []}},
    {ok, {Flags, [Child]}}.

shared(Input) ->
    Box = {tag, Input},
    {Box, [Box]}.

note(Value) ->
    put(probe, Value).
