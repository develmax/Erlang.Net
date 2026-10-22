-module(ordsets_core_worker).
-export([run/2,worker/1]).
run(Module,Entry) ->
    {Pid,Ref}=spawn_monitor(ordsets_core_worker,worker,[{self(),Module,Entry}]),
    wait_result(Pid,Ref).
worker({Parent,Module,Entry}) ->
    Rows=erlang:apply(Module,Entry,[]),
    Parent!{self(),Rows},
    normal.
wait_result(Pid,Ref) ->
    receive
        {Pid,Rows} -> wait_down(Pid,Ref,Rows);
        {'DOWN',Ref,process,Pid,Reason} -> erlang:error({observer_worker_failed,Reason})
    after worker_timeout() -> abort(Pid,Ref)
    end.
wait_down(Pid,Ref,Rows) ->
    receive
        {'DOWN',Ref,process,Pid,normal} -> Rows;
        {'DOWN',Ref,process,Pid,Reason} -> erlang:error({observer_worker_failed,Reason})
    after cleanup_timeout() -> abort(Pid,Ref)
    end.
abort(Pid,Ref) ->
    exit(Pid,kill),
    receive
        {'DOWN',Ref,process,Pid,_} -> erlang:error(observer_worker_timeout)
    after cleanup_timeout() -> erlang:error(observer_worker_cleanup_timeout)
    end.
worker_timeout() -> 540000.
cleanup_timeout() -> 5000.
