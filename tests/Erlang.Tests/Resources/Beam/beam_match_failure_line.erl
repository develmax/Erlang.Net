-module(beam_match_failure_line).
-export([run/2, first/0, second/0]).
run(Fun, Input) ->
    {ok, Value} = Fun(Input),
    Value.
first() -> ok. second() -> ok.
