-module(group_leader_outbound318).
-export([run/1, worker/1]).

-define(DEADLINE, 5000).

run(RemoteNode) when is_atom(RemoteNode), RemoteNode =/= node() ->
    true = lists:member(RemoteNode, nodes()),
    put(group_leader_outbound318_operations, []),
    Remote = spawn(RemoteNode, ?MODULE, worker, [self()]),
    Monitor = erlang:monitor(process, Remote),
    Outcome = capture(fun() ->
        receive
            {ready, Remote} -> ok
        after ?DEADLINE -> erlang:error(worker_ready_timeout)
        end,
        true = observe(first_set, fun() -> erlang:group_leader(self(), Remote) end),
        First = observe(first_probe, fun() -> probe(Remote) end),
        true = First =:= self(),
        DeadLeader = dead_local_pid(),
        true = observe(ordered_first_set, fun() -> erlang:group_leader(self(), Remote) end),
        true = observe(last_set, fun() -> erlang:group_leader(DeadLeader, Remote) end),
        Last = observe(last_probe, fun() -> probe(Remote) end),
        true = Last =:= DeadLeader,
        Remote ! stop,
        receive
            {'DOWN', Monitor, process, Remote, normal} -> ok
        after ?DEADLINE -> erlang:error(worker_stop_timeout)
        end,
        true = observe(dead_remote_set, fun() -> erlang:group_leader(self(), Remote) end),
        {group_leader_outbound318,
         [{connected_leader_observed, true},
          {same_sender_last_update_observed, true},
          {dead_leader_accepted_and_observed, true},
          {dead_remote_target_accepted, true}],
         {raw_pids, self(), Remote, First, DeadLeader, Last}}
    end),
    Cleanup = capture(fun() ->
        CleanupMonitor = erlang:monitor(process, Remote),
        erlang:exit(Remote, kill),
        receive
            {'DOWN', CleanupMonitor, process, Remote, Reason} -> {worker_joined, Reason}
        after ?DEADLINE ->
            erlang:demonitor(CleanupMonitor, [flush]),
            erlang:error(worker_cleanup_timeout)
        end
    end),
    erlang:demonitor(Monitor, [flush]),
    put(group_leader_outbound318_cleanup, Cleanup),
    case Outcome of
        {exception, Class, Reason, Stack} -> erlang:raise(Class, Reason, Stack);
        {return, Value} ->
            case Cleanup of
                {return, _} -> Value;
                {exception, CleanupClass, CleanupReason, CleanupStack} ->
                    erlang:raise(CleanupClass, CleanupReason, CleanupStack)
            end
    end.

observe(Name, Operation) ->
    Outcome = capture(Operation),
    Operations = [{Name, Outcome} | get(group_leader_outbound318_operations)],
    put(group_leader_outbound318_operations, Operations),
    case os:getenv("GROUP_LEADER_OUTPUT") of
        false -> ok;
        Directory ->
            ok = file:write_file(filename:join(Directory, "operations.etf"), term_to_binary(lists:reverse(Operations)))
    end,
    io:format("~tp.~n", [{Name, Outcome}]),
    case Outcome of
        {return, Value} -> Value;
        {exception, Class, Reason, Stack} -> erlang:raise(Class, Reason, Stack)
    end.

capture(Operation) ->
    try Operation() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

probe(Remote) ->
    Reference = make_ref(),
    Remote ! {probe, self(), Reference},
    receive
        {Reference, ObservedLeader} -> ObservedLeader
    after ?DEADLINE -> erlang:error(group_leader_probe_timeout)
    end.

dead_local_pid() ->
    {Pid, Reference} = spawn_monitor(fun() -> ok end),
    receive
        {'DOWN', Reference, process, Pid, normal} -> Pid
    after ?DEADLINE -> erlang:error(local_leader_stop_timeout)
    end.

worker(Owner) ->
    Owner ! {ready, self()},
    worker_loop().

worker_loop() ->
    receive
        {probe, Owner, Reference} ->
            Owner ! {Reference, erlang:group_leader()},
            worker_loop();
        stop -> ok
    after ?DEADLINE -> erlang:error(worker_idle_timeout)
    end.
