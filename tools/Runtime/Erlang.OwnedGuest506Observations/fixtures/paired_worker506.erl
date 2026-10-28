-module(paired_worker506).
-export([run/0, worker/3]).

run() ->
    Ref = make_ref(),
    {Pid, Monitor} = erlang:spawn_monitor(?MODULE, worker, [self(), Ref, owned_observer506]),
    try
        Result = receive
            {Ref, Pid, Observed} -> Observed
        after 15000 -> error({result_timeout, Pid, Monitor})
        end,
        Down = receive
            {'DOWN', Monitor, process, Pid, Reason} -> Reason
        after 5000 -> error({down_timeout, Pid, Monitor})
        end,
        put(paired_witness506, #{worker => Pid, monitor => Monitor, correlation => Ref,
                               worker_alive_after_down => is_process_alive(Pid), down_reason => Down}),
        {Result, Down}
    after
        case is_process_alive(Pid) of
            true ->
                exit(Pid, kill),
                receive {'DOWN', Monitor, process, Pid, _} -> ok
                after 5000 -> error({cleanup_timeout, Pid, Monitor})
                end;
            false -> ok
        end,
        demonitor(Monitor, [flush])
    end.

worker(Parent, Ref, Subject) ->
    Result = try Subject:run() of
        Observed -> {returned, Observed}
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end,
    Parent ! {Ref, self(), Result},
    ok.
