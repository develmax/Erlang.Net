-module(distribution_monitor_payload_observer).
-export([run/2]).
-define(ETF_VERSION, 131).
-define(NEW_PID, 88).
-define(PASS_THROUGH, 112).
-define(DEADLINE, 5000).

run(Output, Scenario) ->
    put(output, Output),
    put(operations, []),
    put(outputs, []),
    false = erlang:is_alive(),
    true = register(net_kernel, self()),
    true = erlang:setnode(signal_local@localhost, 1),
    Extra = scenario_flags(Scenario),
    Pending = erts_internal:new_connection(signal_remote@localhost),
    Owner = self(),
    {Controller, Monitor} = spawn_monitor(fun() -> controller(Owner, flags() bor Extra) end),
    try
        Handle = receive
                     {ready, Controller, Value} -> Value;
                     {'DOWN', Monitor, process, Controller, ReadyReason} ->
                         put(controller_ready_down, {observed, ReadyReason}),
                         save("ready-failure.etf", {observed, ReadyReason}),
                         error({controller_ready_failed, ReadyReason})
                 after ?DEADLINE -> error(controller_ready_timeout) end,
        put(controller, Controller),
        put(controller_monitor, Monitor),
        put(handle, Handle),
        Notice = receive Message = {dist_ctrlr, Controller, signal_remote@localhost, Controller} -> Message
                 after ?DEADLINE -> error(kernel_notice_timeout) end,
        save("identities.etf", {Owner, Controller, Pending, Handle, Notice, flags() bor Extra, Scenario}),
        observe_payload_monitor(Scenario),
        save("operations.etf", lists:reverse(get(operations))),
        save("outputs.etf", lists:reverse(get(outputs))),
        ok = file:write_file(filename:join(get(output), "stage"), <<"complete">>),
        ok
    after
        Controller ! stop,
        Cleanup = case erase(controller_down) of
            {observed, RetiredReason} -> {observed, RetiredReason};
            undefined -> case erase(controller_ready_down) of
            {observed, ObservedReason} -> {observed, ObservedReason};
            undefined -> receive {'DOWN', Monitor, process, Controller, CleanupReason} -> {observed, CleanupReason}
                         after ?DEADLINE -> exit(Controller, kill),
                             receive {'DOWN', Monitor, process, Controller, ForcedReason} -> {forced, ForcedReason}
                             after ?DEADLINE -> {unobserved, timeout} end end
            end
        end,
        save("cleanup.etf", Cleanup)
    end.

observe_payload_monitor(Scenario) ->
    Owner = self(),
    {Target, TargetMonitor} = spawn_monitor(fun() -> monitor_receiver(Owner) end),
    try
    Target ! {establish, Scenario},
    Ref = receive {origin, Target, Value} -> Value after ?DEADLINE -> error(origin_timeout) end,
    record(origin_reference, Ref),
    drain(origin_monitor),
    Watcher = case Scenario of
        dead_watcher_malformed ->
            Target ! stop,
            record(target_down, receive {'DOWN', TargetMonitor, process, Target, R} ->
                                   TargetDown = {observed, R},
                                   put(payload_target_down, TargetDown),
                                   TargetDown
                               after ?DEADLINE -> {unobserved, timeout} end),
            Target;
        _ -> Target
    end,
    Payload = case Scenario of matched_reason -> term_to_binary({monitor_reason, <<0,255,128>>}); _ -> <<131,255>> end,
    Remote = remote_pid(signal_remote@localhost, 11, 1),
    Control = {28, Remote, Watcher, Ref},
    Packet = <<?PASS_THROUGH, (term_to_binary(Control))/binary, Payload/binary>>,
    Request = make_ref(),
    get(controller) ! {input, Request, Packet},
    record(payload_input, {Control, Payload, Packet,
        receive {input_result, Request, Result} -> Result after ?DEADLINE -> {unobserved, timeout} end}),
    Target ! barrier,
    CapturedController = get(controller),
    record(post_input, receive {target_state, Target, Result2} -> {observed, Result2};
                       {'DOWN', Monitor, process, Controller, Reason} when Controller =:= CapturedController ->
                           put(controller_down, {observed, Reason}), {controller_down, Monitor, Reason}
                       after ?DEADLINE -> {unobserved, timeout} end),
    record(controller_stat, capture(fun() -> erlang:dist_get_stat(get(handle)) end)),
    ok
    after cleanup_payload_target(Target, TargetMonitor) end.

cleanup_payload_target(Target, TargetMonitor) ->
    Cleanup = case erase(payload_target_down) of
        {observed, _} = Observed -> Observed;
        undefined ->
            Target ! stop,
            receive {'DOWN', TargetMonitor, process, Target, Reason} -> {observed, Reason}
            after ?DEADLINE ->
                exit(Target, kill),
                receive {'DOWN', TargetMonitor, process, Target, ForcedReason} -> {forced, ForcedReason}
                after ?DEADLINE -> {unobserved, timeout} end
            end
    end,
    save("target-cleanup.etf", Cleanup).

monitor_receiver(Owner) ->
    receive {establish, Scenario} ->
        Remote = remote_pid(signal_remote@localhost, 11, 1),
        Ref = case Scenario of live_unknown_ref_malformed -> make_ref(); _ -> monitor(process, Remote) end,
        Owner ! {origin, self(), Ref},
        monitor_receiver_loop(Owner, [])
    end.

monitor_receiver_loop(Owner, Messages) ->
    receive stop -> ok;
        barrier -> Owner ! {target_state, self(), lists:reverse(Messages)}, monitor_receiver_loop(Owner, Messages);
        Message -> monitor_receiver_loop(Owner, [Message | Messages])
    end.

controller(Owner, Flags) ->
    Handle = erlang:setnode(signal_remote@localhost, self(), {Flags, 1}),
    ok = erlang:dist_ctrl_input_handler(Handle, self()),
    Owner ! {ready, self(), Handle},
    controller_loop(Owner, Handle).

controller_loop(Owner, Handle) ->
    receive
        {input, Request, Packet} ->
            Owner ! {input_result, Request, capture(fun() -> erlang:dist_ctrl_put_data(Handle, Packet) end)},
            controller_loop(Owner, Handle);
        {drain, Request} ->
            Owner ! {output_result, Request, capture(fun() -> erlang:dist_ctrl_get_data(Handle) end)},
            controller_loop(Owner, Handle);
        stop -> ok
    end.

drain(Name) ->
    Request = make_ref(),
    get(controller) ! {drain, Request},
    Result = receive {output_result, Request, Value} -> Value
             after ?DEADLINE -> {unobserved, timeout} end,
    put(outputs, [{Name, Result} | get(outputs)]).

record(Name, Value) -> put(operations, [{Name, Value} | get(operations)]).

capture(Fun) -> try {ok, Fun()} catch Class:Reason:Stack -> {exception, Class, Reason, Stack} end.

remote_pid(Node, Number, Creation) ->
    <<?ETF_VERSION, Atom/binary>> = term_to_binary(Node),
    binary_to_term(<<?ETF_VERSION, ?NEW_PID, Atom/binary, Number:32, 0:32, Creation:32>>).

scenario_flags(matched_reason) -> 16#400028;
scenario_flags(live_unknown_ref_malformed) -> 16#400028;
scenario_flags(dead_watcher_malformed) -> 16#400028.

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32).

save(Name, Term) -> ok = file:write_file(filename:join(get(output), Name), term_to_binary(Term)).
