-module(distribution_pressure_probe).
-export([run/2]).

-define(DEADLINE, 4000).
-define(MAX_FILL, 256).
-define(MAX_DRAIN, 512).
-define(REMOTE, pressure_remote@localhost).
-define(UNLINK_ID, 173).

run(Case, Output) ->
    put(output, Output),
    put(children, []),
    false = erlang:is_alive(),
    true = register(net_kernel, self()),
    true = erlang:setnode(pressure_local@localhost, 1),
    Owner = self(),
    Pending = erts_internal:new_connection(?REMOTE),
    {Controller, _Monitor} = child(fun() -> controller(Owner) end),
    put(controller, Controller),
    try
        Handle = receive {ready, Controller, H} -> H
                 after ?DEADLINE -> error(controller_ready_timeout) end,
        put(handle, Handle),
        Notice = receive N = {dist_ctrlr, Controller, ?REMOTE, Controller} -> N
                 after ?DEADLINE -> error(kernel_notice_timeout) end,
        save("setup.etf", {Case, self(), Controller, Pending, Handle, Notice,
            flags(), node(), erlang:system_info(creation),
            erlang:system_info(otp_release), erlang:system_info(version),
            erlang:system_info(system_architecture), erlang:system_info(wordsize),
            erlang:system_info(dist_buf_busy_limit)}),
        scenario(Case, Controller, Handle),
        save("outer.etf", {result, ok}),
        save_bytes("stage", <<"complete">>)
    catch Class:Reason:Stack ->
        save("failure-owner-mailbox.etf", process_info(self(), messages)),
        save("failure-controller.etf", process_info(Controller, [status, current_function, current_stacktrace])),
        save("outer.etf", {exception, Class, Reason, Stack}),
        save_bytes("stage", <<"failed">>),
        erlang:raise(Class, Reason, Stack)
    after cleanup() end.

scenario(same_controller_replies, Controller, Handle) ->
    {Dead, DeadMonitor} = child(fun() -> ok end),
    receive {'DOWN', DeadMonitor, process, Dead, normal} -> ok
    after ?DEADLINE -> error(dead_target_timeout) end,
    Remote = remote_pid(11),
    fill(Handle),
    save("identities.etf", {Remote, Dead}),
    input(missing_link, {1, Remote, Dead}, Controller),
    input(unlink_id, {35, ?UNLINK_ID, Remote, Dead}, Controller),
    save("before-any-drain.etf", {both_put_results_observed, stat(Handle),
        process_info(Controller, [status, current_function])}),
    Expected = [{3, Dead, Remote, noproc}, {36, ?UNLINK_ID, Dead, Remote}],
    save("expected-controls.etf", Expected),
    drain(required_replies, Controller, Expected),
    save("after-drain.etf", stat(Handle));

scenario(producer_death_then_writer, Controller, Handle) ->
    fill(Handle),
    Owner = self(),
    {Producer, Monitor} = child(fun() -> producer(Owner, cancelled_producer) end),
    receive {producer_ready, Producer} -> ok
    after ?DEADLINE -> error(producer_ready_timeout) end,
    Producer ! go,
    Suspended = await_suspended(Producer),
    save("producer-suspended.etf", {Producer, Suspended, stat(Handle)}),
    exit(Producer, kill),
    Down = receive D = {'DOWN', Monitor, process, Producer, killed} -> D
           after ?DEADLINE -> error(producer_down_timeout) end,
    save("producer-down-before-drain.etf", {Down, is_process_alive(Producer),
        process_info(Producer), stat(Handle)}),
    {Writer, WriterMonitor} = child(fun() -> producer(Owner, surviving_writer) end),
    receive {producer_ready, Writer} -> ok
    after ?DEADLINE -> error(writer_ready_timeout) end,
    Writer ! go,
    save("next-writer-suspended.etf", {Writer, await_suspended(Writer), stat(Handle)}),
    drain(release_capacity, Controller, []),
    Sent = receive S = {producer_sent, Writer, surviving_writer, _} -> S
           after ?DEADLINE -> error(next_writer_send_timeout) end,
    save("next-writer-sent.etf", Sent),
    receive {'DOWN', WriterMonitor, process, Writer, normal} -> ok
    after ?DEADLINE -> error(next_writer_down_timeout) end,
    drain(after_writer_return, Controller, []),
    save("after-drain.etf", stat(Handle));

