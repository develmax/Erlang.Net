-module(beam_discarded_case_literals).
-export([run/2]).
-define(DISABLED_DEBUG, ok).
run(Name, Payload) ->
    ?DISABLED_DEBUG,
    case whereis(Name) of
        Pid when is_pid(Pid) -> result(Pid, Payload);
        _ ->
            ?DISABLED_DEBUG,
            absent
    end.
result(Pid, Payload) -> {Pid, Payload}.
