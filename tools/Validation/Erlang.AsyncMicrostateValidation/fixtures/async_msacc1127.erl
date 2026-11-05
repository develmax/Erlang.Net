-module(async_msacc1127).
-export([main/0, capture/2, run/0]).

-define(DRIVER, "async_msacc1127_drv").
-define(DEADLINE, 5000).
-define(INTERVAL, 20).

main() ->
    [DriverDir, Output] = init:get_plain_arguments(),
    case capture(DriverDir, Output) of
        ok -> halt(0);
        Failure -> io:format("~p~n", [Failure]), halt(1)
    end.

capture(DriverDir, Output) ->
    Loaded = capture(fun() -> erl_ddll:load_driver(DriverDir, ?DRIVER) end),
    case Loaded of
        {return, ok} ->
            Result = capture(fun run/0),
            Write = capture(fun() -> file:write_file(Output, term_to_binary(
                #{fixture => ?MODULE, load => Loaded, result => Result},
                [{minor_version, 2}])) end),
            Unload = capture(fun() -> erl_ddll:unload_driver(?DRIVER) end),
            case {Result, Write, Unload} of
                {{return, #{all_cases_returned := true, final_live_jobs_zero := true,
                            final_only_controller_state := true}}, {return, ok}, {return, ok}} -> ok;
                _ -> {failed, Result, Write, Unload}
            end;
        _ -> {load_failed, Loaded}
    end.

run() ->
    Threads = erlang:system_info(thread_pool_size),
    true = Threads > 0,
    Controller = open(),
    put({?MODULE, controller}, Controller),
    try
        Results = [observe(Id, Fun) || {Id, Fun} <- [
            {a01_topology, fun() -> topology(Threads) end},
            {a02_disabled, fun disabled/0},
            {a03_idle, fun idle/0},
            {a04_busy_gather, fun busy_gather/0},
            {a05_disable_busy, fun disable_busy/0},
            {a06_reset_disabled, fun reset_disabled/0},
            {a07_reset_busy, fun reset_busy/0},
            {a08_enable_busy, fun enable_busy/0},
            {a09_keyed_lanes, fun() -> keyed_lanes(Threads) end},
            {a10_no_pool_boundary, fun() ->
                #{status => not_executed, reason => requires_separate_zero_pool_vm,
                  actual_thread_pool_size => Threads}
            end},
            {a11_owner_close, fun() -> owner_close(Controller) end},
            {a12_invalid_control, fun invalid_control/0}
        ]],
        {return, Final} = capture(fun() -> snapshot(Controller) end),
        #{actual_thread_pool_size => Threads, cases => Results,
          final_native => Final,
          final_live_jobs_zero => maps:get(live_jobs, Final) =:= 0,
          final_only_controller_state => maps:get(live_states, Final) =:= 1,
          all_cases_returned => lists:all(fun(#{outcome := V}) -> element(1, V) =:= return end, Results),
          managed_ipc_negative_controls => not_executed}
    after
        erase({?MODULE, controller}),
        port_close(Controller)
    end.

observe(Id, Fun) ->
    Outcome = capture(Fun),
    Cleanup = capture(fun() -> erlang:system_flag(microstate_accounting, false) end),
    #{id => Id, outcome => Outcome, disable_cleanup => Cleanup}.

capture(Fun) ->
    try Fun() of Value -> {return, Value}
    catch Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

open() -> open_port({spawn_driver, ?DRIVER}, [binary]).
command(P, C, Data) -> iolist_to_binary(port_control(P, C, Data)).
submit(P, Key, Held) ->
    Id = binary_to_integer(command(P, case Held of true -> 1; false -> 4 end,
                                   integer_to_binary(Key))),
    true = Id >= 0,
    Id.
release(P) -> <<"released">> = command(P, 2, <<>>), ok.
snapshot(P) ->
    [I, R, D, F, S, J, T, L, Released] =
        [binary_to_integer(V) || V <- binary:split(command(P, 3, <<>>), <<",">>, [global])],
    #{invoked => I, returned => R, ready => D, freed => F,
      live_states => S, live_jobs => J, actual_native_tid => T,
      local_jobs => L, released => Released}.
