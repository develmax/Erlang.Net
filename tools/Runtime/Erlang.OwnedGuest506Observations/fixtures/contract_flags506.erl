-module(contract_flags506).
-export([run/0]).

capture(Fun) ->
    try Fun() of Value -> {returned, Value}
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.

globals() ->
    [{Key, erlang:system_info(Key)} || Key <-
        [min_heap_size, min_bin_vheap_size, max_heap_size, fullsweep_after]].

value(Key, Snapshot) ->
    case proplists:get_value(Key, Snapshot) of
        {Key, Value} -> Value;
        Value -> Value
    end.

restore(Snapshot) ->
    % Unlimited first: restoring a larger minimum must not make restoring the
    % saved maximum fail because of an intermediate minimum/maximum relation.
    erlang:system_flag(max_heap_size, 0),
    [erlang:system_flag(Key, value(Key, Snapshot)) || Key <-
        [min_heap_size, min_bin_vheap_size, fullsweep_after]],
    erlang:system_flag(max_heap_size, value(max_heap_size, Snapshot)),
    After = globals(),
    case After =:= Snapshot of
        true -> {restored, After};
        false -> error({global_restore_mismatch, Snapshot, After})
    end.

run() ->
    Original = globals(),
    LogToken = make_ref(),
    Logger = capture(fun() -> logger:add_handler(contract_flags_log506, contract_logger506,
        #{level => all, config => #{owner => self(), token => LogToken}}) end),
    try
        Cases = [{Key, Value, none} || {Key, Values} <- contract_cases506:flag_values(),
                                      Value <- Values] ++ effect_cases(),
        Observed = [capture(fun() -> isolated(Key, Value, Effect, Original) end) ||
                    {Key, Value, Effect} <- Cases],
        {#{caller => self(), node => node(), initial_defaults => Original,
           heap_classes => erlang:system_info(heap_sizes)},
         Logger, Observed, sweep_comparison(), drain_logs(LogToken), globals()}
    after
        restore(Original),
        capture(fun() -> logger:remove_handler(contract_flags_log506) end)
    end.

effect_cases() ->
    [{max_heap_size, #{size => 4096, kill => true, error_logger => false}, list},
     {max_heap_size, #{size => 4096, kill => false, error_logger => false}, list},
     {max_heap_size, #{size => 4096, kill => false, error_logger => true}, list},
     {max_heap_size, #{size => 4096, kill => true, error_logger => false,
                       include_shared_binaries => false}, binary},
     {max_heap_size, #{size => 4096, kill => true, error_logger => false,
                       include_shared_binaries => true}, binary}].

isolated(Key, Value, Effect, Original) ->
    restore(Original),
    {Existing, ExistingMon, ExistingToken} = start([]),
    Observation = capture(fun() ->
        Before = globals(),
        ExistingBefore = info(Existing),
        Setter = capture(fun() -> erlang:system_flag(Key, Value) end),
        After = globals(),
        New = capture(fun() -> observe([], Effect) end),
        ExistingAfter = info(Existing),
        {Before, Setter, After,
         #{existing_child => Existing, existing_monitor => ExistingMon,
           existing_token => ExistingToken}, ExistingBefore, ExistingAfter, New}
    end),
    Settlement = capture(fun() -> settle(Existing, ExistingMon) end),
    Restoration = restore(Original),
    {Key, Value, Effect, Observation, Settlement, Restoration}.

% Spawn's Uint16 conversion and system_flag's saturation are distinct source
% protocols. Both results below come from actual child process_info values.
sweep_comparison() ->
    Original = globals(),
    try
        [begin
            Setter = capture(fun() -> erlang:system_flag(fullsweep_after, Input) end),
            {Input, Setter, erlang:system_info(fullsweep_after),
             capture(fun() -> observe([], none) end),
             capture(fun() -> observe([{fullsweep_after, Input}], none) end)}
         end || Input <- [65535, 65536, 65537]]
    after restore(Original)
    end.

start(Options) ->
    Parent = self(),
    Token = make_ref(),
    Pid = erlang:spawn_opt(fun() -> contract_spawn_subject506:held(Parent, Token, []) end, Options),
    Mon = monitor(process, Pid),
    receive
        {held, Token, Pid} -> {Pid, Mon, Token};
        {'DOWN', Mon, process, Pid, Reason} -> error({child_died_before_ready, Pid, Reason})
    after 5000 ->
        settle(Pid, Mon),
        error({held_timeout, Pid, Mon, Token})
    end.

observe(Options, Effect) ->
    {Pid, Mon, Token} = start(Options),
    Observation = capture(fun() ->
        Before = info(Pid),
        Work = work(Pid, Mon, Effect),
        {#{child => Pid, monitor => Mon, token => Token}, Before, Work, info(Pid)}
    end),
    {Observation, settle(Pid, Mon)}.

info(Pid) ->
    erlang:process_info(Pid, [heap_size, total_heap_size, stack_size,
        min_heap_size, min_bin_vheap_size, max_heap_size, fullsweep_after, garbage_collection]).

work(_, _, none) -> not_requested;
work(Pid, Mon, Kind) ->
    Ref = make_ref(),
    Pid ! {allocate, self(), Ref, Kind},
    receive
        {allocated, Ref, Pid, _} = Ack ->
            GcRef = make_ref(),
            Pid ! {gc, self(), GcRef},
            receive
                {collected, GcRef, Pid, _} = Gc -> {Ack, Gc};
                {'DOWN', Mon, process, Pid, _} = Down ->
                    put({contract_flags_down506, Mon}, Down), {Ack, Down}
            after 5000 -> error({gc_ack_timeout, Pid, GcRef})
            end;
        {'DOWN', Mon, process, Pid, _} = Down ->
            put({contract_flags_down506, Mon}, Down), Down
    after 5000 -> error({allocation_ack_timeout, Pid, Ref})
    end.

settle(Pid, Mon) ->
    exit(Pid, kill),
    Down = case erase({contract_flags_down506, Mon}) of
        undefined -> receive {'DOWN', Mon, process, Pid, _} = Message -> Message
            after 1000 -> error({down_timeout, Pid, Mon, is_process_alive(Pid)}) end;
        Message -> Message
    end,
    false = is_process_alive(Pid),
    demonitor(Mon, [flush]),
    Down.

drain_logs(Token) ->
    receive {contract_log506, Token, Event} -> [Event | drain_logs(Token)]
    after 0 -> []
    end.
