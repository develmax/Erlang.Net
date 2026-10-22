-module(gen_sys_observer391).
-export([run/0, run/1, run_stacks/0, run_stacks/1, worker/2, cases/1]).

run() -> run(primary).

run(Group) ->
    {Child, Monitor} = erlang:spawn_monitor(?MODULE, worker, [self(), Group]),
    receive
        {Child, Values} ->
            receive
                {'DOWN', Monitor, process, Child, normal} -> Values;
                {'DOWN', Monitor, process, Child, Reason} -> erlang:error({worker_failure, Reason})
            after 5000 -> erlang:error(worker_not_settled)
            end;
        {'DOWN', Monitor, process, Child, Reason} -> erlang:error({worker_failure, Reason})
    after 30000 ->
        erlang:exit(Child, kill),
        receive {'DOWN', Monitor, process, Child, _} -> erlang:error(worker_timeout)
        after 5000 -> erlang:error(worker_unsettled)
        end
    end.

worker(Owner, Group) ->
    Values = rows(cases(Group)),
    Owner ! {self(), Values},
    ok.

rows([]) -> [];
rows([Case | Rest]) -> [{Case, observe(Case)} | rows(Rest)].

run_stacks() -> run_stacks(primary).

run_stacks(Group) ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, run(Group)},
        Zero = at_depth(0, Group),
        One = at_depth(1, Group),
        Two = at_depth(2, Group),
        Eight = at_depth(8, Group),
        [Default, Zero, One, Two, Eight]
    after erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth, Group) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run(Group)}.

observe(Case) ->
    try value(Case) of Value -> {returned, Value}
    catch Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

cases(primary) ->
    [name_local, name_global, name_via, name_invalid,
     hibernate_default, hibernate_option, debug_default, debug_trace,
     parent_missing, parent_identity, proc_name_identity, proc_name_missing,
     unregister_missing, reply_legacy, reply_ignored_bad_target,
     call_default, call_infinite, call_no_process, call_invalid_timeout, call_timeout,
     send_wait, send_receive, send_check, response_no_match, response_bad_id,
     wait_timeout, receive_timeout, wait_collection_empty, receive_collection_empty,
     check_collection_empty, request_new, request_add_identity, request_bad_size,
     request_bad_add, request_bad_list, request_collection_delete,
     request_collection_keep, status_identity, status_bad_return, status_crash,
     status_unknown_key, status_legacy, status_legacy_throw,
     sys_state_default, sys_state_timeout_arg, sys_state_bad_return, sys_state_crash,
     sys_state_no_process, sys_replace_default, sys_replace_timeout_arg,
     sys_suspend_resume, sys_change_running, sys_change_suspended, sys_change_crash,
     sys_log, sys_trace, sys_no_debug, sys_install_remove, sys_statistics_absent,
     sys_statistics_disable, sys_log_file_disable, sys_terminate,
     sys_options, sys_options_invalid, sys_log_direct, sys_log_empty,
     sys_debug_lookup, sys_debug_default, sys_print_empty];
cases(startup) -> [start_unnamed, start_named, start_monitor];
cases(all) -> cases(primary) ++ cases(startup).

value(name_local) -> gen:name({local, stable_name});
value(name_global) -> gen:name({global, stable_name});
value(name_via) -> gen:name({via, unavailable_registry391, stable_name});
value(name_invalid) -> gen:name(invalid);
value(hibernate_default) -> gen:hibernate_after([]);
value(hibernate_option) -> gen:hibernate_after([{hibernate_after, 42}]);
value(debug_default) -> gen:debug_options(stable_name, []);
value(debug_trace) -> gen:debug_options(stable_name, [{debug, [trace]}]);
value(parent_missing) -> erase('$ancestors'), gen:get_parent();
value(parent_identity) ->
    put('$ancestors', [self()]),
    try gen:get_parent() =:= self() after erase('$ancestors') end;
value(proc_name_identity) -> gen:get_proc_name(self()) =:= self();
value(proc_name_missing) -> gen:get_proc_name({local, gen_sys_missing391});
value(unregister_missing) -> gen:unregister_name({local, gen_sys_missing391});
value(reply_legacy) ->
    ok = gen:reply({self(), stable_tag}, stable_reply),
    receive {stable_tag, Reply} -> Reply after 1000 -> erlang:error(reply_timeout) end;
