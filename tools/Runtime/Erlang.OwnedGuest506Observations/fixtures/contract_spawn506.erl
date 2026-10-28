-module(contract_spawn506).
-export([run/0, run/1]).

capture(Fun) ->
    try Fun() of Value -> {returned, Value}
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.

info(Pid) ->
    capture(fun() -> erlang:process_info(Pid,
        [heap_size, total_heap_size, stack_size, min_heap_size, min_bin_vheap_size,
         max_heap_size, fullsweep_after, garbage_collection, priority,
         message_queue_data, async_dist, links, monitors]) end).

run() -> run(no_remote_peer).

run(Peer) ->
    true = register(contract_controller506, self()),
    OldTrap = process_flag(trap_exit, true),
    LogToken = make_ref(),
    Logger = capture(fun() -> logger:add_handler(contract_log506, contract_logger506,
        #{level => all, config => #{owner => self(), token => LogToken}}) end),
    put(contract_tracked506, []),
    try
        Cases = contract_cases506:options() ++ extra_options(),
        Observed = [{Facade, Id, isolated(Facade, Id, Opts, Effect, Peer)} ||
            Facade <- contract_cases506:facades(), {Id, Opts, Effect} <- expand(Cases)],
        {#{caller => self(), node => node(), peer => Peer, group_leader => group_leader()},
         Logger, Observed, drain_logs(LogToken)}
    after
        cleanup_tracked(),
        capture(fun() -> logger:remove_handler(contract_log506) end),
        erase(contract_tracked506),
        unregister(contract_controller506),
        process_flag(trap_exit, OldTrap)
    end.

isolated(Facade, Id, Opts, Effect, Peer) ->
    Observation = capture(fun() -> one(Facade, Id, Opts, Effect, Peer) end),
    Cleanup = capture(fun cleanup_tracked/0),
    case Cleanup of {returned, _} -> put(contract_tracked506, []); _ -> ok end,
    {Observation, Cleanup}.

expand(Cases) ->
    [case Case of {Id, Opts} -> {Id, Opts, none}; Triple -> Triple end || Case <- Cases].

extra_options() ->
    [{link, [link]}, {link_monitor, [link, monitor]},
     {monitor_priority, [{monitor, [priority, {tag, contract_down506}]}]},
     {request_reply_no, [{reply, no}]},
     {request_reply_error_only, [{reply, error_only}]},
     {request_reply_success_only, [{reply, success_only}]},
     {request_reply_tag, [{reply_tag, contract_spawn_reply506}]},
     {heap_kill_effect, [{max_heap_size, #{size => 4096, kill => true, error_logger => false}}], list},
     {heap_no_kill_effect, [{max_heap_size, #{size => 4096, kill => false, error_logger => false}}], list},
     {heap_log_effect, [{max_heap_size, #{size => 4096, kill => false, error_logger => true}}], list},
     {bin_excluded_effect, [{max_heap_size, #{size => 4096, kill => true, error_logger => false,
                                            include_shared_binaries => false}}], binary},
     {bin_included_effect, [{max_heap_size, #{size => 4096, kill => true, error_logger => false,
                                            include_shared_binaries => true}}], binary}].

one(Facade, Id, Opts, Effect, Peer) ->
    case runnable(Facade, Id, Peer) of
        {false, Why} -> {not_executed, Why};
        true ->
            Token = make_ref(),
            Launch = capture(fun() -> launch(Facade, Opts, Token, Peer) end),
            case Launch of
                {raised, _, _, _} -> {#{case_token => Token}, Launch};
                {returned, Return} ->
                    Found = identify(Return, Token),
                    case Found of
                        {child, Pid, Reply, ReturnedRef} ->
                            Mon = monitor(process, Pid),
                            put(contract_tracked506, [{Pid, Mon, ReturnedRef}|get(contract_tracked506)]),
                            Observed = capture(fun() ->
                                Ready = case Reply of
                                    {startup_ready, Existing} -> Existing;
                                    {ready_before_reply, Existing} -> Existing;
                                    _ -> wait_ready(Pid, Token, Mon)
                                end,
                                Before = info(Pid),
                                Work = capture(fun() -> effect(Pid, Mon, Effect) end),
                                {Ready, Before, Work, info(Pid)}
                            end),
                            Settled = cleanup(Pid, Mon, ReturnedRef),
                            put(contract_tracked506, lists:keydelete(Pid, 1, get(contract_tracked506))),
                            {#{case_token => Token, child => Pid, cleanup_monitor => Mon,
                               returned_ref => ReturnedRef}, Launch, Reply, Observed, Settled};
                        Other -> {#{case_token => Token}, Launch, Other}
                    end
            end
    end.

runnable(Facade, Id, Peer) ->
    NoOptions = lists:member(Facade, [erlang_spawn_request_2_node_fun,
        erlang_spawn_request_3_mfa, erlang_spawn_request_4_node_mfa]),
    Remote = lists:member(Facade, [erlang_spawn_opt_3_remote_node,
        erlang_spawn_opt_5_remote_node, erlang_spawn_request_5_remote_node]),
    case {NoOptions andalso Id =/= baseline,
          Remote andalso (Peer =:= no_remote_peer orelse Peer =:= node() orelse not is_atom(Peer))} of
        {true, _} -> {false, facade_has_no_options_argument};
        {_, true} -> {false, real_remote_peer_required};
        _ -> true
    end.

launch(Facade, Opts, Token, Peer) ->
    Parent = self(),
    Fun = fun() -> contract_spawn_subject506:held(Parent, Token, []) end,
    Args = [Parent, Token, []],
    case Facade of
        erlang_spawn_opt_2_fun -> erlang:spawn_opt(Fun, Opts);
        erlang_spawn_opt_2_tuple -> erlang:spawn_opt({contract_spawn_subject506, entry0}, Opts);
        erlang_spawn_opt_3_local_node -> erlang:spawn_opt(node(), Fun, Opts);
        erlang_spawn_opt_3_remote_node -> erlang:spawn_opt(Peer, Fun, Opts);
        erlang_spawn_opt_4_mfa -> erlang:spawn_opt(contract_spawn_subject506, held, Args, Opts);
        erlang_spawn_opt_5_local_node -> erlang:spawn_opt(node(), contract_spawn_subject506, held, Args, Opts);
        erlang_spawn_opt_5_remote_node -> erlang:spawn_opt(Peer, contract_spawn_subject506, held, Args, Opts);
        erlang_spawn_request_2_fun_options -> erlang:spawn_request(Fun, Opts);
        erlang_spawn_request_2_node_fun -> erlang:spawn_request(node(), Fun);
        erlang_spawn_request_3_node_fun_options -> erlang:spawn_request(node(), Fun, Opts);
        erlang_spawn_request_3_mfa -> erlang:spawn_request(contract_spawn_subject506, held, Args);
        erlang_spawn_request_4_node_mfa -> erlang:spawn_request(node(), contract_spawn_subject506, held, Args);
        erlang_spawn_request_4_mfa_options -> erlang:spawn_request(contract_spawn_subject506, held, Args, Opts);
        erlang_spawn_request_5_local_node -> erlang:spawn_request(node(), contract_spawn_subject506, held, Args, Opts);
        erlang_spawn_request_5_remote_node -> erlang:spawn_request(Peer, contract_spawn_subject506, held, Args, Opts);
        proc_spawn_opt_2_fun -> proc_lib:spawn_opt(Fun, Opts);
        proc_spawn_opt_3_node_fun -> proc_lib:spawn_opt(node(), Fun, Opts);
        proc_spawn_opt_4_mfa -> proc_lib:spawn_opt(contract_spawn_subject506, held, Args, Opts);
        proc_spawn_opt_5_node_mfa -> proc_lib:spawn_opt(node(), contract_spawn_subject506, held, Args, Opts);
        proc_start_5 -> proc_lib:start(contract_spawn_subject506, start_ack, [Parent, Token, {started, Token}], 5000, Opts);
        proc_start_link_5 -> proc_lib:start_link(contract_spawn_subject506, start_ack, [Parent, Token, {started, Token}], 5000, Opts);
        proc_start_monitor_5 -> proc_lib:start_monitor(contract_spawn_subject506, start_ack, [Parent, Token, {started, Token}], 5000, Opts)
    end.

identify(Pid, _) when is_pid(Pid) -> {child, Pid, no_reply, none};
identify({Pid, Ref}, _) when is_pid(Pid), is_reference(Ref) -> {child, Pid, no_reply, Ref};
identify({{started, Token}, Ref}, Token) when is_reference(Ref) ->
    identify_started(Token, Ref);
identify({started, Token}, Token) -> identify_started(Token, none);
identify(Ref, Token) when is_reference(Ref) ->
    receive
        {_Tag, Ref, ok, Pid} = Reply when is_pid(Pid) -> {child, Pid, Reply, Ref};
        {_Tag, Ref, error, Reason} = Reply -> {request_error, Reply, Reason};
        {held, Token, Pid} = Ready -> {child, Pid, {ready_before_reply, Ready}, Ref}
    after 5000 ->
        Abandon = capture(fun() -> erlang:spawn_request_abandon(Ref) end),
        error({unresolved_request, Ref, Token, Abandon})
    end;
identify(Return, _) -> {other_return, Return}.

identify_started(Token, ReturnedRef) ->
    receive {held, Token, Pid} = Ready -> {child, Pid, {startup_ready, Ready}, ReturnedRef}
    after 5000 -> error({startup_ready_timeout, Token, ReturnedRef})
    end.

wait_ready(Pid, Token, Mon) ->
    receive
        {held, Token, Pid} = Ready -> Ready;
        {held, tuple_entry, Pid} = Ready -> Ready;
        {'DOWN', Mon, process, Pid, Reason} = Down ->
            put({contract_down506, Mon}, Down),
            {already_down, Down, Reason}
    after 5000 -> error({held_timeout, Token, Pid, Mon})
    end.

effect(_, _, none) -> not_requested;
effect(Pid, Mon, Kind) ->
    Ref = make_ref(),
    Pid ! {allocate, self(), Ref, Kind},
    receive
        {allocated, Ref, Pid, Hash} = Allocated ->
            GcRef = make_ref(),
            Pid ! {gc, self(), GcRef},
            receive
                {collected, GcRef, Pid, _} = Gc -> {Allocated, Gc, Hash};
                {'DOWN', Mon, process, Pid, _} = Down ->
                    put({contract_down506, Mon}, Down),
                    {Allocated, Down}
            after 5000 -> {gc_ack_timeout, GcRef}
            end;
        {'DOWN', Mon, process, Pid, _} = Down ->
            put({contract_down506, Mon}, Down),
            Down
    after 5000 -> {allocation_ack_timeout, Ref}
    end.

cleanup(Pid, Mon, ReturnedRef) ->
    unlink(Pid),
    exit(Pid, kill),
    Down = case erase({contract_down506, Mon}) of
        undefined -> receive {'DOWN', Mon, process, Pid, _} = Message -> Message
            after 1000 -> {down_not_received, is_process_alive(Pid)} end;
        Seen -> Seen
    end,
    case is_process_alive(Pid) of true -> error({unsettled_child, Pid, Mon}); false -> ok end,
    demonitor(Mon, [flush]),
    case is_reference(ReturnedRef) of true -> demonitor(ReturnedRef, [flush]); false -> ok end,
    receive {'EXIT', Pid, _} -> ok after 0 -> ok end,
    Replies = case is_reference(ReturnedRef) of
        true -> receive {_, ReturnedRef, _, _} = Reply -> [Reply] after 0 -> [] end;
        false -> []
    end,
    {Down, is_process_alive(Pid), Replies}.

cleanup_tracked() ->
    case get(contract_tracked506) of
        Entries when is_list(Entries) -> [cleanup(P, R, Extra) || {P,R,Extra} <- Entries];
        _ -> ok
    end.

drain_logs(Token) ->
    receive {contract_log506, Token, Event} -> [Event|drain_logs(Token)]
    after 0 -> []
    end.