with_port(Fun) ->
    Controller = get({?MODULE, controller}),
    Baseline = snapshot(Controller),
    P = open(),
    Body = capture(fun() -> Fun(P) end),
    %% Attempt each cleanup even if an earlier action fails. No manual ACK.
    Release = capture(fun() -> case erlang:port_info(P) of
        undefined -> ok; _ -> release(P) end end),
    %% Prove THIS port's accepted jobs have physically freed before close.
    LocalDrain = capture(fun() -> wait(fun() ->
        maps:get(local_jobs, snapshot(P)) =:= 0
    end) end),
    Close = capture(fun() -> case erlang:port_info(P) of
        undefined -> ok; _ -> port_close(P) end end),
    %% Outer scopes retain their own ports/jobs. Retire this exact state only.
    Drain = capture(fun() -> wait(fun() ->
        S = snapshot(Controller),
        maps:get(live_jobs, S) =< maps:get(live_jobs, Baseline) andalso
        maps:get(live_states, S) =:= maps:get(live_states, Baseline)
    end) end),
    case {Release, LocalDrain, Close, Drain} of
        {{return, _}, {return, ok}, {return, _}, {return, ok}} -> restore(Body);
        _ -> erlang:error({body_and_physical_cleanup, Body, Release, LocalDrain, Close, Drain})
    end.

restore({return, Value}) -> Value;
restore({exception, Class, Reason, Stack}) -> erlang:raise(Class, Reason, Stack).
flag(Value) -> erlang:system_flag(microstate_accounting, Value).
rows() -> erlang:statistics(microstate_accounting).
async(Rows) -> [Row || #{type := async} = Row <- Rows].
count_value({Ticks, _Transitions}) -> Ticks;
count_value(Ticks) when is_integer(Ticks) -> Ticks.
total(Rows, State) -> lists:sum([count_value(maps:get(State, maps:get(counters, Row), 0))
                              || Row <- async(Rows)]).
zero(Rows) -> lists:all(fun(Row) -> lists:all(fun(V) -> count_value(V) =:= 0 end,
                                      maps:values(maps:get(counters, Row))) end, async(Rows)).
now_ms() -> erlang:monotonic_time(millisecond).
wait(Predicate) -> wait(Predicate, now_ms() + ?DEADLINE).
wait(Predicate, Until) ->
    case Predicate() of
        true -> ok;
        false ->
            case now_ms() >= Until of
                true -> erlang:error(actual_native_handshake_timeout);
                false -> receive after 1 -> wait(Predicate, Until) end
            end
    end.
entered(P, Before) -> wait(fun() -> maps:get(invoked, snapshot(P)) > maps:get(invoked, Before) end).
done(P) -> receive {P, {data, <<"done">>}} -> ok after ?DEADLINE -> erlang:error(ready_async_timeout) end.
settled(P) -> wait(fun() -> maps:get(local_jobs, snapshot(P)) =:= 0 end).
bounded(Fun) ->
    Ref = make_ref(), Parent = self(),
    {Pid, Mon} = spawn_monitor(fun() -> Parent ! {Ref, capture(Fun)} end),
    receive
        {Ref, {return, Value}} ->
            receive {'DOWN', Mon, process, Pid, normal} -> Value end;
        {Ref, {exception, Class, Reason, Stack}} ->
            receive {'DOWN', Mon, process, Pid, normal} -> erlang:raise(Class, Reason, Stack) end;
        {'DOWN', Mon, process, Pid, Reason} -> erlang:error({request_died, Reason})
    after ?DEADLINE ->
        exit(Pid, kill),
        receive {'DOWN', Mon, process, Pid, _} -> ok end,
        erlang:error(unmanaged_control_waited_for_held_job)
    end.

 topology(Threads) ->
    R = rows(), A = async(R),
    #{raw => R, ids => lists:sort([maps:get(id, Row) || Row <- A]),
      expected_count => Threads, actual_count => length(A)}.
disabled() -> with_port(fun(P) ->
    Old = flag(false), Reset = flag(reset), A = rows(),
    Id = submit(P, 0, false), done(P), settled(P), B = rows(),
    #{old_flag => Old, reset => Reset, job => Id, before => A, after_work => B,
      all_zero_before => zero(A), all_zero_after => zero(B), native => snapshot(P)}
end).
idle() -> with_port(fun(P) ->
    flag(false), flag(reset), submit(P, 0, false), done(P), settled(P),
    Old = flag(true), A = rows(), receive after ?INTERVAL -> ok end, B = rows(),
    #{old_flag => Old, before => A, after_wait => B,
      sleep_increased => total(B, sleep) > total(A, sleep)}
