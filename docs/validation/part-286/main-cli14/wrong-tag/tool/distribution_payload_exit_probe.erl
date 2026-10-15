-module(distribution_payload_exit_probe).
-export([run/2]).

%% Independent fresh VM per case. Semantic outcomes are raw observations.
run(Case, Output) ->
    put(output, Output),
    try
        setup(Case),
        save("outer.etf", {result, ok}),
        save("stage.etf", {complete, Case})
    catch Class:Reason:Stack ->
        save("outer.etf", {exception, Class, Reason, Stack}),
        save("stage.etf", {failed, Case})
    end.

setup(Case) ->
    false = erlang:is_alive(),
    true = register(net_kernel, self()),
    true = erlang:setnode(payload_local@localhost, 1),
    Owner = self(),
    Pending = erts_internal:new_connection(payload_remote@localhost),
    {Controller, Monitor} = spawn_monitor(fun() -> controller(Owner) end),
    try
        Handle = receive {ready, Controller, H} -> H;
                         {'DOWN', Monitor, process, Controller, EarlyReason} ->
                             put(controller_down, EarlyReason), error({early_controller_down, EarlyReason})
                 after 5000 -> error(controller_ready_timeout) end,
        Notice = receive N = {dist_ctrlr, Controller, payload_remote@localhost, Controller} -> N
                 after 5000 -> error(kernel_notice_timeout) end,
        save("setup.etf", {Owner, Controller, Pending, Handle, Notice, flags(), node(), erlang:system_info(creation), Case}),
        scenario(Case, Owner, Controller, Monitor, Handle)
    after cleanup(Controller, Monitor, controller_down, "controller-cleanup.etf") end.

scenario(Case, Owner, Controller, ControllerMonitor, Handle) ->
    {Receiver, ReceiverMonitor} = spawn_monitor(fun() -> receiver(Owner, Case) end),
    try
        Ready = receive R = {ready_receiver, Receiver} -> R
                after 5000 -> error(receiver_ready_timeout) end,
        save("receiver-ready.etf", Ready),
        Remote = pid(payload_remote@localhost, 1, 1),
        Target = case Case of
                     dead_link_truncated -> pid(node(), 900000, erlang:system_info(creation));
                     dead_explicit_truncated -> pid(node(), 900000, erlang:system_info(creation));
                     _ -> Receiver
                 end,
        case Case of
            linked_trap -> link_input(Controller, ControllerMonitor, Remote, Receiver);
            linked_trace_trap -> link_input(Controller, ControllerMonitor, Remote, Receiver);
            linked_live_truncated -> link_input(Controller, ControllerMonitor, Remote, Receiver);
            _ -> save("link-input-result.etf", not_applicable)
        end,
        save("links-before.etf", process_info(Receiver, links)),
        save("receiver-before.etf", snapshot(Receiver)),
        {Control, ReasonBytes} = control(Case, Remote, Target),
        Packet = <<112, (term_to_binary(Control))/binary, ReasonBytes/binary>>,
        save("input-control.etf", Control),
        save_bytes("input.packet", Packet),
        save("input-result.etf", input(Controller, ControllerMonitor, Packet)),
        Selected = receive S = {selected, Receiver, _, _, _, _} -> S;
                           {'DOWN', ReceiverMonitor, process, Receiver, ReceiverReason} ->
                               put(receiver_down, ReceiverReason), {receiver_down, ReceiverReason};
                           {'DOWN', ControllerMonitor, process, Controller, ControllerReason} ->
                               put(controller_down, ControllerReason), {controller_down, ControllerReason}
                   after 500 -> observation_timeout end,
        save("selected.etf", Selected),
        save("receiver-after.etf", snapshot(Receiver)),
        save("links-after.etf", process_info(Receiver, links)),
        save("controller-after.etf", {is_process_alive(Controller), process_info(Controller, status)}),
        save("stats-after.etf", capture(fun() -> erlang:dist_get_stat(Handle) end)),
        save("identities.etf", {Owner, Controller, Receiver, Remote, Target})
    after cleanup(Receiver, ReceiverMonitor, receiver_down, "receiver-cleanup.etf") end.