scenario(terminal_public_free, Controller, Handle) ->
    Owner = self(),
    {Target, Monitor} = child(fun() -> target(Owner) end),
    Remote = remote_pid(19),
    input(live_link, {1, Remote, Target}, Controller),
    Links = await_link(Target, Remote),
    save("established-link.etf", {Target, Remote, Links}),
    fill(Handle),
    Target ! exit_now,
    Free = await_free(Target),
    save("public-free-before-drain.etf", {Target, Free, process_info(Target), stat(Handle)}),
    Down = receive D = {'DOWN', Monitor, process, Target, pressure_terminal_reason} -> D
           after ?DEADLINE -> error(local_down_before_drain_timeout) end,
    save("local-down-before-drain.etf", {Down, stat(Handle)}),
    Expected = [{3, Target, Remote, pressure_terminal_reason}],
    save("expected-controls.etf", Expected),
    drain(terminal_exit, Controller, Expected),
    save("after-drain.etf", stat(Handle)).

fill(Handle) ->
    Payload = {pressure_fill, binary:copy(<<0, 128, 255, 1>>, 16384)},
    save("fill-payload.etf", Payload),
    save("before-fill.etf", stat(Handle)),
    Count = fill(Payload, 0),
    save("busy-barrier.etf", {send_nosuspend_refused, Count, stat(Handle)}).

fill(_Payload, ?MAX_FILL) -> error(busy_threshold_not_reached_within_bound);
fill(Payload, Count) ->
    case erlang:send({pressure_sink, ?REMOTE}, Payload, [nosuspend]) of
        ok -> fill(Payload, Count + 1);
        nosuspend -> Count
    end.

producer(Owner, Marker) ->
    Owner ! {producer_ready, self()},
    receive go -> ok after ?DEADLINE -> error(producer_go_timeout) end,
    Result = erlang:send({pressure_sink, ?REMOTE}, {pressure_marker, Marker}),
    Owner ! {producer_sent, self(), Marker, Result}.

target(Owner) ->
    receive
        inspect_links ->
            Owner ! {target_links, self(), process_info(self(), links)},
            target(Owner);
        exit_now -> exit(pressure_terminal_reason)
    after ?DEADLINE * 4 -> error(target_command_timeout) end.

await_suspended(Pid) -> poll(fun() ->
    Info = process_info(Pid, [status, current_function, current_stacktrace]),
    case Info of
        [{status, suspended} | _] -> {ready, Info};
        undefined -> error({producer_died_before_suspension, Pid});
        _ -> again
    end
end).

await_free(Pid) -> poll(fun() ->
    case is_process_alive(Pid) of false -> {ready, false}; true -> again end
end).

await_link(Pid, Remote) -> poll(fun() ->
    Pid ! inspect_links,
    receive
        {target_links, Pid, {links, Links} = Info} ->
            case lists:member(Remote, Links) of true -> {ready, Info}; false -> again end
    after 100 -> again
    end
end).

poll(Fun) -> poll(Fun, erlang:monotonic_time(millisecond) + ?DEADLINE).
poll(Fun, Deadline) ->
    case Fun() of
        {ready, Value} -> Value;
        again ->
            case erlang:monotonic_time(millisecond) < Deadline of
                true -> receive after 1 -> poll(Fun, Deadline) end;
                false -> error(controlled_barrier_timeout)
            end
    end.

input(Name, Control, Controller) ->
    Packet = packet(Control),
    Prefix = atom_to_list(Name),
    save(Prefix ++ "-control.etf", Control),
    save_bytes(Prefix ++ ".packet", Packet),
    Request = make_ref(),
    Controller ! {input, Request, Packet},
    Reply = receive {put_result, Request, Result, Before, After} -> {Result, Before, After}
            after ?DEADLINE -> error({put_return_before_drain_timeout, Name}) end,
    save(Prefix ++ "-put.etf", Reply),
    {{result, ok}, _, _} = Reply.

