-module(beam_pid_heap).
-export([notify/2]).
notify(Key, Pid) ->
    erlang:put(seen, Key),
    case erlang:get(Key) of
        ready -> Pid ! {started, self()};
        _ -> Pid ! {wrong, node()}
    end.