value(reply_ignored_bad_target) -> gen:reply({42, stable_tag}, stable_reply);
value(call_default) -> with_server(normal, fun() -> gen:call(gen_sys_target391, '$gen_call', {add, 3}) end);
value(call_infinite) -> with_server(normal, fun() -> gen:call(gen_sys_target391, '$gen_call', get, infinity) end);
value(call_no_process) -> gen:call(gen_sys_missing391, '$gen_call', get);
value(call_invalid_timeout) -> gen:call(gen_sys_missing391, '$gen_call', get, invalid);
value(call_timeout) -> with_server(normal, fun() -> gen:call(gen_sys_target391, '$gen_call', no_reply, 0) end);
value(send_wait) -> with_server(normal, fun() ->
    Request = gen:send_request(gen_sys_target391, '$gen_call', {add, 2}),
    gen:wait_response(Request, infinity)
end);
value(send_receive) -> with_server(normal, fun() ->
    Request = gen:send_request(gen_sys_target391, '$gen_call', get),
    gen:receive_response(Request, infinity)
end);
value(send_check) -> with_server(normal, fun() ->
    Request = gen:send_request(gen_sys_target391, '$gen_call', get),
    receive {[alias | Request], _} = Message -> gen:check_response(Message, Request)
    after 1000 -> erlang:error(response_timeout)
    end
end);
value(response_no_match) -> gen:check_response(unrelated, make_ref());
value(response_bad_id) -> gen:check_response(unrelated, invalid);
value(wait_timeout) -> gen:wait_response(make_ref(), 0);
value(receive_timeout) -> gen:receive_response(make_ref(), 0);
value(wait_collection_empty) -> gen:wait_response(gen:reqids_new(), 0, true);
value(receive_collection_empty) -> gen:receive_response(gen:reqids_new(), 0, false);
value(check_collection_empty) -> gen:check_response(unrelated, gen:reqids_new(), true);
value(request_new) -> gen:reqids_new();
value(request_add_identity) ->
    Reference = make_ref(),
    Collection = gen:reqids_add(Reference, label, gen:reqids_new()),
    {gen:reqids_size(Collection), gen:reqids_to_list(Collection) =:= [{Reference, label}]};
