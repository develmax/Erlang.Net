-module(queue_statistics504).
-export([load/1, cpu/0, io/0, release/0, status/0, details/0, run/1]).

load(Path) -> erlang:load_nif(Path, []).
cpu() -> erlang:nif_error(not_loaded).
io() -> erlang:nif_error(not_loaded).
release() -> erlang:nif_error(not_loaded).
status() -> erlang:nif_error(not_loaded).
details() -> erlang:nif_error(not_loaded).

retain(Name, Value) ->
    put(resource504_captures, [{Name,Value} | get(resource504_captures)]),
    Value.

poll(Label, Observation) ->
    Row = {Label,erlang:monotonic_time(millisecond),Observation},
    put(resource504_polls, lists:sublist([Row | get(resource504_polls)], 32)).

wait(Label, Predicate, N) ->
    {Ready,Observation} = Predicate(),
    poll(Label,Observation),
    case {Ready,N} of
        {true,_} -> ok;
        {false,0} -> erlang:error({state_not_reached,Label,Observation});
        {false,_} -> receive after 1 -> wait(Label,Predicate,N - 1) end
    end.

flush() ->
    Captures = lists:reverse(get(resource504_captures)),
    Diagnostic = {diagnostics,lists:reverse(get(resource504_polls)),lists:reverse(get(resource504_ready))},
    lists:foreach(fun({Name,Value}) ->
        ok = file:write_file(filename:join(os:getenv("RESOURCE504_DIR"),
            atom_to_list(Name) ++ ".etf"),term_to_binary(Value,[{minor_version,2}]))
    end, Captures ++ [{diagnostics,Diagnostic}]).

keys() -> [run_queue, run_queue_lengths, run_queue_lengths_all,
    total_run_queue_lengths, total_run_queue_lengths_all, active_tasks,
    active_tasks_all, total_active_tasks, total_active_tasks_all, run_queues].

sample(Name) ->
    Values = [{Key, erlang:statistics(Key)} || Key <- keys()],
    %% File I/O itself uses the dirty IO worker held by this experiment.
    %% Retain the actual observation in memory; flush only after release.
    retain(Name,{Name,Values}).

dirty_queues() ->
    [Io, Cpu | _] = lists:reverse(erlang:statistics(run_queue_lengths_all)),
    {Cpu, Io}.

await_down(Ref) ->
    receive {'DOWN', Ref, process, _, Reason} -> Reason
    after 5000 -> erlang:error(missing_down)
    end.

normal_loop() -> receive stop -> ok after 0 -> normal_loop() end.

spawn_native(Kind) ->
    Parent = self(),
    spawn_monitor(fun() ->
        Parent ! {resource504_ready,self(),Kind,erlang:process_info(self(),current_function)},
        case Kind of cpu -> cpu(); io -> io() end
    end).

ready(Count) when Count > 0 ->
    receive
        {resource504_ready,Pid,Kind,Function} ->
            put(resource504_ready,[{Pid,Kind,Function} | get(resource504_ready)]),
            ready(Count - 1)
    after 2000 -> erlang:error({native_not_admitted,Count})
    end;
ready(0) -> ok.

native_observation() -> {dirty_queues(),status(),details()}.

native_complete() ->
    S = status(),
    {Cpu,Io,CpuDone,IoDone} = S,
    {Cpu == CpuDone andalso Io == IoDone,native_observation()}.

run(Scenario) ->
    put(resource504_captures,[]),
    put(resource504_polls,[]),
    put(resource504_ready,[]),
    Outcome = try run_case(Scenario) of Value -> {returned,Value}
        catch Class:Reason:Stack -> {raised,Class,Reason,Stack}
    end,
    Flush = try flush() of ok -> ok catch FC:FR:FS -> {flush_failed,FC,FR,FS} end,
    case {Outcome,Flush} of
        {{returned,Returned},ok} -> Returned;
        {{raised,C,R,S},ok} -> erlang:raise(C,R,S);
        _ -> erlang:error({capture_flush_failed,Outcome,Flush})
    end.

run_case(debug_flags) ->
    Arguments = [true,false,0,-1,1 bsl 100,1.5,[],[a|b],{},#{},<<>>,<<1:1>>,
        self(),make_ref(),fun() -> ok end],
    Values = [erlang:system_flag(debug_flags, Value) || Value <- Arguments],
    true = length(Values) == 15,
    true = lists:all(fun(Value) -> Value == true end, Values),
    {debug_flags, Values};
run_case(normal_queue) ->
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
run_case(Scenario) when Scenario == held_queues; Scenario == cancel_queued; Scenario == cancel_running ->
    {Cpu, CpuRef} = spawn_native(cpu),
    {Io, IoRef} = spawn_native(io),
    try
        ok = ready(2),
        ok = wait(held,fun() -> S=status(),{S == {1,1,0,0},native_observation()} end,2000),
        Held = sample(held),
        {CpuQueued, CpuQueuedRef} = spawn_native(cpu),
        {IoQueued, IoQueuedRef} = spawn_native(io),
        ok = ready(2),
        ok = wait(queued,fun() -> Q=dirty_queues(),{Q == {1,1},native_observation()} end,2000),
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
        ok = wait(retired,fun() -> Q=dirty_queues(),{Q == {0,0},native_observation()} end,2000),
        {Scenario,Held,Queued,AfterAction,Physical,Reasons,status(),sample(retired)}
    after
        release(),
        Cleanup = try wait(cleanup_wait,fun native_complete/0,2000) of ok -> ok
            catch CC:CR:CS -> {cleanup_failed,CC,CR,CS}
        end,
        case Cleanup of
            ok -> ok;
            _ -> retain(cleanup_error,Cleanup),poll(cleanup_failed,native_observation())
        end,
        poll(cleanup,native_observation())
    end.
