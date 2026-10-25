-module(distribution_signal_observer).
-export([run/2]).
-define(ETF_VERSION, 131).
-define(NEW_PID, 88).
-define(NEWER_REFERENCE, 90).
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
        case Scenario of
            group_leader -> observe_group_leader();
            _ -> observe_monitors()
        end,
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

observe_monitors() ->
    Remote = remote_pid(signal_remote@localhost, 11, 1),
    Ref = remote_ref(signal_remote@localhost, 13, 1),
    Ref2 = remote_ref(signal_remote@localhost, 17, 1),
    {Target, TargetMonitor} = spawn_monitor(fun target/0),
    input({19, Remote, signal_missing_target, Ref}),
    drain(missing),
    input({19, Remote, Target, Ref}),
    record(pid_monitored_by, capture(fun() -> process_info(Target, monitored_by) end)),
    Target ! stop,
    record(target_death, receive {'DOWN', TargetMonitor, process, Target, Reason} -> {observed, Reason}
                         after ?DEADLINE -> {unobserved, timeout} end),
    drain(pid_death),
    Observer = self(),
    {Named, NamedMonitor} = spawn_monitor(fun() ->
        true = register(signal_named_target, self()),
        Observer ! {named_ready, self()},
        target() end),
    record(named_registered, receive {named_ready, Named} -> {observed, Named}
                             after ?DEADLINE -> {unobserved, timeout} end),
    input({19, Remote, signal_named_target, Ref2}),
    record(name_monitored_by, capture(fun() -> process_info(Named, monitored_by) end)),
    input({20, Remote, signal_named_target, Ref2}),
    record(name_after_demonitor, capture(fun() -> process_info(Named, monitored_by) end)),
    Named ! stop,
    record(named_death, receive {'DOWN', NamedMonitor, process, Named, NameReason} -> {observed, NameReason}
                       after ?DEADLINE -> {unobserved, timeout} end),
    drain(name_demonitored_death),
    Origin = monitor(process, Remote),
    record(origin_reference, Origin),
    drain(origin_monitor),
    input({21, Remote, self(), Origin, observer_remote_reason}),
    record(origin_down, receive {'DOWN', Origin, process, Reported, DownReason} -> {observed, Reported, DownReason}
                        after ?DEADLINE -> {unobserved, timeout} end),
    Fallback = monitor(process, {signal_remote_name, signal_remote@localhost}),
    record(fallback_reference, Fallback),
    drain(connection_monitor),
    get(controller) ! stop,
    record(connection_only_down,
        receive {'DOWN', Fallback, process, FallbackTarget, FallbackReason} -> {observed, FallbackTarget, FallbackReason}
        after ?DEADLINE -> {unobserved, timeout} end).

observe_group_leader() ->
    {Target, TargetMonitor} = spawn_monitor(fun target/0),
    {Leader, LeaderMonitor} = spawn_monitor(fun target/0),
    External = remote_pid(signal_third@localhost, 19, 23),
    record(initial_group_leader, capture(fun() -> process_info(Target, group_leader) end)),
    input({7, Leader, Target}),
    record(local_group_leader, capture(fun() -> process_info(Target, group_leader) end)),
    Leader ! stop,
    record(leader_death, receive {'DOWN', LeaderMonitor, process, Leader, LeaderReason} -> {observed, LeaderReason}
                        after ?DEADLINE -> {unobserved, timeout} end),
    input({7, Leader, Target}),
    record(dead_leader_group_leader, capture(fun() -> process_info(Target, group_leader) end)),
    input({7, External, Target}),
    input({7, Target, Target}),
    record(ordered_group_leader, capture(fun() -> process_info(Target, group_leader) end)),
    input({7, External, Target}),
    case stop_controller() of
        {observed, _} ->
            record(post_teardown_group_leader, capture(fun() -> process_info(Target, group_leader) end));
        Unobserved -> record(teardown_unobserved, Unobserved)
    end,
    Target ! stop,
    record(member_death, receive {'DOWN', TargetMonitor, process, Target, TargetReason} -> {observed, TargetReason}
                        after ?DEADLINE -> {unobserved, timeout} end).

stop_controller() ->
    Controller = get(controller),
    Monitor = get(controller_monitor),
    Controller ! stop,
    Result = receive {'DOWN', Monitor, process, Controller, Reason} ->
                         Value = {observed, Reason},
                         put(controller_down, Value),
                         Value
             after ?DEADLINE -> {unobserved, timeout} end,
    record(controller_teardown, Result),
    Result.

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

input(Control) ->
    Bytes = <<?PASS_THROUGH, (term_to_binary(Control))/binary>>,
    Request = make_ref(),
    get(controller) ! {input, Request, Bytes},
    Result = receive {input_result, Request, Value} -> Value
             after ?DEADLINE -> {unobserved, timeout} end,
    record(input, {Control, Bytes, Result}).

drain(Name) ->
    Request = make_ref(),
    get(controller) ! {drain, Request},
    Result = receive {output_result, Request, Value} -> Value
             after ?DEADLINE -> {unobserved, timeout} end,
    put(outputs, [{Name, Result} | get(outputs)]).

record(Name, Value) -> put(operations, [{Name, Value} | get(operations)]).

capture(Fun) -> try {ok, Fun()} catch Class:Reason:Stack -> {exception, Class, Reason, Stack} end.

target() -> receive stop -> ok; _ -> target() end.

remote_pid(Node, Number, Creation) ->
    <<?ETF_VERSION, Atom/binary>> = term_to_binary(Node),
    binary_to_term(<<?ETF_VERSION, ?NEW_PID, Atom/binary, Number:32, 0:32, Creation:32>>).

remote_ref(Node, Word, Creation) ->
    <<?ETF_VERSION, Atom/binary>> = term_to_binary(Node),
    binary_to_term(<<?ETF_VERSION, ?NEWER_REFERENCE, 1:16, Atom/binary, Creation:32, Word:32>>).

scenario_flags(monitors_both) -> 16#08 bor 16#20;
scenario_flags(monitors_none) -> 0;
scenario_flags(monitors_pid) -> 16#08;
scenario_flags(monitors_name) -> 16#20;
scenario_flags(group_leader) -> 0.

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32).

save(Name, Term) -> ok = file:write_file(filename:join(get(output), Name), term_to_binary(Term)).