drain(Name, Controller, Expected) ->
    Request = make_ref(),
    Controller ! {drain, Request, Expected},
    Rows = receive {drained, Request, Value} -> Value
           after ?DEADLINE * 2 -> error({drain_timeout, Name}) end,
    save(atom_to_list(Name) ++ "-drain.etf", Rows).

controller(Owner) ->
    Handle = erlang:setnode(?REMOTE, self(), {flags(), 1}),
    ok = erlang:dist_ctrl_input_handler(Handle, self()),
    _ = erlang:dist_ctrl_set_opt(Handle, get_size, true),
    Owner ! {ready, self(), Handle},
    controller_loop(Owner, Handle).

controller_loop(Owner, Handle) ->
    receive
        {input, Request, Packet} ->
            Before = stat(Handle),
            Result = outcome(fun() -> erlang:dist_ctrl_put_data(Handle, Packet) end),
            Owner ! {put_result, Request, Result, Before, stat(Handle)},
            controller_loop(Owner, Handle);
        {drain, Request, Expected} ->
            Rows = drain_loop(Handle, Expected, [], 0,
                erlang:monotonic_time(millisecond) + ?DEADLINE),
            Owner ! {drained, Request, Rows},
            controller_loop(Owner, Handle);
        stop -> ok
    end.

drain_loop(_Handle, _Expected, _Rows, ?MAX_DRAIN, _Deadline) -> error(drain_packet_bound);
drain_loop(Handle, Expected, Rows, Count, Deadline) ->
    case erlang:dist_ctrl_get_data(Handle) of
        none when Expected == [] -> lists:reverse(Rows);
        none ->
            case erlang:monotonic_time(millisecond) < Deadline of
                false -> error({expected_control_not_observed, Expected});
                true ->
                    ok = erlang:dist_ctrl_get_data_notification(Handle),
                    receive dist_data -> drain_loop(Handle, Expected, Rows, Count, Deadline)
                    after 100 -> drain_loop(Handle, Expected, Rows, Count, Deadline)
                    end
            end;
        {Size, Segments} = Raw ->
            Bytes = iolist_to_binary(Segments),
            <<112, Terms/binary>> = Bytes,
            {Control, Used} = binary_to_term(Terms, [used]),
            <<_:Used/binary, Tail/binary>> = Terms,
            Row = {Count, Raw, Size, byte_size(Bytes), Bytes, Control, Tail, stat(Handle)},
            drain_loop(Handle, lists:delete(Control, Expected), [Row | Rows], Count + 1, Deadline)
    end.

packet(Control) -> Bytes = term_to_binary(Control), <<112, Bytes/binary>>.
remote_pid(Number) ->
    <<131, Atom/binary>> = term_to_binary(?REMOTE),
    binary_to_term(<<131, 88, Atom/binary, Number:32, 0:32, 1:32>>).
stat(Handle) -> outcome(fun() -> erlang:dist_get_stat(Handle) end).
outcome(Fun) -> try {result, Fun()} catch Class:Reason:Stack -> {exception, Class, Reason, Stack} end.

child(Fun) ->
    Pair = spawn_monitor(Fun),
    put(children, [Pair | get(children)]),
    Pair.

cleanup() ->
    Outcomes = lists:map(fun({Pid, Monitor}) ->
        case is_process_alive(Pid) of
            false -> erlang:demonitor(Monitor, [flush]), {Pid, already_not_alive};
            true ->
                exit(Pid, kill),
                receive {'DOWN', Monitor, process, Pid, Reason} -> {Pid, killed_cleanup, Reason}
                after ?DEADLINE -> {Pid, cleanup_timeout} end
        end
    end, get(children)),
    save("cleanup.etf", Outcomes).

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32).

save(Name, Term) -> save_bytes(Name, term_to_binary(Term)).
save_bytes(Name, Bytes) -> ok = file:write_file(filename:join(get(output), Name), Bytes).
