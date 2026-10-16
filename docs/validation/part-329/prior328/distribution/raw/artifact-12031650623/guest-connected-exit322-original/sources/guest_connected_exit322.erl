-module(guest_connected_exit322).
-export([run/1, worker/1]).

-define(DEADLINE, 5000).
-define(LABEL, 42).
-define(LAST_COUNT, 4).
-define(SERIAL, 8).

run(RemoteNode) when is_atom(RemoteNode), RemoteNode =/= node() ->
    true = lists:member(RemoteNode, nodes()),
    PreviousTrap = process_flag(trap_exit, true),
    Remote = spawn(RemoteNode, ?MODULE, worker, [self()]),
    Monitor = erlang:monitor(process, Remote),
    Outcome = capture(fun() ->
        receive
            {ready, Remote, true} -> ok
        after ?DEADLINE -> erlang:error(worker_ready_timeout)
        end,
        Two = observe(Remote, exit_two, fun(Reason) -> erlang:exit(Remote, Reason) end, valid),
        Three = observe(Remote, exit_three, fun(Reason) -> erlang:exit(Remote, Reason, [priority]) end, valid),
        SignalTwo = observe(Remote, exit_signal_two, fun(Reason) -> erlang:exit_signal(Remote, Reason) end, valid),
        SignalThree = observe(Remote, exit_signal_three, fun(Reason) -> erlang:exit_signal(Remote, Reason, [priority]) end, valid),
        Duplicate = observe(Remote, duplicate_priority,
            fun(Reason) -> erlang:exit(Remote, Reason, [priority, priority]) end, valid),
        Unknown = observe(Remote, unknown_option,
            fun(Reason) -> erlang:exit(Remote, Reason, [priority, invalid_exit_option]) end, invalid),
        Improper = observe(Remote, improper_options,
            fun(Reason) -> erlang:exit_signal(Remote, Reason, [priority | invalid_exit_tail]) end, invalid),
        Kill = observe_kill(Remote),
        {guest_connected_exit322,
         [{exit_two, true}, {exit_three, true}, {exit_signal_two, true}, {exit_signal_three, true},
          {duplicate_priority, true}, {unknown_option, true}, {improper_options, true}, {kill_trapped_worker, true}],
         {raw, self(), Remote, [Two, Three, SignalTwo, SignalThree, Duplicate, Unknown, Improper, Kill]}}
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
    seq_trace:set_token([]),
    process_flag(trap_exit, PreviousTrap),
    put(guest_connected_exit322_cleanup, Cleanup),
    case Outcome of
        {exception, Class, Failure, Stack} -> erlang:raise(Class, Failure, Stack);
        {return, Value} ->
            case Cleanup of
                {return, _} -> Value;
                {exception, CleanupClass, CleanupFailure, CleanupStack} ->
                    erlang:raise(CleanupClass, CleanupFailure, CleanupStack)
            end
    end.

prepare(Remote) ->
    seq_trace:set_token([]),
    Reference = make_ref(),
    Message = {prepare, self(), Reference},
    Message = guest_connected_bang322:bang(Remote, Message),
    receive
        {prepared, Reference} -> ok
    after ?DEADLINE -> erlang:error(worker_prepare_timeout)
    end,

    Reference.

configure_token() ->
    seq_trace:set_token(label, ?LABEL),
    seq_trace:set_token(serial, {?LAST_COUNT, ?SERIAL}),
    seq_trace:set_token(send, true).

observe(Remote, Label, Operation, Kind) ->
    Reference = prepare(Remote),
    configure_token(),
    Reason = {Label, <<1, 2, 3>>},
    Before = seq_trace:get_token(),
    Result = capture(fun() -> Operation(Reason) end),
    After = seq_trace:get_token(),
    %% No receive or probe send occurs before the exact after snapshot.
    Before = After,
    Observation = case Kind of
        valid ->
            {return, true} = Result,
            receive
                {observed, {'EXIT', Owner, Reason}, WorkerToken} when Owner =:= self() ->
                    [] = WorkerToken,
                    {observed, Reference, {'EXIT', Owner, Reason}, WorkerToken}
            after ?DEADLINE -> erlang:error(exit_observation_timeout)
            end;
        invalid ->
            {exception, error, badarg, _Stack} = Result,
            {no_exit, Reference}
    end,
    Inspect = {inspect, self(), Reference},
    Inspect = guest_connected_bang322:bang(Remote, Inspect),
    History = receive
        {history, Reference, Values} -> Values
    after ?DEADLINE -> erlang:error(worker_history_timeout)
    end,
    case Kind of
        valid -> [{'EXIT', ExpectedOwner, Reason}] = History, true = ExpectedOwner =:= self();
        invalid -> [] = History
    end,

    {case_result, Label, Before, After, Result, Observation, History}.

observe_kill(Remote) ->
    _Reference = prepare(Remote),
    KillMonitor = erlang:monitor(process, Remote),
    configure_token(),
    Before = seq_trace:get_token(),
    Result = capture(fun() -> erlang:exit(Remote, kill) end),
    After = seq_trace:get_token(),
    Before = After,
    {return, true} = Result,
    Observation = receive
        {'DOWN', KillMonitor, process, Remote, killed} -> {down, KillMonitor, process, Remote, killed}
    after ?DEADLINE -> erlang:error(kill_down_timeout)
    end,

    {case_result, kill_trapped_worker, Before, After, Result, Observation, []}.

worker(Owner) ->
    process_flag(trap_exit, true),
    {trap_exit, true} = process_info(self(), trap_exit),
    Owner ! {ready, self(), true},
    worker_loop(Owner, []).

worker_loop(Owner, ReverseHistory) ->
    receive
        {prepare, Owner, Reference} ->
            seq_trace:set_token([]),
            Message = {prepared, Reference},
            Message = guest_connected_bang322:bang(Owner, Message),
            worker_loop(Owner, []);
        {'EXIT', Owner, _Reason} = Exit ->
            WorkerToken = seq_trace:get_token(),
            Message = {observed, Exit, WorkerToken},
            Message = guest_connected_bang322:bang(Owner, Message),
            worker_loop(Owner, [Exit | ReverseHistory]);
        {inspect, Owner, Reference} ->
            seq_trace:set_token([]),
            Message = {history, Reference, lists:reverse(ReverseHistory)},
            Message = guest_connected_bang322:bang(Owner, Message),
            worker_loop(Owner, ReverseHistory)
    after ?DEADLINE -> erlang:error(worker_idle_timeout)
    end.

capture(Operation) ->
    try Operation() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.
