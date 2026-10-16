-module(connected_spawn_probe).
-export([run/2, child/1]).

%% Fixed source observer, using the existing distribution-controller collector pattern.
%% Each case needs its own pinned original VM. No SDK output is an expected baseline.
run(Case, Output) ->
    put(output, Output),
    try setup(Case), save("stage.etf", {complete, Case}), save("outer.etf", {result, ok})
    catch Class:Reason:Stack ->
        save("outer.etf", {exception, Class, Reason, Stack}), save("stage.etf", {failed, Case})
    end.

setup(Case) ->
    false = erlang:is_alive(),
    true = register(net_kernel, self()),
    true = erlang:setnode(spawn_local@localhost, 1),
    Pending = erts_internal:new_connection(spawn_remote@localhost),
    Parent = self(),
    {Controller, CM} = spawn_monitor(fun() -> controller(Parent) end),
    {Tracer, TM} = spawn_monitor(fun() -> tracer([]) end),
    try
        Handle = receive {ready, Controller, H} -> H after 5000 -> error(controller_ready_timeout) end,
        Notice = receive N = {dist_ctrlr, Controller, spawn_remote@localhost, Controller} -> N
                 after 5000 -> error(controller_notice_timeout) end,
        save("setup.etf", {Case, Parent, Controller, Pending, Handle, Notice, flags()}),
        save("tracer-setting.etf", erlang:system_flag(sequential_tracer, Tracer)),
        case Case of
            inbound_plain -> inbound(Controller, false, false);
            inbound_trace -> inbound(Controller, true, false);
            inbound_badopt_trace -> inbound(Controller, true, true);
            outbound_plain -> outbound(Controller, false, false);
            outbound_trace -> outbound(Controller, true, false);
            outbound_monitor_options -> outbound(Controller, false, true);
            alias_input_explicit -> alias_input(Controller, explicit_unalias, false);
            alias_input_demonitor -> alias_input(Controller, demonitor, false);
            alias_input_reply_demonitor -> alias_input(Controller, reply_demonitor, false);
            alias_input_trace -> alias_input(Controller, explicit_unalias, true);
            alias_output_send2 -> alias_output(Controller, false);
            alias_output_trace_send3 -> alias_output(Controller, true)
        end,
        save("trace-events.etf", rpc(Tracer, {snapshot, case get(trace_expectations) of undefined -> []; Expected -> Expected end}))
    after
        erlang:system_flag(sequential_tracer, false),
        cleanup(Tracer, TM, "tracer-cleanup.etf"),
        cleanup(Controller, CM, "controller-cleanup.etf")
    end.

inbound(Controller, Trace, BadOptions) ->
    Reference = remote_ref(), Parent = remote_pid(), Leader = group_leader(),
    Mfa = {?MODULE, child, 1},
    Options = case BadOptions of true -> [unsupported_creator_option]; false -> [monitor] end,
    Token = {3, 77, 12, Parent, 0},
    Control = case Trace of
        true -> {30, Reference, Parent, Leader, Mfa, Options, Token};
        false -> {29, Reference, Parent, Leader, Mfa, Options}
    end,
    Payload = [self()],
    Packet = <<112, (term_to_binary(Control))/binary, (term_to_binary(Payload))/binary>>,
    save("request-control.etf", Control), save("request-arguments.etf", Payload),
    save_bytes("request.packet", Packet), save("input-result.etf", rpc(Controller, {input, Packet})),
    ReplyPacket = take(Controller), save_bytes("reply.packet", ReplyPacket),
    {DecodedReply, no_payload} = decode(ReplyPacket),
    save("reply-decoded.etf", {DecodedReply, no_payload}),
    ReplyResult = element(5, DecodedReply),
    case Trace of
        true -> Header = {spawn_request, Reference, Parent, Leader, Mfa, Options},
                ReplyMessage = {spawn_reply, Reference, case BadOptions of true -> error; false -> ok end, ReplyResult},
                Core = [{'receive', Header}, {send, ReplyMessage}],
                put(trace_expectations, case BadOptions of true -> Core; false -> Core ++ [{'receive', Payload}] end);
        false -> ok
    end,
    case BadOptions of
        true -> save("child-state.etf", no_child_expected);
        false ->
            {Child, ChildMonitor} = receive {ready_child, ReadyChild, State} ->
                save("child-state.etf", State),
                {ReadyChild, monitor(process, ReadyChild)}
            after 5000 -> error(child_ready_timeout) end,
            Child ! finish,
            save_bytes("child-monitor-exit.packet", take(Controller)),
            receive {'DOWN', ChildMonitor, process, Child, ChildReason} ->
                save("child-cleanup.etf", {observed, ChildReason})
            after 5000 -> error(child_cleanup_timeout) end
    end.

