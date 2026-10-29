-module(contract_worker513).
-export([run/1, worker/3]).

run(Subject) ->
    Ref = make_ref(),
    {Pid, Monitor} = erlang:spawn_monitor(?MODULE, worker, [self(), Ref, Subject]),
    try
        Result = receive {Ref, Pid, Value} -> Value
                 after 120000 -> error({worker_result_timeout513, Subject, Pid, Monitor}) end,
        Down = receive {'DOWN', Monitor, process, Pid, Reason} -> Reason
               after 5000 -> error({worker_down_timeout513, Subject, Pid, Monitor}) end,
        put(contract_worker513_witnesses, #{subject => Subject, caller => self(),
            worker => Pid, monitor => Monitor, correlation => Ref,
            worker_alive_after_down => is_process_alive(Pid), down_reason => Down}),
        {Result, Down}
    after
        case is_process_alive(Pid) of
            true -> exit(Pid, kill),
                receive {'DOWN', Monitor, process, Pid, _} -> ok
                after 5000 -> error({worker_cleanup_timeout513, Subject, Pid, Monitor}) end;
            false -> ok
        end,
        demonitor(Monitor, [flush])
    end.

worker(Parent, Ref, Subject) ->
    Result = try Subject:run() of Value -> {returned, Value}
             catch Class:Reason:Stack -> {raised, Class, Reason, Stack} end,
    Parent ! {Ref, self(), Result},
    ok.