value(request_bad_size) -> gen:reqids_size(invalid);
value(request_bad_add) -> gen:reqids_add(invalid, label, #{});
value(request_bad_list) -> gen:reqids_to_list(invalid);
value(request_collection_delete) -> with_server(normal, fun() ->
    Collection = gen:send_request(gen_sys_target391, '$gen_call', get, label, gen:reqids_new()),
    gen:receive_response(Collection, infinity, true)
end);
value(request_collection_keep) -> with_server(normal, fun() ->
    Collection = gen:send_request(gen_sys_target391, '$gen_call', get, label, gen:reqids_new()),
    {Reply, Label, Remaining} = gen:wait_response(Collection, infinity, false),
    {Reply, Label, gen:reqids_size(Remaining), Remaining =:= Collection}
end);
value(status_identity) -> gen:format_status(gen_status_callback391, normal, #{mode => identity, state => 7}, undefined);
value(status_bad_return) -> gen:format_status(gen_status_callback391, normal, #{mode => bad_return, state => 7}, undefined);
value(status_crash) -> gen:format_status(gen_status_callback391, normal, #{mode => crash, state => 7}, undefined);
value(status_unknown_key) -> gen:format_status(gen_status_callback391, normal, #{mode => unknown_key, state => 7}, undefined);
value(status_legacy) -> gen:format_status(gen_status_legacy391, normal, #{state => 7}, [7]);
value(status_legacy_throw) -> gen:format_status(gen_status_legacy391, throw_value, #{state => 7}, [7]);
value(sys_state_default) -> with_server(normal, fun() -> sys:get_state(gen_sys_target391) end);
value(sys_state_timeout_arg) -> with_server(normal, fun() -> sys:get_state(gen_sys_target391, infinity) end);
value(sys_state_bad_return) -> with_server(bad_get, fun() -> sys:get_state(gen_sys_target391) end);
value(sys_state_crash) -> with_server(crash_get, fun() -> sys:get_state(gen_sys_target391, infinity) end);
value(sys_state_no_process) -> sys:get_state(gen_sys_missing391);
value(sys_replace_default) -> with_server(normal, fun() ->
    New = sys:replace_state(gen_sys_target391, fun(X) -> X + 4 end),
    {New, sys:get_state(gen_sys_target391)}
end);
value(sys_replace_timeout_arg) -> with_server(normal, fun() ->
    New = sys:replace_state(gen_sys_target391, fun(X) -> X + 4 end, infinity),
    {New, sys:get_state(gen_sys_target391)}
end);
value(sys_suspend_resume) -> with_server(normal, fun() ->
    First = sys:suspend(gen_sys_target391),
    During = sys:get_state(gen_sys_target391),
    Second = sys:resume(gen_sys_target391, infinity),
    {First, During, Second, gen:call(gen_sys_target391, '$gen_call', get)}
end);
value(sys_change_running) -> with_server(normal, fun() -> sys:change_code(gen_sys_target391, gen_sys_server391, old, 3) end);
value(sys_change_suspended) -> with_server(normal, fun() ->
    ok = sys:suspend(gen_sys_target391, infinity),
    Result = sys:change_code(gen_sys_target391, gen_sys_server391, old, 3, infinity),
    ok = sys:resume(gen_sys_target391),
    {Result, sys:get_state(gen_sys_target391)}
end);
value(sys_change_crash) -> with_server(normal, fun() ->
    ok = sys:suspend(gen_sys_target391),
    Result = sys:change_code(gen_sys_target391, gen_sys_server391, old, crash),
    ok = sys:resume(gen_sys_target391),
    Result
end);
value(sys_log) -> with_server(normal, fun() ->
    ok = sys:log(gen_sys_target391, true),
    _ = gen:call(gen_sys_target391, '$gen_call', get),
    Log = sys:log(gen_sys_target391, get, infinity),
    ok = sys:log(gen_sys_target391, false),
    Log
end);
value(sys_trace) -> with_server(normal, fun() ->
    First = sys:trace(gen_sys_target391, true),
    _ = gen:call(gen_sys_target391, '$gen_call', get),
    {First, sys:trace(gen_sys_target391, false, infinity)}
end);
value(sys_no_debug) -> with_server(normal, fun() -> {sys:no_debug(gen_sys_target391), sys:no_debug(gen_sys_target391, infinity)} end);
value(sys_install_remove) -> with_server(normal, fun() ->
    A = sys:install(gen_sys_target391, {stable_id, fun gen_sys_server391:debug_event/3, 0}),
    _ = gen:call(gen_sys_target391, '$gen_call', get),
    {A, sys:remove(gen_sys_target391, stable_id, infinity)}
end);
value(sys_statistics_absent) -> with_server(normal, fun() -> sys:statistics(gen_sys_target391, get) end);
value(sys_statistics_disable) -> with_server(normal, fun() -> sys:statistics(gen_sys_target391, false, infinity) end);
value(sys_log_file_disable) -> with_server(normal, fun() -> {sys:log_to_file(gen_sys_target391, false), sys:log_to_file(gen_sys_target391, false, infinity)} end);
value(sys_terminate) -> with_server(normal, fun() -> sys:terminate(gen_sys_target391, normal, infinity) end);
value(sys_options) -> sys:debug_options([trace, {log, 2}, ignored]);
value(sys_options_invalid) -> sys:debug_options(invalid);
value(sys_log_direct) ->
    Debug = sys:debug_options([{log, 2}]),
    A = sys:handle_debug(Debug, fun gen_sys_server391:format_event/3, state, event1),
    B = sys:handle_debug(A, fun gen_sys_server391:format_event/3, state, event2),
    C = sys:handle_debug(B, fun gen_sys_server391:format_event/3, state, event3),
    sys:get_log(C);
value(sys_log_empty) -> sys:get_log([]);
value(sys_debug_lookup) -> sys:get_debug(log, [{log, stable_data}], absent);
value(sys_debug_default) -> sys:get_debug(log, [], absent);
value(sys_print_empty) -> sys:print_log([]);
value(start_unnamed) -> with_started(unnamed, fun() -> gen:call(gen_sys_target391, '$gen_call', get) end);
value(start_named) -> with_started(named, fun() -> gen:call(gen_sys_target391, '$gen_call', get) end);
value(start_monitor) -> with_started(monitor, fun() -> gen:call(gen_sys_target391, '$gen_call', get) end).

with_server(Mode, Fun) ->
    {Child, Monitor} = erlang:spawn_monitor(gen_sys_server391, start, [self(), Mode]),
    receive
        {server_ready, Child} -> execute_owned(Child, Monitor, Fun);
        {'DOWN', Monitor, process, Child, Reason} -> erlang:error({server_not_started, Reason})
    after 5000 ->
        erlang:exit(Child, kill),
        settle(Child, Monitor),
        erlang:error(server_start_timeout)
    end.

with_started(Kind, Fun) ->
    Started = case Kind of
        unnamed -> gen:start(gen_sys_server391, nolink, gen_sys_server391, {normal, 0}, []);
        named -> gen:start(gen_sys_server391, nolink, {local, gen_sys_target391}, gen_sys_server391, {normal, 0}, []);
        monitor -> gen:start(gen_sys_server391, monitor, {local, gen_sys_target391}, gen_sys_server391, {normal, 0}, [])
    end,
    {Child, Monitor} = case Started of
        {ok, {Pid, Reference}} -> {Pid, Reference};
        {ok, Pid} -> {Pid, erlang:monitor(process, Pid)}
    end,
    execute_owned(Child, Monitor, Fun).

execute_owned(Child, Monitor, Fun) ->
    try Fun()
    after
        erlang:exit(Child, kill),
        settle(Child, Monitor)
    end.

settle(Child, Monitor) ->
    receive {'DOWN', Monitor, process, Child, _} -> ok
    after 5000 -> erlang:error(server_unsettled)
    end.
