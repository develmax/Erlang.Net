-module(native_writable_observer).
-export([run/0,worker/1]).
run() ->
    Parent = self(),
    {Pid,Ref} = spawn_monitor(native_writable_observer,worker,[Parent]),
    receive {Pid,Result} ->
        receive {'DOWN',Ref,process,Pid,normal} -> Result end
    end.
worker(Parent) ->
    Result = native_writable_protocol:run(),
    Parent ! {self(),Result}.
