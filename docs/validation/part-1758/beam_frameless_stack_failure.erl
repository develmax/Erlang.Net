-module(beam_frameless_stack_failure).
-export([run/2]).

run(Triple, Keep) ->
    {_, Key, Value} = Triple,
    Old = put(Key, Value),
    {Old, Keep, put(next, Old)}.
