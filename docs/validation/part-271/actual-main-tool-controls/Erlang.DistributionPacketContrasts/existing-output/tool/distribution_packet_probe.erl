-module(distribution_packet_probe).
-export([run/1]).

run(Output) ->
    put(output, Output),
    false = erlang:is_alive(),
    nonode@nohost = node(),
    true = register(net_kernel, self()),
    true = erlang:setnode(packet_local@localhost, 1),
    Owner = self(),
    {Controller, Monitor} = spawn_monitor(fun() -> controller(Owner) end),
    try
        Handle = receive {ready, Controller, H} -> H after 5000 -> error(controller_timeout) end,
        save("inputs.etf", term_to_binary({packet_remote@localhost, flags(), 1})),
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
        true = register(packet_sink, self()),
        Registered = {6, self(), '', packet_sink},
        RegisteredPacket = packet(Registered, Payload),
        save("incoming-reg-send-control.etf", term_to_binary(Registered)),
        save("incoming-reg-send.packet", RegisteredPacket),
        Controller ! {input, RegisteredPacket},
        receive {input_result, RegisteredResult} -> save("incoming-reg-send-result.etf", term_to_binary(RegisteredResult))
        after 5000 -> error(input_result_timeout) end,
        receive Payload -> save("incoming-reg-send-delivered.etf", term_to_binary(Payload))
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
        Controller ! stop,
        receive {'DOWN', Monitor, process, Controller, Reason} ->
            save("cleanup-controller.etf", term_to_binary(Reason))
        after 5000 -> exit(Controller, kill),
                      receive {'DOWN', Monitor, process, Controller, Reason} ->
                          save("cleanup-controller.etf", term_to_binary({forced, Reason}))
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
