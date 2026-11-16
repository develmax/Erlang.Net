-module(beam_guarded_monitor).
-export([run/1, wait/1]).
run(Name) when is_atom(Name) ->
    run([Name]);
run(Names) when is_list(Names) ->
    ok = beam_probe:setup(Names),
    Ref = monitor(process, beam_monitor_target),
    receive
        {'DOWN',Ref,process,_,_} -> done
    end.
wait(Value) when is_integer(Value) ->
    ok = beam_probe:setup(Value),
    Ref = erlang:monitor(process, beam_monitor_target),
    receive
        {'DOWN',Ref,process,_,_} -> done
    end.
