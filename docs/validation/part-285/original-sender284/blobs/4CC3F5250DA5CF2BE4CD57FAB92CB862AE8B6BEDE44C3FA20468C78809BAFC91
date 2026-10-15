-module(distribution_sender_observer).
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
    true = erlang:setnode(sender_local@localhost, 1),
    Extra = scenario_flags(Scenario),
    Pending = erts_internal:new_connection(sender_remote@localhost),
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
        Notice = receive Message = {dist_ctrlr, Controller, sender_remote@localhost, Controller} -> Message
                 after ?DEADLINE -> error(kernel_notice_timeout) end,
        save("identities.etf", {Owner, Controller, Pending, Handle, Notice, flags() bor Extra, Scenario}),
        observe_senders(Scenario),
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

observe_senders(Scenario) ->
    Owner = self(),
    {Target, TargetMonitor} = spawn_monitor(fun() -> receiving_target(Owner) end),
    Remote = remote_pid(sender_remote@localhost, 11, 1),
    Third = remote_pid(sender_third@localhost, 13, 7),
    Token = case Scenario of
        sender_plain -> [];
        sender_trace ->
            seq_trace:set_token(label, 42),
            seq_trace:set_token(send, true),
            ObservedToken = seq_trace:get_token(),
            seq_trace:reset_trace(),
            ObservedToken
    end,
    record(generated_trace_token, Token),
    lists:foreach(fun({Tag, Sender}) ->
        Payload = {sender_payload, Tag, <<0, 255, 128>>},
        Control = case Scenario of
            sender_plain -> {22, Sender, Target};
            sender_trace -> {23, Sender, Target, Token}
        end,
        input_payload(Control, Payload),
        record(delivery, receive {received, Target, Message, ReceivedToken} ->
                            {observed, Tag, Message, ReceivedToken}
                         after ?DEADLINE -> {unobserved, Tag, timeout} end)
    end, [{remote, Remote}, {third_node, Third}, {current_local, self()}]),
    Target ! stop,
    record(target_down, receive {'DOWN', TargetMonitor, process, Target, TargetReason} -> {observed, TargetReason}
                       after ?DEADLINE -> {unobserved, timeout} end),
    DeadControl = case Scenario of
        sender_plain -> {22, Remote, Target};
        sender_trace -> {23, Remote, Target, Token}
    end,
    input_payload(DeadControl, {dead_target, <<1, 2>>}),
    drain(dead_target_output),
    record(controller_stat, capture(fun() -> erlang:dist_get_stat(get(handle)) end)).

receiving_target(Owner) ->
    receive
        stop -> ok;
        Message ->
            Token = seq_trace:get_token(),
            seq_trace:reset_trace(),
            Owner ! {received, self(), Message, Token},
            receiving_target(Owner)
    end.

input_payload(Control, Payload) ->
    Bytes = <<?PASS_THROUGH, (term_to_binary(Control))/binary, (term_to_binary(Payload))/binary>>,
    Request = make_ref(),
    get(controller) ! {input, Request, Bytes},
    Result = receive {input_result, Request, Value} -> Value
             after ?DEADLINE -> {unobserved, timeout} end,
    record(input, {Control, Payload, Bytes, Result}).

controller(Owner, Flags) ->
    Handle = erlang:setnode(sender_remote@localhost, self(), {Flags, 1}),
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

scenario_flags(sender_plain) -> 0;
scenario_flags(sender_trace) -> 16#80000.

flags() ->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor
    16#800 bor 16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor
    16#2000000 bor (16#04 bsl 32).

save(Name, Term) -> ok = file:write_file(filename:join(get(output), Name), term_to_binary(Term)).
