-module(guest_connected_links321).
-export([run/1, worker/1]).

-define(DEADLINE, 5000).

%% Source-only original two-node relation probe; no execution is claimed.
run(RemoteNode) when is_atom(RemoteNode), RemoteNode =/= node() ->
    true = lists:member(RemoteNode, nodes()),
    PreviousTrap = process_flag(trap_exit, true),
    Remote = spawn(RemoteNode, ?MODULE, worker, [self()]),
    Monitor = erlang:monitor(process, Remote),
    Outcome = capture(fun() ->
        receive
            {ready, Remote} -> ok
        after ?DEADLINE -> erlang:error(worker_ready_timeout)
        end,
        Baseline = observe(Remote, baseline, false, false),
        true = erlang:link(Remote),
        Linked = observe(Remote, linked, true, false),
        true = erlang:link(Remote),
        Duplicate = observe(Remote, duplicate_link, true, false),
        true = erlang:link(Remote, [priority]),
        Priority = observe(Remote, priority_link, true, true),
        true = erlang:link(Remote, []),
        Cleared = observe(Remote, cleared_priority, true, false),
        true = erlang:unlink(Remote),
        Unlinked = observe(Remote, unlinked, false, false),
        true = erlang:unlink(Remote),
        Repeat = observe(Remote, duplicate_unlink, false, false),
        true = erlang:link(Remote),
        Relinked = observe(Remote, relinked, true, false),
        true = erlang:unlink(Remote),
        Final = observe(Remote, final_unlink, false, false),
        {guest_connected_links321,
         [{public_link_propagates, true},
          {duplicate_link_idempotent, true},
          {link_priority_is_local_receiver_permission, true},
          {empty_link_options_clear_priority, true},
          {public_unlink_propagates, true},
          {duplicate_unlink_idempotent, true},
          {relink_after_unlink_propagates, true}],
         {raw, self(), Remote, [Baseline, Linked, Duplicate, Priority, Cleared,
                                Unlinked, Repeat, Relinked, Final]}}
    end),
    Cleanup = capture(fun() ->
        CleanupMonitor = erlang:monitor(process, Remote),
        true = erlang:unlink(Remote),
        erlang:exit(Remote, kill),
        receive
            {'DOWN', CleanupMonitor, process, Remote, Reason} -> {worker_joined, Reason}
        after ?DEADLINE ->
            erlang:demonitor(CleanupMonitor, [flush]),
            erlang:error(worker_cleanup_timeout)
        end
    end),
    erlang:demonitor(Monitor, [flush]),
    process_flag(trap_exit, PreviousTrap),
    put(guest_connected_links321_cleanup, Cleanup),
    case Outcome of
        {exception, Class, Failure, Stack} -> erlang:raise(Class, Failure, Stack);
        {return, Value} ->
            case Cleanup of
                {return, _} -> Value;
                {exception, CleanupClass, CleanupFailure, CleanupStack} ->
                    erlang:raise(CleanupClass, CleanupFailure, CleanupStack)
            end
    end.

observe(Remote, Phase, ExpectedLinked, ExpectedPriority) ->
    Reference = make_ref(),
    Message = {probe, self(), Reference, Phase},
    Message = guest_connected_bang322:bang(Remote, Message),
    RemoteLinks = receive
        {observed, Reference, Phase, Links} -> Links
    after ?DEADLINE -> erlang:error(relation_probe_timeout)
    end,
    {links, OwnerLinks} = erlang:process_info(self(), links),
    {priority_messages, Priority} = erlang:process_info(self(), priority_messages),
    ExpectedLinked = lists:member(self(), RemoteLinks),
    ExpectedLinked = lists:member(Remote, OwnerLinks),
    ExpectedCount = case ExpectedLinked of true -> 1; false -> 0 end,
    ExpectedCount = length([Pid || Pid <- RemoteLinks, Pid =:= self()]),
    ExpectedCount = length([Pid || Pid <- OwnerLinks, Pid =:= Remote]),
    ExpectedPriority = Priority,

    {Phase, OwnerLinks, RemoteLinks, Priority}.

worker(Owner) ->
    Owner ! {ready, self()},
    worker_loop(Owner).

worker_loop(Owner) ->
    receive
        {probe, Owner, Reference, Phase} ->
            {links, Links} = erlang:process_info(self(), links),
            Message = {observed, Reference, Phase, Links},
            Message = guest_connected_bang322:bang(Owner, Message),
            worker_loop(Owner)
    after ?DEADLINE -> erlang:error(worker_idle_timeout)
    end.

capture(Operation) ->
    try Operation() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.
