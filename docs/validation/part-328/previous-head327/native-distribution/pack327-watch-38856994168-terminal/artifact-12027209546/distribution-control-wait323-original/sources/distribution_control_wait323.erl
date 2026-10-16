-module(distribution_control_wait323).
-export([run/3, target/4, filler/4, worker/1]).

-define(BUSY_MS, 8000).
-define(QUERY_MS, 4000).
-define(KILL_MS, 4000).
-define(READY_MS, 5000).
-define(QUIET_MS, 250).
-define(PAYLOAD_BYTES, 65536).
-define(MAX_SENDS, 1024).
-define(MAX_EVENTS, 2048).
-define(TARGET_GO_MS, 20000).

run(B, C, Mode) ->
    put(events, []), put(progress, []),
    Previous = erlang:system_monitor(self(), [busy_dist_port]),
    Outcome = distribution_control_wait323_driver:capture(fun() -> measure(B, C, Mode) end),
    distribution_control_wait323_driver:save("phase-outcome.etf", Outcome),
    distribution_control_wait323_driver:save("resume-request.etf",
        {resume_request, distribution_control_wait323_driver:nonce(), Mode, node(), Outcome}),
    Cleanup = distribution_control_wait323_driver:capture(fun() ->
        try
            ok = distribution_control_wait323_driver:wait_marker("resume-ack", ?READY_MS),
            Results = [{Name, cleanup(Name)} || Name <- [query_worker, target, filler]],
            distribution_control_wait323_driver:save("local-worker-cleanup.etf", Results),
            true = lists:all(fun({_, {return, _}}) -> true; (_) -> false end, Results),
            {local_workers_joined, Results}
        after
            case Previous of
                undefined -> erlang:system_monitor(undefined);
                {Pid, Options} -> erlang:system_monitor(Pid, Options)
            end
        end
    end),
    {probe, Outcome, Cleanup}.

measure(B, C, Mode) ->
    pong = net_adm:ping(B), pong = net_adm:ping(C),
    Prepared = make_ref(),
    {control_wait323_destination, B} ! {prepare, self(), Prepared, B, C},
    {control_wait323_killer, C} ! {prepare, self(), Prepared, B, C},
    {Destination, Worker, BSourcePid} = receive
        {prepared, Prepared, BPid, WPid, OSPid} when node(BPid) =:= B, is_pid(WPid) -> {BPid, WPid, OSPid}
    after ?READY_MS -> erlang:error(destination_ready_timeout) end,
    Killer = receive
        {prepared, Prepared, CPid, undefined, _} when node(CPid) =:= C -> CPid
    after ?READY_MS -> erlang:error(killer_ready_timeout) end,
    _ = Destination,
    Controllers = erlang:system_info(dist_ctrl),
    {B, BController} = lists:keyfind(B, 1, Controllers),
    {C, CController} = lists:keyfind(C, 1, Controllers),
    true = BController =/= CController,
    distribution_control_wait323_driver:save("connections.etf", {node(), nodes(), Controllers}),
    Ref = make_ref(),
    {Target, TargetMonitor} = spawn_monitor(?MODULE, target, [self(), Worker, Mode, Ref]),
    put(target, {Target, TargetMonitor}),
    Ready = receive
        {target_ready, Ref, Target, Info} -> Info
    after ?READY_MS -> erlang:error(target_ready_timeout) end,
    distribution_control_wait323_driver:save("stop-request.etf",
        {stop_request, distribution_control_wait323_driver:nonce(), Mode, node(), B, BSourcePid}),
    ok = distribution_control_wait323_driver:wait_marker("stop-ack", ?READY_MS),
    End = erlang:monotonic_time(millisecond) + ?BUSY_MS,
    FRef = make_ref(),
    {Filler, FMonitor} = spawn_monitor(?MODULE, filler, [self(), Worker, FRef, ?MAX_SENDS]),
    put(filler, {Filler, FMonitor}),
    FStatus = wait_filler(Filler, FRef, End, false),
    Target ! {go, Ref},
    Busy = wait_target(Target, Ref, BController, End),
    QRef = make_ref(),
    Owner = self(),
    QueryWorker = spawn(fun() ->
        Result = distribution_control_wait323_driver:capture(fun() ->
            T0 = erlang:monotonic_time(millisecond),
            Information = process_info(Target, [links, status, async_dist, current_function]),
            {Information, T0, erlang:monotonic_time(millisecond)}
        end),
        Owner ! {query_result, QRef, Result}
    end),
    put(query_worker, {QueryWorker, undefined}),
    Query = receive
        {query_result, QRef, QValue} -> QValue
    after ?QUERY_MS -> erlang:error(query_timeout) end,
    distribution_control_wait323_driver:save("query.etf", {QRef, Query}),
    {return, {[{links, [Worker]}, {status, suspended}, {async_dist, false},
               {current_function, {M, F, Arity}}], Started, Ended}} = Query,
    true = is_atom(M) andalso is_atom(F) andalso is_integer(Arity),
    true = Ended >= Started,
    no_return(Target, Ref),
    KillRef = make_ref(),
    KillEnd = erlang:monotonic_time(millisecond) + ?KILL_MS,
    Killer ! {kill, self(), KillRef, Target},
    Ack = receive
        {kill_ack, KillRef, Killer, _} = KValue -> KValue
    after remaining(KillEnd) -> erlang:error(killer_ack_timeout) end,
    {kill_ack, KillRef, Killer, {return, true}} = Ack,
    Down = receive
        {'DOWN', TargetMonitor, process, Target, _} = DValue -> DValue
    after remaining(KillEnd) -> erlang:error(killed_down_timeout) end,
    {'DOWN', TargetMonitor, process, Target, killed} = Down,
    no_return(Target, Ref),
    Value = {observation, Mode,
        {identities, self(), Target, Worker, Filler, Killer, BController, CController},
        {prepared, Ready, FStatus},
        {busy, Busy, lists:reverse(get(events)), lists:reverse(get(progress))},
        {query, QRef, Query}, {kill, KillRef, Ack, Down}, {postcall, false}},
    distribution_control_wait323_driver:save("measurement.etf", Value),
    Value.