outbound(Controller, Trace, MonitorOptions) ->
    case Trace of
        true -> seq_trace:set_token(send, true), seq_trace:set_token('receive', true),
                seq_trace:set_token(label, 77), seq_trace:set_token(serial, {0, 10});
        false -> ok
    end,
    Options = case MonitorOptions of
        true -> [{monitor, [priority, {tag, custom_down}, {alias, explicit_unalias}]}];
        false -> []
    end,
    Ref = spawn_request(spawn_remote@localhost, erlang, self, [], Options),
    Packet = take(Controller), {Control, Payload} = decode(Packet),
    save_bytes("request.packet", Packet), save("request-control.etf", Control), save("request-arguments.etf", Payload),
    RemoteChild = remote_pid(), Flags = case MonitorOptions of true -> 2; false -> 0 end,
    Reply = case Trace of
        true -> Token = element(7, Control), {32, Ref, self(), Flags, RemoteChild,
                    setelement(5, setelement(4, Token, RemoteChild), 11)};
        false -> {31, Ref, self(), Flags, RemoteChild}
    end,
    ReplyPacket = <<112, (term_to_binary(Reply))/binary>>,
    save_bytes("reply.packet", ReplyPacket), save("reply-control.etf", Reply),
    save("reply-input-result.etf", rpc(Controller, {input, ReplyPacket})),
    Received = receive R = {spawn_reply, Ref, _, _} -> R after 5000 -> reply_timeout end,
    save("spawn-reply.etf", Received), save("parent-token.etf", seq_trace:get_token()),
    save("parent-state.etf", process_info(self(), [links, monitors, priority_messages])),
    case Trace of
        true -> put(trace_expectations, [
                    {send, {spawn_request, Ref, self(), element(4,Control), element(5,Control), element(6,Control)}},
                    {send, Payload},
                    {'receive', {spawn_reply, Ref, ok, RemoteChild}}]);
        false -> ok
    end,
    case MonitorOptions of
        true -> DownPacket = <<112, (term_to_binary({21, RemoteChild, self(), Ref, remote_done}))/binary>>,
                save_bytes("down.packet", DownPacket),
                save("down-input-result.etf", rpc(Controller, {input, DownPacket})),
                Down = receive D = {custom_down, Ref, process, RemoteChild, _} -> D after 5000 -> down_timeout end,
                save("down.etf", Down), save("alias-removal.etf", unalias(Ref));
        false -> ok
    end.

child(Parent) ->
    Parent ! {ready_child, self(), {self(), group_leader(),
        process_info(self(), [initial_call, links, sequential_trace_token]), seq_trace:get_token()}},
    receive finish -> ok end.

controller(Parent) ->
    Handle = erlang:setnode(spawn_remote@localhost, self(), {flags(), 1}),
    ok = erlang:dist_ctrl_input_handler(Handle, self()),
    Parent ! {ready, self(), Handle},
    controller_loop(Handle).

controller_loop(Handle) ->
    receive
        {rpc, From, Ref, {input, Packet}} ->
            Result = try erlang:dist_ctrl_put_data(Handle, Packet)
                     catch C:R:S -> {exception, C, R, S} end,
            From ! {rpc_reply, Ref, Result}, controller_loop(Handle);
        {rpc, From, Ref, take} ->
            From ! {rpc_reply, Ref, erlang:dist_ctrl_get_data(Handle)}, controller_loop(Handle);
        stop -> ok
    end.

tracer(Events) -> tracer(Events, none).

tracer(Events, Pending) ->
    receive
        {rpc, From, Ref, {snapshot, Expected}} ->
            tracer_reply(Events, {From, Ref, Expected});
        stop -> ok;
        Event -> tracer_reply([Event | Events], Pending)
    end.

tracer_reply(Events, none) -> tracer(Events, none);
tracer_reply(Events, {From, Ref, Expected}=Pending) ->
    Complete = lists:all(fun({Kind, Message}) ->
        lists:any(fun
            ({seq_trace, 77, {K, _Serial, _Sender, _Receiver, M}}) -> K =:= Kind andalso M =:= Message;
            (_) -> false
        end, Events)
    end, Expected),
    case Complete of
        true -> From ! {rpc_reply, Ref, lists:reverse(Events)}, tracer(Events, none);
        false -> tracer(Events, Pending)
    end.

rpc(Target, Request) ->
    Ref = make_ref(), Target ! {rpc, self(), Ref, Request},
    receive {rpc_reply, Ref, Value} -> Value after 5000 -> error(rpc_timeout) end.

take(Controller) -> take(Controller, erlang:monotonic_time(millisecond) + 5000).
take(Controller, Deadline) ->
    case rpc(Controller, take) of
        none ->
            case erlang:monotonic_time(millisecond) < Deadline of
                true -> receive after 1 -> take(Controller, Deadline) end;
                false -> error(output_timeout)
            end;
        Data -> iolist_to_binary(Data)
    end.