end).
busy_gather() -> with_port(fun(P) ->
    flag(false), flag(reset), flag(true), Before = snapshot(P), submit(P, 0, true), entered(P, Before),
    A = bounded(fun rows/0), receive after ?INTERVAL -> ok end, B = bounded(fun rows/0),
    Held = snapshot(P), release(P), done(P), settled(P),
    #{before => A, after_wait => B, native_held => Held,
      returned_still_pending => maps:get(returned, Held) =:= maps:get(returned, Before),
      port_increased => total(B, port) > total(A, port)}
end).
disable_busy() -> with_port(fun(P) ->
    flag(false), flag(reset), flag(true), Before = snapshot(P), submit(P, 0, true), entered(P, Before),
    A = bounded(fun rows/0), Old = bounded(fun() -> flag(false) end),
    B = bounded(fun rows/0), receive after ?INTERVAL -> ok end, C = bounded(fun rows/0),
    Held = snapshot(P), release(P), done(P), settled(P),
    #{before => A, disable_old => Old, flushed => B, frozen => C,
      disabled_frozen => async(B) =:= async(C), native_held => Held}
end).
reset_disabled() -> with_port(fun(P) ->
    flag(false), flag(reset), flag(true), submit(P, 0, false), done(P), settled(P),
    receive after ?INTERVAL -> ok end, Positive = rows(), flag(false),
    Reset = flag(reset), A = rows(), B = rows(),
    #{positive => Positive, reset => Reset, reset_rows => A, later => B,
      all_zero => zero(A) andalso zero(B)}
end).
reset_busy() -> with_port(fun(P) ->
    flag(false), flag(reset), flag(true), Before = snapshot(P), submit(P, 0, true), entered(P, Before),
    A = bounded(fun rows/0), Reset = bounded(fun() -> flag(reset) end), B = bounded(fun rows/0),
    Held = snapshot(P), release(P), done(P), settled(P),
    #{before => A, reset => Reset, after_reset => B, native_held => Held}
end).
enable_busy() -> with_port(fun(P) ->
    flag(false), flag(reset), Before = snapshot(P), submit(P, 0, true), entered(P, Before),
    Old = bounded(fun() -> flag(true) end), A = bounded(fun rows/0),
    receive after ?INTERVAL -> ok end, B = bounded(fun rows/0),
    Repeat = bounded(fun() -> flag(true) end), C = bounded(fun rows/0),
    release(P), done(P), settled(P),
    #{old_flag => Old, enabled => A, while_held => B, repeated_old => Repeat,
      repeated_rows => C, sleep_delta => total(B, sleep) - total(A, sleep),
      port_delta => total(B, port) - total(A, port)}
end).
keyed_lanes(Threads) when Threads < 2 ->
    #{status => not_executed, reason => requires_two_actual_async_workers};
keyed_lanes(_) -> with_port(fun(P) -> with_port(fun(Q) ->
    Before = snapshot(P), submit(P, 0, true), entered(P, Before),
    submit(Q, 1, false), done(Q), settled(Q), Independent = snapshot(Q),
    Held = snapshot(P), release(P), done(P), settled(P),
    First = snapshot(P), submit(P, 0, false), done(P), settled(P), Second = snapshot(P),
    #{other_lane_before_release => Independent, held_lane => Held,
      same_key_same_native_thread => maps:get(actual_native_tid, First) =:= maps:get(actual_native_tid, Second),
      distinct_keys_distinct_native_threads => maps:get(actual_native_tid, First) =/= maps:get(actual_native_tid, Independent)}
end) end).
owner_close(Controller) ->
    Parent = self(), Ref = make_ref(),
    {Owner, Mon} = spawn_monitor(fun() ->
        P = open(), Before = snapshot(P), submit(P, 0, true), entered(P, Before),
        Parent ! {Ref, P}, receive never -> ok end
    end),
    receive
        {Ref, _P} -> ok;
        {'DOWN', Mon, process, Owner, Failure} -> erlang:error({owner_setup_failed, Failure})
    after ?DEADLINE -> exit(Owner, kill), erlang:error(owner_setup_timeout)
    end,
    BeforeClose = snapshot(Controller), exit(Owner, kill),
    Reason = receive {'DOWN', Mon, process, Owner, R} -> R after ?DEADLINE -> erlang:error(owner_down_timeout) end,
    wait(fun() -> S = snapshot(Controller), maps:get(live_jobs, S) =:= 0 andalso maps:get(live_states, S) =:= 1 end),
    #{owner_down => Reason, before_close => BeforeClose, after_close => snapshot(Controller)}.
invalid_control() -> with_port(fun(P) ->
    #{invalid_public_control => capture(fun() -> command(P, 255, <<>>) end),
      authenticated_managed_ipc => not_executed}
end).
