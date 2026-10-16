-module(guest_connected_send319).
-export([run/1, worker/1, bang/2]).

-define(DEADLINE, 5000).

%% Source-only original-runtime probe. It has not yet been compiled or executed.
run(RemoteNode) when is_atom(RemoteNode), RemoteNode =/= node() ->
    true = lists:member(RemoteNode, nodes()),
    Remote = spawn(RemoteNode, ?MODULE, worker, [self()]),
    Monitor = erlang:monitor(process, Remote),
    Outcome = capture(fun() ->
        receive
            {ready, Remote} -> ok
        after ?DEADLINE -> erlang:error(worker_ready_timeout)
        end,
        Reference = make_ref(),
        First = {deliver, self(), Reference, send_two},
        First = erlang:send(Remote, First),
        Second = {deliver, self(), Reference, bang},
        Second = bang(Remote, Second),
        ok = erlang:send(Remote, {deliver, self(), Reference, no_connect}, [noconnect]),
        true = erlang:send_nosuspend(Remote, {deliver, self(), Reference, no_suspend}),
        true = erlang:send_nosuspend(
            Remote,
            {deliver, self(), Reference, no_suspend_no_connect},
            [noconnect]
        ),
        Ordered = receive
            {delivered, Reference, History} -> History
        after ?DEADLINE -> erlang:error(delivery_timeout)
        end,
        [send_two, bang, no_connect, no_suspend, no_suspend_no_connect] = Ordered,
        true = erlang:group_leader(self(), Remote),
        Probe = make_ref(),
        {probe, Probe} = erlang:send(Remote, {probe, Probe}),
        Leader = receive
            {leader, Probe, CurrentLeader} -> CurrentLeader
        after ?DEADLINE -> erlang:error(group_leader_probe_timeout)
        end,
        true = Leader =:= self(),
        {guest_connected_send319,
         [{send_two_returns_message_and_delivers, true},
          {bang_returns_message_and_delivers, true},
          {connected_noconnect_delivers, true},
          {connected_no_suspend_delivers, true},
          {connected_no_suspend_noconnect_delivers, true},
          {same_sender_five_message_order, true},
          {group_leader_then_public_send_order, true}],
         {raw, self(), Remote, Reference, Ordered, Leader}}
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
    put(guest_connected_send319_cleanup, Cleanup),
    case Outcome of
        {exception, Class, Failure, Stack} -> erlang:raise(Class, Failure, Stack);
        {return, Value} ->
            case Cleanup of
                {return, _} -> Value;
                {exception, CleanupClass, CleanupFailure, CleanupStack} ->
                    erlang:raise(CleanupClass, CleanupFailure, CleanupStack)
            end
    end.

bang(P, M) -> P ! M.

worker(Owner) ->
    Owner ! {ready, self()},
    worker_loop(Owner, []).

worker_loop(Owner, ReverseHistory) ->
    receive
        {deliver, Owner, Reference, Kind} ->
            History = [Kind | ReverseHistory],
            case length(History) of
                5 -> Owner ! {delivered, Reference, lists:reverse(History)};
                _ -> ok
            end,
            worker_loop(Owner, History);
        {probe, Reference} ->
            Owner ! {leader, Reference, erlang:group_leader()},
            worker_loop(Owner, ReverseHistory)
    after ?DEADLINE -> erlang:error(worker_idle_timeout)
    end.

capture(Operation) ->
    try Operation() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.