receiver(Owner, Case) ->
    case Case of
        linked_trap -> process_flag(trap_exit, true);
        linked_trace_trap -> process_flag(trap_exit, true);
        explicit_trace_trap -> process_flag(trap_exit, true);
        explicit_kill -> process_flag(trap_exit, true);
        trailing_reason -> process_flag(trap_exit, true);
        _ -> ok
    end,
    _ = erlang:seq_trace(label, initial_payload_label),
    Owner ! {ready_receiver, self()},
    receive Message = {'EXIT', _, _} ->
        Label = erlang:seq_trace_info(label),
        Serial = erlang:seq_trace_info(serial),
        Token = erlang:seq_trace(sequential_trace_token, []),
        Owner ! {selected, self(), Message, Label, Serial, Token}
    end.

control(Case, Remote, Target) ->
    Token = {2, payload_label, 9, Remote, 3},
    Opcode = case Case of
                 linked_trace_trap -> 25;
                 explicit_trace_trap -> 27;
                 linked_trap -> 24;
                 dead_link_truncated -> 24;
                 unlinked_live_truncated -> 24;
                 linked_live_truncated -> 24;
                 _ -> 26
             end,
    Control = case Opcode of
                  25 -> {25, Remote, Target, Token};
                  27 -> {27, Remote, Target, Token};
                  _ -> {Opcode, Remote, Target}
              end,
    Reason = case Case of
                 explicit_normal -> normal;
                 explicit_kill -> kill;
                 _ -> {payload_reason, <<0, 128, 255>>, [a | binary_tail]}
             end,
    Bytes = case Case of
                dead_link_truncated -> <<131>>;
                dead_explicit_truncated -> <<131>>;
                unlinked_live_truncated -> <<131>>;
                linked_live_truncated -> <<131>>;
                explicit_live_truncated -> <<131>>;
                trailing_reason -> <<(term_to_binary(Reason))/binary, 0>>;
                _ -> term_to_binary(Reason)
            end,
    {Control, Bytes}.

link_input(Controller, Monitor, Remote, Target) ->
    save("link-input-result.etf", input(Controller, Monitor, <<112, (term_to_binary({1, Remote, Target}))/binary>>)).

controller(Owner) ->
    Handle = erlang:setnode(payload_remote@localhost, self(), {flags(), 1}),
    ok = erlang:dist_ctrl_input_handler(Handle, self()),
    Owner ! {ready, self(), Handle},
    loop(Owner, Handle).

loop(Owner, Handle) ->
    receive
        {input, Tag, Packet} ->
            Owner ! {input_result, Tag, capture(fun() -> erlang:dist_ctrl_put_data(Handle, Packet) end)},
            loop(Owner, Handle);
        stop -> ok
    end.

input(Controller, Monitor, Packet) ->
    Tag = make_ref(),
    Controller ! {input, Tag, Packet},
    receive {input_result, Tag, Result} -> Result;
            {'DOWN', Monitor, process, Controller, Reason} ->
                put(controller_down, Reason), {controller_down, Reason}
    after 5000 -> input_result_timeout end.

snapshot(Pid) -> capture(fun() -> {process_info(Pid, status), process_info(Pid, messages),
                                process_info(Pid, sequential_trace_token), process_info(Pid, trap_exit)} end).

capture(Fun) -> try {result, Fun()} catch Class:Reason:Stack -> {exception, Class, Reason, Stack} end.

pid(Node, Number, Creation) ->
    Name = atom_to_binary(Node, utf8),
    binary_to_term(<<131, 88, 119, (byte_size(Name)), Name/binary, Number:32, 0:32, Creation:32>>).

cleanup(Pid, Monitor, Key, Name) ->
    case erase(Key) of
        undefined ->
            Pid ! stop,
            receive {'DOWN', Monitor, process, Pid, Reason} -> save(Name, Reason)
            after 100 ->
                exit(Pid, kill),
                receive {'DOWN', Monitor, process, Pid, ForcedReason} -> save(Name, {forced, ForcedReason})
                after 5000 -> save(Name, cleanup_timeout) end
            end;
        ObservedReason -> save(Name, ObservedReason)
    end.

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32) bor 16#400000.

save(Name, Term) -> save_bytes(Name, term_to_binary(Term)).
save_bytes(Name, Bytes) -> ok = file:write_file(filename:join(get(output), Name), Bytes).
