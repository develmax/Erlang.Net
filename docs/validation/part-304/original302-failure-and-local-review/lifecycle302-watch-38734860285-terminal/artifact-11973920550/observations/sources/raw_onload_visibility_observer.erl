-module(raw_onload_visibility_observer).
-export([run/2, metadata/0]).

-define(MODULE_UNDER_TEST, raw_onload_visibility_fixture).
-define(DEADLINE, 5000).

metadata() ->
    {raw_onload_visibility_metadata,
        [{otp_release, erlang:system_info(otp_release)},
         {erts_version, erlang:system_info(version)},
         {system_version, erlang:system_info(system_version)},
         {system_architecture, erlang:system_info(system_architecture)},
         {wordsize, erlang:system_info(wordsize)},
         {build_type, erlang:system_info(build_type)},
         {emulator_type, erlang:system_info(emu_flavor)}]}.

run(Baseline, Operation) ->
    Old = baseline(Baseline),
    Before = snapshot(before_stage, none, Old),
    {ok, NewBytes} = file:read_file(os:getenv("RAW_NEW_BEAM")),
    Prepared = erlang:prepare_loading(?MODULE_UNDER_TEST, NewBytes),
    Stage = erlang:finish_loading([Prepared]),
    Staged = snapshot(staged, none, Old),
    Parent = self(),
    Pending = spawn_monitor(fun() ->
        Callback = capture(fun() -> erlang:call_on_load_function(?MODULE_UNDER_TEST) end),
        Parent ! {callback_finished, self(), Callback},
        holder_loop()
    end),
    try
        Callback = await_callback(Pending),
        AfterCallback = snapshot(after_callback, Pending, Old),
        Events = finalize(Operation, Pending, Old),
        {raw_onload_visibility_observation, Baseline, Operation,
            {before_stage, Before}, {stage_result, Stage}, {staged, Staged},
            {callback, Callback}, {after_callback, AfterCallback}, {events, Events}}
    after
        stop_holder(Pending),
        stop_holder(Old)
    end.

baseline(fresh) -> none;
baseline(Baseline) when Baseline =:= current; Baseline =:= deleted_old ->
    {ok, OldBytes} = file:read_file(os:getenv("RAW_OLD_BEAM")),
    {module, ?MODULE_UNDER_TEST} = erlang:load_module(?MODULE_UNDER_TEST, OldBytes),
    Parent = self(),
    Holder = spawn_monitor(fun() ->
        erlang:put(raw_visibility_held_fun, ?MODULE_UNDER_TEST:capture()),
        Parent ! {holder_ready, self()},
        holder_loop()
    end),
    {Pid, _} = Holder,
    receive {holder_ready, Pid} -> ok after ?DEADLINE -> erlang:error(old_holder_timeout) end,
    case Baseline of
        deleted_old -> true = erlang:delete_module(?MODULE_UNDER_TEST);
        current -> ok
    end,
    Holder.

await_callback({Pid, Monitor}) ->
    receive
        {callback_finished, Pid, Result} -> Result;
        {'DOWN', Monitor, process, Pid, Reason} ->
            erlang:put({raw_visibility_down, Pid}, {known, Reason}),
            {callback_holder_down, Reason}
    after ?DEADLINE -> erlang:error(callback_timeout)
    end.

finalize(direct_true, Pending, Old) ->
    [event(keep_true, capture(fun() -> erlang:finish_after_on_load(?MODULE_UNDER_TEST, true) end), Pending, Old)];
finalize(false_false_true, Pending, Old) ->
    First = event(false_first, capture(fun() -> erlang:finish_after_on_load(?MODULE_UNDER_TEST, false) end), Pending, Old),
    Second = event(false_second, capture(fun() -> erlang:finish_after_on_load(?MODULE_UNDER_TEST, false) end), Pending, Old),
    Last = event(keep_true, capture(fun() -> erlang:finish_after_on_load(?MODULE_UNDER_TEST, true) end), Pending, Old),
    [First, Second, Last];
finalize(wrapper_false, Pending, Old) ->
    [event(wrapper_false, capture(fun() -> erts_code_purger:finish_after_on_load(?MODULE_UNDER_TEST, false) end), Pending, Old)].

event(Name, Result, Pending, Old) ->
    {event, Name, {result, Result}, {snapshot, snapshot(Name, Pending, Old)}}.

snapshot(Name, Pending, Old) ->
    {snapshot, Name,
        {module_loaded, erlang:module_loaded(?MODULE_UNDER_TEST)},
        {old_code, erlang:check_old_code(?MODULE_UNDER_TEST)},
        {body_exported, erlang:function_exported(?MODULE_UNDER_TEST, body, 0)},
        {capture_exported, erlang:function_exported(?MODULE_UNDER_TEST, capture, 0)},
        {exports, capture(fun() -> erlang:get_module_info(?MODULE_UNDER_TEST, exports) end)},
        {body, capture(fun() -> ?MODULE_UNDER_TEST:body() end)},
        {pending_fun, probe_holder(Pending)},
        {old_fun, probe_holder(Old)}}.

probe_holder(none) -> no_holder;
probe_holder({Pid, Monitor}) ->
    case erlang:get({raw_visibility_down, Pid}) of
        {known, Reason} -> {holder_down, Reason};
        undefined ->
            Reference = make_ref(),
            Pid ! {probe, self(), Reference},
            receive
                {held_fun_result, Reference, Result} -> Result;
                {'DOWN', Monitor, process, Pid, Reason} ->
                    erlang:put({raw_visibility_down, Pid}, {known, Reason}),
                    {holder_down, Reason}
            after ?DEADLINE -> erlang:error(holder_probe_timeout)
            end
    end.

holder_loop() ->
    receive
        {probe, Parent, Reference} ->
            Result = capture(fun() ->
                Held = erlang:get(raw_visibility_held_fun),
                Held()
            end),
            Parent ! {held_fun_result, Reference, Result},
            holder_loop();
        stop -> ok
    end.

stop_holder(none) -> ok;
stop_holder({Pid, Monitor}) ->
    case erlang:get({raw_visibility_down, Pid}) of
        {known, _} -> ok;
        undefined ->
            Pid ! stop,
            receive
                {'DOWN', Monitor, process, Pid, _} -> ok
            after ?DEADLINE ->
                exit(Pid, kill),
                receive {'DOWN', Monitor, process, Pid, _} -> ok
                after ?DEADLINE -> erlang:error(holder_cleanup_timeout)
                end
            end
    end.

capture(Fun) ->
    try Fun() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.
