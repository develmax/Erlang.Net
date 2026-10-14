-module(distribution_packet_probe).
-export([run/1]).

run(Output) ->
    put(output, Output),
    false = erlang:is_alive(),
    nonode@nohost = node(),
    true = register(net_kernel, self()),
    true = erlang:setnode(packet_local@localhost, 1),
    Owner = self(),
    Pending = erts_internal:new_connection(packet_remote@localhost),
    save("pending-handle.etf", term_to_binary(Pending)),
    {Controller, Monitor} = spawn_monitor(fun() -> controller(Owner) end),
    try
        Handle = receive
                     {ready, Controller, H} -> H;
                     {'DOWN', Monitor, process, Controller, ReadyReason} ->
                         put(packet_ready_down, {observed, ReadyReason}),
                         save("controller-ready-failure.etf", term_to_binary(ReadyReason)),
                         error({controller_died_before_ready, ReadyReason})
                 after 5000 -> error(controller_timeout) end,
        save("inputs.etf", term_to_binary({packet_remote@localhost, flags(), 1, Pending})),
        receive Notice = {dist_ctrlr, Controller, packet_remote@localhost, Controller} ->
            save("kernel-notice.etf", term_to_binary(Notice))
        after 5000 -> error(kernel_notice_timeout) end,
        Payload = {packet_payload, <<0, 128, 255>>, [a | binary_tail]},
        save("payload.etf", term_to_binary(Payload)),
        save("identities.etf", term_to_binary({Owner, Controller, node(), erlang:is_alive()})),
        Control = {2, '', self()},
        Packet = packet(Control, Payload),
        save("incoming-send-control.etf", term_to_binary(Control)),
        save("incoming-send.packet", Packet),
        Controller ! {input, Packet},
        receive {input_result, Result} -> save("incoming-send-result.etf", term_to_binary(Result))
        after 5000 -> error(input_result_timeout) end,
        receive Payload -> save("incoming-send-delivered.etf", term_to_binary(Payload))
        after 5000 -> error(send_delivery_timeout) end,
        Sink = start_sink(Owner),
        save("registered-sink-identities.etf", term_to_binary({Owner, Sink, whereis(net_kernel), whereis(packet_sink)})),
        Registered = {6, self(), '', packet_sink},
        RegisteredPacket = packet(Registered, Payload),
        save("incoming-reg-send-control.etf", term_to_binary(Registered)),
        save("incoming-reg-send.packet", RegisteredPacket),
        Controller ! {input, RegisteredPacket},
        receive {input_result, RegisteredResult} -> save("incoming-reg-send-result.etf", term_to_binary(RegisteredResult))
        after 5000 -> error(input_result_timeout) end,
        receive {sink_delivery, Sink, Payload} ->
            save("incoming-reg-send-delivered.etf", term_to_binary(Payload))
        after 5000 -> error(registered_delivery_timeout) end,
        {packet_sink, packet_remote@localhost} ! Payload,
        Controller ! drain,
        receive {output, Bytes} ->
            save("outgoing-reg-send.packet", Bytes),
            <<112, Terms/binary>> = Bytes,
            {OutgoingControl, Used} = binary_to_term(Terms, [used]),
            <<_:Used/binary, MessageBytes/binary>> = Terms,
            Payload = binary_to_term(MessageBytes),
            {6, Owner, '', packet_sink} = OutgoingControl,
            save("outgoing-reg-send-control.etf", term_to_binary(OutgoingControl))
        after 5000 -> error(output_timeout) end,
        save("stats.etf", term_to_binary(erlang:dist_get_stat(Handle))),
        save("stage", <<"complete">>),
        ok
    after
        try cleanup_sink()
        after cleanup_controller(Controller, Monitor) end
    end.

start_sink(Owner) ->
    {Sink, SinkMonitor} = spawn_monitor(fun() ->
        true = register(packet_sink, self()),
        Owner ! {sink_ready, self(), whereis(packet_sink)},
        sink_loop(Owner)
    end),
    put(packet_owned_sink, {Sink, SinkMonitor}),
    receive
        {sink_ready, Sink, Sink} -> Sink;
        {'DOWN', SinkMonitor, process, Sink, SinkReadyReason} ->
            put(packet_owned_sink_down, {observed, SinkReadyReason}),
            save("registered-sink-ready-failure.etf", term_to_binary(SinkReadyReason)),
            error({sink_died_before_ready, SinkReadyReason})
    after 5000 -> error(sink_ready_timeout) end.

sink_loop(Owner) ->
    receive
        stop -> ok;
        Message ->
            Owner ! {sink_delivery, self(), Message},
            sink_loop(Owner)
    end.

cleanup_sink() ->
    case erase(packet_owned_sink) of
        undefined -> ok;
        {Sink, SinkMonitor} ->
            case erase(packet_owned_sink_down) of
                {observed, ObservedSinkReason} ->
                    save("cleanup-sink.etf", term_to_binary(ObservedSinkReason));
                undefined ->
                    Sink ! stop,
                    receive {'DOWN', SinkMonitor, process, Sink, SinkCleanupReason} ->
                        save("cleanup-sink.etf", term_to_binary(SinkCleanupReason))
                    after 5000 -> exit(Sink, kill),
                                  receive {'DOWN', SinkMonitor, process, Sink, SinkForcedReason} ->
                                      save("cleanup-sink.etf", term_to_binary({forced, SinkForcedReason}))
                                  after 5000 -> error(sink_cleanup_timeout) end
                    end
            end
    end.

cleanup_controller(Controller, Monitor) ->
    case erase(packet_ready_down) of
        {observed, ObservedReason} ->
            save("cleanup-controller.etf", term_to_binary(ObservedReason));
        undefined ->
            Controller ! stop,
            receive {'DOWN', Monitor, process, Controller, CleanupReason} ->
                save("cleanup-controller.etf", term_to_binary(CleanupReason))
            after 5000 -> exit(Controller, kill),
                          receive {'DOWN', Monitor, process, Controller, ForcedReason} ->
                              save("cleanup-controller.etf", term_to_binary({forced, ForcedReason}))
                          after 5000 -> error(cleanup_timeout) end
            end
    end.

controller(Owner) ->
    Handle = erlang:setnode(packet_remote@localhost, self(), {flags(), 1}),
    ok = erlang:dist_ctrl_input_handler(Handle, self()),
    Owner ! {ready, self(), Handle},
    loop(Owner, Handle).

loop(Owner, Handle) ->
    receive
        {input, Bytes} ->
            Owner ! {input_result, erlang:dist_ctrl_put_data(Handle, Bytes)},
            loop(Owner, Handle);
        drain ->
            erlang:dist_ctrl_get_data_notification(Handle),
            receive dist_data -> ok after 5000 -> error(data_notice_timeout) end,
            Data = erlang:dist_ctrl_get_data(Handle),
            Owner ! {output, iolist_to_binary(Data)},
            loop(Owner, Handle);
        stop -> ok
    end.

packet(Control, Payload) ->
    <<112, (term_to_binary(Control))/binary, (term_to_binary(Payload))/binary>>.

flags() ->
    %% OTP-29.1.1 dist.h DFLAG_DIST_MANDATORY; no cache, fragments or sender flag.
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32).

save(Name, Bytes) -> ok = file:write_file(filename:join(get(output), Name), Bytes).
