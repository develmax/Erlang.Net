-module(distribution_trace_probe).
-export([run/2]).

%% Source-only observer. Each scenario requires a separate fresh pinned VM.
%% All semantic values are captured without expected-value assertions.
run(Case, Output) ->
    put(output, Output),
    try
        setup(Case),
        save("stage.etf", {complete, Case}),
        save("outer.etf", {result, ok})
    catch Class:Reason:Stack ->
        save("outer.etf", {exception, Class, Reason, Stack}),
        save("stage.etf", {failed, Case})
    end.

setup(Case) ->
    false = erlang:is_alive(),
    true = register(net_kernel, self()),
    true = erlang:setnode(trace_local@localhost, 1),
    Owner = self(),
    Pending = erts_internal:new_connection(trace_remote@localhost),
    {Controller, ControllerMonitor} = spawn_monitor(fun() -> controller(Owner) end),
    try
        Handle = receive {ready, Controller, H} -> H
                 after 5000 -> error(controller_ready_timeout) end,
        Notice = receive N = {dist_ctrlr, Controller, trace_remote@localhost, Controller} -> N
                 after 5000 -> error(kernel_notice_timeout) end,
        save("setup.etf", {Owner, Controller, Pending, Handle, Notice,
                           node(), erlang:system_info(creation), flags(), Case}),
        scenario(Case, Owner, Controller)
    after
        Controller ! stop,
        receive {'DOWN', ControllerMonitor, process, Controller, ControllerReason} ->
            save("controller-cleanup.etf", ControllerReason)
        after 5000 ->
            exit(Controller, kill),
            receive {'DOWN', ControllerMonitor, process, Controller, ForcedControllerReason} ->
                save("controller-cleanup.etf", {forced, ForcedControllerReason})
            after 5000 -> save("controller-cleanup.etf", cleanup_timeout)
            end
        end
    end.

scenario(Case, Owner, Controller) ->
    {Receiver, ReceiverMonitor} = spawn_monitor(fun() -> receiver(Owner, Case) end),
    {Tracer, TracerMonitor} = spawn_monitor(fun tracer/0),
    try
        Ready = receive R = {receiver_ready, Receiver} -> R
                after 5000 -> error(receiver_ready_timeout) end,
        save("receiver-ready.etf", Ready),
        save("tracer-setting.etf", erlang:system_flag(sequential_tracer, Tracer)),
        Remote = remote_pid(),
        case Case of
            exit_link ->
                input(Controller, <<112, (term_to_binary({1, Remote, Receiver}))/binary>>),
                save("links-before.etf", process_info(Receiver, links));
            _ -> save("links-before.etf", process_info(Receiver, links))
        end,
        true = erlang:suspend_process(Receiver),
        save("token-before.etf", process_info(Receiver, sequential_trace_token)),
        {Control, Payload, HasPayload} = frame_input(Case, Remote, Receiver),
        Packet = case HasPayload of
                     true -> packet(Control, Payload);
                     false -> <<112, (term_to_binary(Control))/binary>>
                 end,
        save("input.etf", {Control, Payload, HasPayload}),
        save_bytes("input.packet", Packet),
        save("input-result.etf", input(Controller, Packet)),
        save("queued.etf", {process_info(Receiver, sequential_trace_token),
                            process_info(Receiver, messages), process_info(Receiver, status)}),
        true = erlang:resume_process(Receiver),
        case Case of
            exit2_normal ->
                save("ignored-exit-state.etf", {process_info(Receiver, sequential_trace_token),
                                               process_info(Receiver, status)}),
                save("completion-input.etf", input(Controller, packet({2, '', Receiver}, selected_payload)));
            _ -> save("ignored-exit-state.etf", not_applicable),
                 save("completion-input.etf", not_applicable)
        end,
        Selected = receive S = {selected, Receiver, _, _, _, _} -> S;
                           {'DOWN', ReceiverMonitor, process, Receiver, EarlyReason} ->
                               put(receiver_down, EarlyReason), {receiver_died_before_report, EarlyReason}
                   after 5000 -> observation_timeout end,
        save("selected.etf", Selected),
        save("tracer-mailbox.etf", process_info(Tracer, messages)),
        save("receiver-identities.etf", {Receiver, Remote, Tracer, whereis(trace_recipient)})
    after
        _ = erlang:system_flag(sequential_tracer, false),
        _ = catch erlang:resume_process(Receiver),
        exit(Tracer, kill),
        receive {'DOWN', TracerMonitor, process, Tracer, TracerReason} ->
            save("tracer-cleanup.etf", TracerReason)
        after 5000 -> save("tracer-cleanup.etf", cleanup_timeout)
        end,
        case erase(receiver_down) of
            undefined ->
                receive {'DOWN', ReceiverMonitor, process, Receiver, ReceiverReason} ->
                    save("receiver-cleanup.etf", ReceiverReason)
                after 5000 ->
                    exit(Receiver, kill),
                    receive {'DOWN', ReceiverMonitor, process, Receiver, ForcedReceiverReason} ->
                        save("receiver-cleanup.etf", {forced, ForcedReceiverReason})
                    after 5000 -> save("receiver-cleanup.etf", cleanup_timeout) end
                end;
            ObservedReason -> save("receiver-cleanup.etf", ObservedReason)
        end
    end.

