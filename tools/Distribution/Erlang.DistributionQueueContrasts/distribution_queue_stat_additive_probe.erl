-module(distribution_queue_stat_additive_probe).
-export([run/2]).

run(Case, Output) ->
    put(output, Output),
    false = erlang:is_alive(),
    true = register(net_kernel, self()),
    true = erlang:setnode(queue_additive_local@localhost, 1),
    save("case.etf", Case),
    save("profile.etf", {erlang:system_info(wordsize), erlang:system_info(build_type),
                         erlang:system_info(system_architecture), node(), erlang:is_alive()}),
    Pending = erts_internal:new_connection(queue_additive_remote@localhost),
    save("pending.etf", Pending),
    save("pending-stat.etf", outcome(fun() -> erlang:dist_get_stat(Pending) end)),
    {Controller, Monitor, Handle} = start_controller(first),
    put(controller, Controller),
    put(controller_monitor, Monitor),
    save("handle.etf", Handle),
    try
        run_case(Case, Controller, Monitor, Handle),
        save("stage", <<"complete">>),
        ok
    after
        cleanup(get(controller), get(controller_monitor))
    end.

run_case(two_buffers, Controller, _Monitor, Handle) ->
    First = {first, 17},
    Second = {second, binary:copy(<<0, 128, 255, 1>>, 16384)},
    save("first-payload.etf", First),
    save("second-payload.etf", Second),
    save("before-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    {queue_additive_sink, queue_additive_remote@localhost} ! First,
    {queue_additive_sink, queue_additive_remote@localhost} ! Second,
    save("both-queued-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    drain(first, First, Controller),
    save("after-first-owner-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    drain(second, Second, Controller),
    save("after-second-owner-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    Controller ! {drain, empty},
    receive
        {drained, empty, PreviousOption, Data, Stat} ->
            save("empty-previous-option.etf", PreviousOption),
            save("empty-data.etf", Data),
            save("empty-controller-stat.etf", Stat)
    after 5000 -> error(empty_drain_timeout) end,
    save("after-empty-owner-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end));

run_case(handle_domain, Controller, Monitor, Handle) ->
    save("nil-stat.etf", outcome(fun() -> erlang:dist_get_stat([]) end)),
    save("atom-stat.etf", outcome(fun() -> erlang:dist_get_stat(invalid_handle) end)),
    save("pid-stat.etf", outcome(fun() -> erlang:dist_get_stat(Controller) end)),
    save("owner-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    Controller ! stat,
    receive {stat, Stat} -> save("controller-stat.etf", Stat)
    after 5000 -> error(controller_stat_timeout) end,
    Owner = self(),
    {Third, ThirdMonitor} = spawn_monitor(fun() ->
        Owner ! {third_stat, self(), outcome(fun() -> erlang:dist_get_stat(Handle) end)}
    end),
    receive {third_stat, Third, ThirdStat} -> save("third-stat.etf", ThirdStat)
    after 5000 -> error(third_stat_timeout) end,
    receive {'DOWN', ThirdMonitor, process, Third, ThirdReason} -> save("third-cleanup.etf", ThirdReason)
    after 5000 -> error(third_cleanup_timeout) end,
    Controller ! stop,
    receive {'DOWN', Monitor, process, Controller, Reason} -> save("closed-reason.etf", Reason)
    after 5000 -> error(controller_close_timeout) end,
    put(controller, undefined),
    save("old-after-close-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    ReopenedPending = erts_internal:new_connection(queue_additive_remote@localhost),
    save("reopened-pending.etf", ReopenedPending),
    {NewController, NewMonitor, NewHandle} = start_controller(reopened),
    put(controller, NewController),
    put(controller_monitor, NewMonitor),
    save("reopened-handle.etf", NewHandle),
    save("old-after-reopen-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    save("new-after-reopen-stat.etf", outcome(fun() -> erlang:dist_get_stat(NewHandle) end));

run_case(input_counter, Controller, Monitor, Handle) ->
    Payload = {actual_input, 17},
    Packet = packet(self(), Payload),
    save("input-payload.etf", Payload),
    save("input.packet", Packet),
    save("before-input-owner-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    put_input(valid, Packet, Controller, Monitor),
    receive Payload -> save("delivery.etf", Payload)
    after 5000 -> error(input_delivery_timeout) end,
    save("after-input-owner-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    Controller ! stat,
    receive {stat, Stat} -> save("after-input-controller-stat.etf", Stat)
    after 5000 -> error(after_input_stat_timeout) end;

run_case(input_malformed, Controller, Monitor, Handle) ->
    Packet = <<112, 131, 104, 0>>,
    save("malformed.packet", Packet),
    save("before-input-owner-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    put_input(malformed, Packet, Controller, Monitor),
    save("after-input-owner-stat.etf", outcome(fun() -> erlang:dist_get_stat(Handle) end)),
    case get(controller) of
        undefined -> save("malformed-controller-down.etf", {down_before_reply, get(controller_down_reason)});
        Controller ->
            receive
                {'DOWN', Monitor, process, Controller, Reason} ->
                    save("malformed-controller-down.etf", {down, Reason}),
                    put(controller, undefined)
            after 100 -> save("malformed-controller-down.etf", not_observed_within_window)
            end
    end.

start_controller(Label) ->
    Owner = self(),
    {Controller, Monitor} = spawn_monitor(fun() ->
        Handle = erlang:setnode(queue_additive_remote@localhost, self(), {flags(), 1}),
        Owner ! {ready, self(), Handle},
        loop(Owner, Handle)
    end),
    Handle = receive
        {ready, Controller, H} -> H;
        {'DOWN', Monitor, process, Controller, ReadyReason} -> error({controller_before_ready, ReadyReason})
    after 5000 -> error(controller_ready_timeout) end,
    receive Notice = {dist_ctrlr, Controller, queue_additive_remote@localhost, Controller} ->
        save(atom_to_list(Label) ++ "-kernel-notice.etf", Notice)
    after 5000 -> error(kernel_notice_timeout) end,
    {Controller, Monitor, Handle}.

loop(Owner, Handle) ->
    receive
        {drain, Name} ->
            PreviousOption = outcome(fun() -> erlang:dist_ctrl_set_opt(Handle, get_size, true) end),
            Data = outcome(fun() -> erlang:dist_ctrl_get_data(Handle) end),
            Owner ! {drained, Name, PreviousOption, Data, outcome(fun() -> erlang:dist_get_stat(Handle) end)},
            loop(Owner, Handle);
        {put, Name, Packet} ->
            Before = outcome(fun() -> erlang:dist_get_stat(Handle) end),
            Owner ! {put_before, Name, Before},
            Put = outcome(fun() -> erlang:dist_ctrl_put_data(Handle, Packet) end),
            After = outcome(fun() -> erlang:dist_get_stat(Handle) end),
            Owner ! {put_result, Name, Put, After},
            loop(Owner, Handle);
        stat ->
            Owner ! {stat, outcome(fun() -> erlang:dist_get_stat(Handle) end)},
            loop(Owner, Handle);
        stop -> ok
    end.

drain(Name, Payload, Controller) ->
    Prefix = atom_to_list(Name),
    Controller ! {drain, Name},
    receive
        {drained, Name, PreviousOption, Data, Stat} ->
            save(Prefix ++ "-previous-option.etf", PreviousOption),
            save(Prefix ++ "-raw-data.etf", Data),
            save(Prefix ++ "-controller-stat.etf", Stat),
            {result, {WireSize, Segments}} = Data,
            Bytes = iolist_to_binary(Segments),
            save(Prefix ++ "-wire-shape.etf", {WireSize, byte_size(Bytes), [byte_size(B) || B <- Segments]}),
            save(Prefix ++ ".packet", Bytes),
            <<112, Terms/binary>> = Bytes,
            {Control, Used} = binary_to_term(Terms, [used]),
            <<_:Used/binary, MessageBytes/binary>> = Terms,
            Payload = binary_to_term(MessageBytes),
            {6, Owner, '', queue_additive_sink} = Control,
            Owner = self(),
            save(Prefix ++ "-control.etf", Control)
    after 5000 -> error({drain_timeout, Name}) end.

put_input(Name, Packet, Controller, Monitor) ->
    Controller ! {put, Name, Packet},
    receive
        {put_before, Name, Before} -> save("input-controller-before-stat.etf", Before)
    after 5000 -> error(input_before_stat_timeout) end,
    receive
        {put_result, Name, Put, After} ->
            save("input-put-outcome.etf", Put),
            save("input-controller-after-stat.etf", After);
        {'DOWN', Monitor, process, Controller, Reason} ->
            save("input-put-outcome.etf", {controller_down_before_reply, Reason}),
            save("input-controller-after-stat.etf", {not_observed, controller_down_before_reply}),
            put(controller_down_reason, Reason),
            put(controller, undefined)
    after 5000 -> error(input_put_timeout) end.

packet(Target, Payload) ->
    ControlBytes = term_to_binary({2, '', Target}),
    PayloadBytes = term_to_binary(Payload),
    <<112, ControlBytes/binary, PayloadBytes/binary>>.

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32).

outcome(Fun) ->
    try {result, Fun()}
    catch Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

cleanup(undefined, _Monitor) -> save("cleanup.etf", already_observed_down);
cleanup(Controller, Monitor) ->
    Controller ! stop,
    receive {'DOWN', Monitor, process, Controller, Reason} -> save("cleanup.etf", Reason)
    after 5000 -> exit(Controller, kill),
        receive {'DOWN', Monitor, process, Controller, ForcedReason} -> save("cleanup.etf", {forced, ForcedReason})
        after 5000 -> error(cleanup_timeout) end
    end.

save(Name, Term) when is_list(Name) ->
    case filename:extension(Name) of
        ".etf" -> save_bytes(Name, term_to_binary(Term));
        _ -> save_bytes(Name, Term)
    end.

save_bytes(Name, Bytes) -> ok = file:write_file(filename:join(get(output), Name), Bytes).
