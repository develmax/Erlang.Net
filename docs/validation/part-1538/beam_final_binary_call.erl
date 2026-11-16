-module(beam_final_binary_call).
-export([run/1, last/1]).

run(Input) ->
    <<17:8, (?MODULE:last(Input))/binary>>.

last(Input) ->
    Input.
