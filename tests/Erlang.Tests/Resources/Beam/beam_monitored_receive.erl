-module(beam_monitored_receive).
-export([run/0, run/1]).
run() ->
    ok = beam_probe:setup(),
    Ref = monitor(process, beam_monitor_target),
    receive
        {'DOWN',Ref,process,_,_} -> done
    end.
run(Value) ->
    ok =
        beam_probe:setup(Value),
    Ref = erlang:monitor(process, beam_monitor_target),
    receive
        {'DOWN',Ref,process,_,_} -> done
    end.

