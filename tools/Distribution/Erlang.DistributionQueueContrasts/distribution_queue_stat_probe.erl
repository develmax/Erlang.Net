-module(distribution_queue_stat_probe).
-export([run/1]).

run(Output) ->
    put(output, Output),
    false = erlang:is_alive(),
    true = register(net_kernel, self()),
    true = erlang:setnode(queue_stat_local@localhost, 1),
    Owner = self(),
    Pending = erts_internal:new_connection(queue_stat_remote@localhost),
    save("pending.etf", Pending),
    {Controller, Monitor} = spawn_monitor(fun() -> controller(Owner) end),
    try
        Handle = receive
            {ready, Controller, H} -> H;
            {'DOWN', Monitor, process, Controller, ReadyReason} ->
                put(controller_down, ReadyReason),
                error({controller_before_ready, ReadyReason})
        after 5000 -> error(controller_timeout) end,
        save("handle.etf", Handle),
        receive Notice = {dist_ctrlr, Controller, queue_stat_remote@localhost, Controller} ->
            save("kernel-notice.etf", Notice)
        after 5000 -> error(kernel_notice_timeout) end,
        save("runtime.etf", {erlang:system_info(wordsize), erlang:system_info(build_type),
                             erlang:system_info(system_architecture), node(), erlang:is_alive()}),
        observe(small, {small, 17}, Controller, Handle),
        observe(large_binary, {large_binary, binary:copy(<<0, 128, 255, 1>>, 16384)}, Controller, Handle),
        observe(two_binaries, {two_binaries, binary:copy(<<2>>, 8192), binary:copy(<<3>>, 8192)}, Controller, Handle),
        ok = file:write_file(filename:join(Output, "stage"), <<"complete">>),
        ok
    after
        case erase(controller_down) of
            undefined ->
                Controller ! stop,
                receive {'DOWN', Monitor, process, Controller, CleanupReason} ->
                    save("cleanup.etf", CleanupReason)
                after 5000 -> exit(Controller, kill),
                    receive {'DOWN', Monitor, process, Controller, ForcedReason} ->
                        save("cleanup.etf", {forced, ForcedReason})
                    after 5000 -> error(cleanup_timeout) end
                end;
            ObservedReason -> save("cleanup.etf", {already_down, ObservedReason})
        end
    end.

observe(Name, Payload, Controller, Handle) ->
    Prefix = atom_to_list(Name),
    save(Prefix ++ "-payload.etf", Payload),
    save(Prefix ++ "-empty-stat.etf", erlang:dist_get_stat(Handle)),
    {queue_stat_sink, queue_stat_remote@localhost} ! Payload,
    save(Prefix ++ "-queued-stat.etf", erlang:dist_get_stat(Handle)),
    Controller ! {drain, Name},
    receive
        {drained, Name, PreviousOption, Data, AfterStat} ->
            save(Prefix ++ "-previous-option.etf", PreviousOption),
            save(Prefix ++ "-raw-data.etf", Data),
            save(Prefix ++ "-after-stat.etf", AfterStat),
            {WireSize, Segments} = Data,
            Bytes = iolist_to_binary(Segments),
            save(Prefix ++ "-wire-shape.etf", {WireSize, byte_size(Bytes), [byte_size(B) || B <- Segments]}),
            ok = file:write_file(filename:join(get(output), Prefix ++ ".packet"), Bytes),
            <<112, Terms/binary>> = Bytes,
            {Control, Used} = binary_to_term(Terms, [used]),
            <<_:Used/binary, MessageBytes/binary>> = Terms,
            Payload = binary_to_term(MessageBytes),
            {6, Owner, '', queue_stat_sink} = Control,
            Owner = self(),
            save(Prefix ++ "-control.etf", Control)
    after 5000 -> error({drain_timeout, Name}) end.

controller(Owner) ->
    Handle = erlang:setnode(queue_stat_remote@localhost, self(), {flags(), 1}),
    Owner ! {ready, self(), Handle},
    loop(Owner, Handle).

loop(Owner, Handle) ->
    receive
        {drain, Name} ->
            PreviousOption = erlang:dist_ctrl_set_opt(Handle, get_size, true),
            Data = erlang:dist_ctrl_get_data(Handle),
            Owner ! {drained, Name, PreviousOption, Data, erlang:dist_get_stat(Handle)},
            loop(Owner, Handle);
        stop -> ok
    end.

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32).

save(Name, Term) ->
    ok = file:write_file(filename:join(get(output), Name), term_to_binary(Term)).