decode(<<112, Body/binary>>) ->
    {Control, Used} = binary_to_term(Body, [used]),
    case Body of
        <<_:Used/binary>> -> {Control, no_payload};
        <<_:Used/binary, Payload/binary>> -> {Control, binary_to_term(Payload)}
    end.

remote_pid() ->
    Name = <<"spawn_remote@localhost">>,
    binary_to_term(<<131, 88, 119, (byte_size(Name)), Name/binary, 11:32, 0:32, 1:32>>).

remote_ref() ->
    Name = <<"spawn_remote@localhost">>,
    binary_to_term(<<131, 90, 3:16, 119, (byte_size(Name)), Name/binary, 1:32, 1:32, 2:32, 3:32>>).

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor 16#800 bor
    16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor 16#2000000 bor
    (16#04 bsl 32) bor (16#01 bsl 32) bor (16#08 bsl 32) bor 16#08 bor 16#20.

cleanup(Pid, Ref, Name) ->
    Pid ! stop,
    receive {'DOWN', Ref, process, Pid, Reason} -> save(Name, {observed, Reason})
    after 5000 -> exit(Pid, kill),
        receive {'DOWN', Ref, process, Pid, Reason} -> save(Name, {forced, Reason})
        after 5000 -> save(Name, unobserved) end
    end.

save(Name, Value) -> save_bytes(Name, term_to_binary(Value)).
save_bytes(Name, Bytes) -> ok = file:write_file(filename:join(get(output), Name), Bytes).


alias_input(Controller, Mode, Trace) ->
    Child = spawn(fun() -> receive finish -> ok end end),
    CleanupMonitor = monitor(process, Child),
    Alias = monitor(process, Child, [{alias, Mode}]),
    Sender = remote_pid(),
    Token = {3,77,12,Sender,0},
    Control = case Trace of true -> {34,Sender,Alias,Token}; false -> {33,Sender,Alias} end,
    First = {alias_first,Mode,Trace},
    Packet = <<112,(term_to_binary(Control))/binary,(term_to_binary(First))/binary>>,
    save("alias-identities.etf", {Mode,Trace,Child,Alias,CleanupMonitor,Sender}),
    save("alias-state-before.etf", process_info(self(),monitors)),
    save_bytes("alias-first.packet", Packet),
    save("alias-first-input.etf", rpc(Controller,{input,Packet})),
    ObservedFirst = receive First -> First after 5000 -> alias_first_timeout end,
    save("alias-first-selected.etf", ObservedFirst),
    save("alias-token.etf", seq_trace:get_token()),
    save("alias-state-selected.etf", process_info(self(),monitors)),
    save("alias-demonitor.etf", demonitor(Alias,[info])),
    Second = {alias_after_demonitor,Mode,Trace},
    SecondPacket = <<112,(term_to_binary(Control))/binary,(term_to_binary(Second))/binary>>,
    save_bytes("alias-second.packet",SecondPacket),
    save("alias-second-input.etf",rpc(Controller,{input,SecondPacket})),
    save("alias-second-selected.etf",receive Second -> Second after 0 -> no_message end),
    save("alias-unalias.etf",unalias(Alias)),
    Third = {alias_after_unalias,Mode,Trace},
    ThirdPacket = <<112,(term_to_binary(Control))/binary,(term_to_binary(Third))/binary>>,
    save_bytes("alias-third.packet",ThirdPacket),
    save("alias-third-input.etf",rpc(Controller,{input,ThirdPacket})),
    save("alias-third-selected.etf",receive Third -> Third after 0 -> no_message end),
    Child ! finish,
    receive {'DOWN',CleanupMonitor,process,Child,ChildReason} ->
        save("child-cleanup.etf",{observed,ChildReason})
    after 5000 -> error(child_cleanup_timeout) end,
    save("alias-extra-down.etf",receive {'DOWN',Alias,process,Child,R} -> {unexpected_down,R} after 0 -> no_message end),
    case Trace of true -> put(trace_expectations,[{'receive',First}]); false -> ok end.

alias_output(Controller, Trace) ->
    Alias = remote_ref(),
    Payload = {outgoing_alias_payload,Trace},
    case Trace of
        true -> seq_trace:set_token(send,true),seq_trace:set_token('receive',true),
                seq_trace:set_token(label,77),seq_trace:set_token(serial,{0,10});
        false -> ok
    end,
    Result = case Trace of
        true -> erlang:send(Alias,Payload,[noconnect]);
        false -> erlang:send(Alias,Payload)
    end,
    save("alias-send-result.etf",Result),save("alias-target.etf",Alias),
    Packet = take(Controller),save_bytes("alias-output.packet",Packet),
    save("alias-output-decoded.etf",decode(Packet)),
    case Trace of true -> put(trace_expectations,[{send,Payload}]); false -> ok end.
