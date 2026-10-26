-module(queue_statistics504).
-export([load/1, cpu/0, io/0, release/0, status/0, run/1]).

load(Path) -> erlang:load_nif(Path, []).
cpu() -> erlang:nif_error(not_loaded).
io() -> erlang:nif_error(not_loaded).
release() -> erlang:nif_error(not_loaded).
status() -> erlang:nif_error(not_loaded).

wait(_, 0) -> erlang:error(state_not_reached);
wait(Predicate, N) ->
    case Predicate() of
        true -> ok;
        false -> receive after 1 -> wait(Predicate, N - 1) end
    end.

keys() -> [run_queue, run_queue_lengths, run_queue_lengths_all,
    total_run_queue_lengths, total_run_queue_lengths_all, active_tasks,
    active_tasks_all, total_active_tasks, total_active_tasks_all, run_queues].

sample(Name) ->
    Values = [{Key, erlang:statistics(Key)} || Key <- keys()],
    ok = file:write_file(filename:join(os:getenv("RESOURCE504_DIR"),
        atom_to_list(Name) ++ ".etf"), term_to_binary({Name, Values}, [{minor_version,2}])),
    {Name, Values}.

dirty_queues() ->
    [Io, Cpu | _] = lists:reverse(erlang:statistics(run_queue_lengths_all)),
    {Cpu, Io}.

await_down(Ref) ->
    receive {'DOWN', Ref, process, _, Reason} -> Reason
    after 5000 -> erlang:error(missing_down)
    end.

normal_loop() -> receive stop -> ok after 0 -> normal_loop() end.

run(debug_flags) ->
    Arguments = [true,false,0,-1,1 bsl 100,1.5,[],[a|b],{},#{},<<>>,<<1:1>>,
        self(),make_ref(),fun() -> ok end],
    Values = [erlang:system_flag(debug_flags, Value) || Value <- Arguments],
    true = length(Values) == 15,
    true = lists:all(fun(Value) -> Value == true end, Values),
    {debug_flags, Values};
run(normal_queue) ->
    {Pid, Ref} = spawn_monitor(fun normal_loop/0),
    try
        %% A reducible loop remains runnable while this observer gets real turns.
        receive after 10 -> ok end,
        Observed = sample(normal_runnable),
        true = exit(Pid, kill),
        killed = await_down(Ref),
        {normal_queue, Observed, sample(normal_retired)}
    after exit(Pid, kill)
    end;
run(Scenario) when Scenario == held_queues; Scenario == cancel_queued; Scenario == cancel_running ->
    {Cpu, CpuRef} = spawn_monitor(fun cpu/0),
    {Io, IoRef} = spawn_monitor(fun io/0),
    try
        ok = wait(fun() -> status() == {1,1,0,0} end, 2000),
        Held = sample(held),
        {CpuQueued, CpuQueuedRef} = spawn_monitor(fun cpu/0),
        {IoQueued, IoQueuedRef} = spawn_monitor(fun io/0),
        ok = wait(fun() -> dirty_queues() == {1,1} end, 2000),
        Queued = sample(queued),
        case Scenario of
            held_queues -> ok;
            cancel_queued -> exit(CpuQueued, kill), exit(IoQueued, kill);
            cancel_running -> exit(Cpu, kill), exit(Io, kill)
        end,
        %% Yield to ordinary signal handlers without releasing either native body.
        receive after 10 -> ok end,
        AfterAction = sample(after_action),
        Physical = status(),
        ok = release(),
        Reasons = [await_down(Ref) || Ref <- [CpuRef,IoRef,CpuQueuedRef,IoQueuedRef]],
        ok = wait(fun() -> dirty_queues() == {0,0} end, 2000),
        {Scenario,Held,Queued,AfterAction,Physical,Reasons,status(),sample(retired)}
    after release()
    end.