target(Owner, Worker, Mode, Ref) ->
    _ = process_flag(async_dist, false),
    _ = process_flag(trap_exit, true),
    case Mode of send -> true = link(Worker); link -> ok end,
    Owner ! {target_ready, Ref, self(), process_info(self(), [links, trap_exit, async_dist])},
    receive
        {go, Ref} ->
            Result = case Mode of send -> erlang:send(Worker, {target, Ref}); link -> erlang:link(Worker) end,
            Owner ! {returned, Ref, self(), Result},
            receive after 20000 -> erlang:error(unexpected_target_survival) end
    after ?TARGET_GO_MS -> erlang:error(target_go_timeout)
    end.

filler(Owner, Worker, Ref, Remaining) ->
    _ = process_flag(async_dist, false),
    fill(Owner, Worker, Ref, Remaining, binary:copy(<<0>>, ?PAYLOAD_BYTES), 0).

fill(Owner, _Worker, Ref, 0, _Payload, Count) ->
    Owner ! {filler_exhausted, Ref, self(), Count},
    exit(filler_budget_exhausted);
fill(Owner, Worker, Ref, Left, Payload, Count) ->
    _ = erlang:send(Worker, {fill, Ref, Count, Payload}),
    Owner ! {progress, Ref, self(), Count + 1},
    fill(Owner, Worker, Ref, Left - 1, Payload, Count + 1).

worker(Owner) ->
    _ = process_flag(trap_exit, true),
    Owner ! {worker_ready, self(), process_info(self(), trap_exit)},
    worker_loop().

worker_loop() -> receive _ -> worker_loop() end.

wait_filler(Pid, Ref, End, Seen) ->
    Timeout = case Seen of true -> erlang:min(?QUIET_MS, remaining(End)); false -> remaining(End) end,
    receive
        {monitor, Pid, busy_dist_port, _} = Event -> remember(Event), wait_filler(Pid, Ref, End, true);
        {monitor, _, busy_dist_port, _} = Event -> remember(Event), wait_filler(Pid, Ref, End, Seen);
        {progress, Ref, Pid, _} = Row -> progress(Row), wait_filler(Pid, Ref, End, Seen);
        {filler_exhausted, Ref, Pid, _} = Value -> erlang:error(Value);
        {'DOWN', _, process, Pid, _} = Value -> erlang:error(Value)
    after Timeout ->
        true = Seen,
        Status = process_info(Pid, status),
        case Status of {status, suspended} -> Status; _ -> wait_filler(Pid, Ref, End, false) end
    end.

wait_target(Pid, Ref, Controller, End) ->
    receive
        {monitor, Pid, busy_dist_port, Controller} = Event -> remember(Event), Event;
        {monitor, Pid, busy_dist_port, _} = Event -> erlang:error({wrong_target_controller, Event});
        {monitor, _, busy_dist_port, _} = Event -> remember(Event), wait_target(Pid, Ref, Controller, End);
        {progress, _, _, _} = Row -> progress(Row), wait_target(Pid, Ref, Controller, End);
        {returned, Ref, Pid, _} = Value -> erlang:error(Value);
        {filler_exhausted, _, _, _} = Value -> erlang:error(Value);
        {'DOWN', _, process, _, _} = Value -> erlang:error(Value)
    after remaining(End) -> erlang:error(target_busy_timeout)
    end.

no_return(Pid, Ref) ->
    receive {returned, Ref, Pid, _} = Value -> erlang:error(Value)
    after 0 -> ok end.

remember(Event) ->
    Rows = get(events), true = length(Rows) < ?MAX_EVENTS,
    put(events, [{Event, erlang:monotonic_time(millisecond)} | Rows]).

progress(Row) ->
    Rows = get(progress), true = length(Rows) < ?MAX_SENDS,
    put(progress, [Row | Rows]).

remaining(End) ->
    Left = End - erlang:monotonic_time(millisecond),
    true = Left > 0,
    Left.

cleanup(Name) ->
    distribution_control_wait323_driver:capture(fun() ->
        case get(Name) of
            undefined -> not_started;
            {Pid, Original} ->
                Result = distribution_control_wait323_driver:join(Pid),
                case Original of undefined -> ok; _ -> erlang:demonitor(Original, [flush]) end,
                Result
        end
    end).
