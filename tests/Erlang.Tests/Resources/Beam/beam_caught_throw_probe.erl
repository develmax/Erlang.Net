-module(beam_caught_throw_probe).
-export([run/1]).

run(Input) ->
    catch scan(Input, []).

scan([stop|Rest], Acc) ->
    {{token,lists:reverse(Acc)},Rest};
scan([Head|Rest], Acc) ->
    scan(Rest, [Head|Acc]);
scan([], _) ->
    throw({error,missing}).
