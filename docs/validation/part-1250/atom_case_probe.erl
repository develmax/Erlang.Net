-module(atom_case_probe).
-export([run/2]).

run(Input, Payload) ->
    case maps:get(status, Input) of
        ready -> {ok, Payload};
        waiting -> pending
    end.
