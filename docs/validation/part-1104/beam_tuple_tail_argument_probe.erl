-module(beam_tuple_tail_argument_probe).
-export([run/3]).
run(Ref, State, Module) ->
    Error = {undefined_import, helper(Ref), Module},
    finish(State, Ref, Error).
helper(fail) -> erlang:error(probe_failure);
helper(Ref) -> {name, Ref}.
finish(State, Ref, Error) -> {State, Ref, Error}.