receiver(Owner, Case) ->
    case Case of registered -> true = register(trace_recipient, self()); _ -> ok end,
    case Case of exit_link -> process_flag(trap_exit, true); exit2_trap -> process_flag(trap_exit, true); _ -> ok end,
    _ = erlang:seq_trace(label, initial_label),
    Owner ! {receiver_ready, self()},
    receive
        selected_payload -> report(Owner, selected_payload);
        {'EXIT', Remote, selected_reason} -> report(Owner, {'EXIT', Remote, selected_reason})
    end.

report(Owner, Message) ->
    Label = erlang:seq_trace_info(label),
    Serial = erlang:seq_trace_info(serial),
    Token = erlang:seq_trace(sequential_trace_token, []),
    Owner ! {selected, self(), Message, Label, Serial, Token}.

tracer() -> receive stop -> ok end.

frame_input(Case, Remote, Receiver) ->
    Token = case Case of
                nil -> [];
                undefined -> undefined;
                wide_flags -> {1 bsl 40, incoming_label, 9, Remote, 3};
                exit2_normal -> arbitrary_unasserted_token;
                _ -> {2, incoming_label, 9, Remote, 3}
            end,
    case Case of
        registered -> {{16, Remote, '', trace_recipient, Token}, selected_payload, true};
        exit_link -> {{13, Remote, Receiver, Token, selected_reason}, no_payload, false};
        exit2_trap -> {{18, Remote, Receiver, Token, selected_reason}, no_payload, false};
        exit2_normal -> {{18, Remote, Receiver, Token, normal}, no_payload, false};
        _ -> {{12, '', Receiver, Token}, selected_payload, true}
    end.

controller(Owner) ->
    Handle = erlang:setnode(trace_remote@localhost, self(), {flags(), 1}),
    ok = erlang:dist_ctrl_input_handler(Handle, self()),
    Owner ! {ready, self(), Handle},
    controller_loop(Owner, Handle).

controller_loop(Owner, Handle) ->
    receive
        {input, Tag, Packet} ->
            Result = try erlang:dist_ctrl_put_data(Handle, Packet)
                     catch Class:Reason:Stack -> {exception, Class, Reason, Stack} end,
            Owner ! {input_result, Tag, Result},
            controller_loop(Owner, Handle);
        stop -> ok
    end.

input(Controller, Packet) ->
    Tag = make_ref(),
    Controller ! {input, Tag, Packet},
    receive {input_result, Tag, Result} -> Result
    after 5000 -> input_timeout end.

remote_pid() ->
    Name = <<"trace_remote@localhost">>,
    binary_to_term(<<131, 88, 119, (byte_size(Name)), Name/binary, 1:32, 0:32, 1:32>>).

packet(Control, Payload) ->
    <<112, (term_to_binary(Control))/binary, (term_to_binary(Payload))/binary>>.

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32).

save(Name, Term) -> save_bytes(Name, term_to_binary(Term)).
save_bytes(Name, Bytes) -> ok = file:write_file(filename:join(get(output), Name), Bytes).
