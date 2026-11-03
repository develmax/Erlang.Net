-module(append_root_capture).
-export([run/0,worker/1]).
run() ->
    Parent = self(),
    {Pid, Monitor} = spawn_monitor(append_root_capture, worker, [Parent]),
    receive
        {Pid, Result} -> Result;
        {'DOWN', Monitor, process, Pid, Reason} -> erlang:error({worker_failed,Reason})
    end.
worker(Parent) ->
    Result = append_root_observer:run(),
    Parent ! {self(), Result}.
