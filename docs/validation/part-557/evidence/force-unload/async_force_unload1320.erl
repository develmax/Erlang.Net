-module(async_force_unload1320).
-export([main/0, capture/2, run/0]).
-define(DRIVER, "async_msacc1127_drv").
-define(DRIVER_ATOM, async_msacc1127_drv).
-define(DEADLINE, 5000).

main() ->
    [DriverDir, Output] = init:get_plain_arguments(),
    case capture(DriverDir, Output) of
        ok -> halt(0);
        Failure -> io:format("~p~n", [Failure]), halt(1)
    end.

capture(DriverDir, Output) ->
    Load = attempt(fun() -> erl_ddll:load_driver(DriverDir, ?DRIVER) end),
    case Load of
        {return, ok} ->
            Outcome = attempt(fun run/0),
            Write = attempt(fun() -> file:write_file(Output, term_to_binary(
                #{fixture => ?MODULE, load => Load, outcome => Outcome}, [{minor_version, 2}])) end),
            case {Outcome, Write} of
                {{return, _}, {return, ok}} -> ok;
                _ -> {failed, Outcome, Write}
            end;
        _ -> {load_failed, Load}
    end.

%% Root caller must own the sole load reference, as capture/2 does.
run() ->
    1 = erlang:system_info(thread_pool_size),
    OldTrap = process_flag(trap_exit, true),
    Body = attempt(fun() ->
        A = remember(a, open()),
        B = remember(b, open()),
        Base = snapshot(A),
        submit(A, true),
        wait(fun() ->
            S = snapshot(A),
            maps:get(invoked, S) =:= maps:get(invoked, Base) + 1 andalso
            maps:get(returned, S) =:= maps:get(returned, Base) andalso
            maps:get(local_jobs, S) =:= 1
        end),
        submit(B, false),
        Queued = snapshot(B),
        true = maps:get(local_jobs, Queued) =:= 1,
        true = maps:get(live_jobs, Queued) =:= maps:get(live_jobs, Base) + 2,
        true = maps:get(invoked, Queued) =:= maps:get(invoked, Base) + 1,
        true = maps:get(returned, Queued) =:= maps:get(returned, Base),
        2 = PortCount = erl_ddll:info(?DRIVER, port_count),
        MA = erlang:monitor(port, A),
        MB = erlang:monitor(port, B),
        Unload = erl_ddll:try_unload(?DRIVER, [kill_ports, {monitor, pending_driver}]),
        {ok, pending_driver, DriverMon} = Unload,
        true = is_reference(DriverMon),
        {Witnesses, Events} = witnesses(A, MA, B, MB, DriverMon, #{}, [], now_ms() + ?DEADLINE),
        undefined = erlang:port_info(A),
        undefined = erlang:port_info(B),
        #{actual_thread_pool_size => 1, root_owned_sole_load => true,
          baseline => Base, queued_b => Queued, port_count_before => PortCount,
          try_unload => Unload, witnesses => Witnesses, event_arrival_order => Events,
          post_unloaded_port_count => attempt(fun() -> erl_ddll:info(?DRIVER, port_count) end),
          both_ports_dead => true, driver_unloaded_observed => true}
    end),
    %% On failure, release A before any synchronous close behind its queued B.
    Release = attempt(fun() -> release_if_open(get({?MODULE, a})) end),
    CloseB = attempt(fun() -> close_if_open(get({?MODULE, b})) end),
    CloseA = attempt(fun() -> close_if_open(get({?MODULE, a})) end),
    DriverCleanup = attempt(fun cleanup_driver/0),
    ExitDrain = attempt(fun() ->
        drain_owned_exit(get({?MODULE, a})),
        drain_owned_exit(get({?MODULE, b}))
    end),
    RestoreTrap = attempt(fun() ->
        case ExitDrain of
            {return, _} -> process_flag(trap_exit, OldTrap);
            _ -> trap_exit_retained_for_unsettled_owned_signals
        end
    end),
    [erase({?MODULE, {exit_seen, get({?MODULE, Key})}}) || Key <- [a, b]],
    [erase({?MODULE, Key}) || Key <- [a, b]],
    Cleanup = [Release, CloseB, CloseA, DriverCleanup, ExitDrain, RestoreTrap],
    case lists:all(fun({return, _}) -> true; (_) -> false end, Cleanup) of
        true ->
            case Body of
                {return, Value} -> Value#{cleanup => Cleanup};
                {exception, Class, Reason, Stack} -> erlang:raise(Class, Reason, Stack)
            end;
        false -> erlang:error({body_and_independent_cleanup, Body, Cleanup})
    end.

witnesses(_, _, _, _, _, Seen, Events, _) when map_size(Seen) =:= 5 ->
    {Seen, lists:reverse(Events)};
witnesses(A, MA, B, MB, DM, Seen, Events, Until) ->
    Remaining = erlang:max(0, Until - now_ms()),
    receive
        {'DOWN', MA, port, A, Reason} = Event ->
            driver_unloaded = Reason,
            witnesses(A, MA, B, MB, DM, Seen#{a_down => Reason}, [Event | Events], Until);
        {'DOWN', MB, port, B, Reason} = Event ->
            driver_unloaded = Reason,
            witnesses(A, MA, B, MB, DM, Seen#{b_down => Reason}, [Event | Events], Until);
        {'EXIT', A, Reason} = Event ->
            put({?MODULE, {exit_seen, A}}, true),
            driver_unloaded = Reason,
            witnesses(A, MA, B, MB, DM, Seen#{a_exit => Reason}, [Event | Events], Until);
        {'EXIT', B, Reason} = Event ->
            put({?MODULE, {exit_seen, B}}, true),
            driver_unloaded = Reason,
            witnesses(A, MA, B, MB, DM, Seen#{b_exit => Reason}, [Event | Events], Until);
        {'DOWN', DM, driver, ?DRIVER_ATOM, Reason} = Event ->
            unloaded = Reason,
            witnesses(A, MA, B, MB, DM, Seen#{driver_down => Reason}, [Event | Events], Until);
        {'UP', DM, driver, ?DRIVER_ATOM, Reason} -> erlang:error({unexpected_driver_up, Reason})
    after Remaining -> erlang:error({force_unload_witness_timeout, Seen, lists:reverse(Events)})
    end.

drain_owned_exit(undefined) -> ok;
drain_owned_exit(P) ->
    case get({?MODULE, {exit_seen, P}}) of
        true -> ok;
        _ ->
            receive
                {'EXIT', P, Reason} -> put({?MODULE, {exit_seen, P}}, true), {observed_exit, Reason}
            after ?DEADLINE -> erlang:error({owned_port_exit_cleanup_timeout, P})
            end
    end.

cleanup_driver() ->
    case erl_ddll:try_unload(?DRIVER, [kill_ports, {monitor, pending_driver}]) of
        {error, not_loaded} -> ok;
        {ok, unloaded} -> ok;
        {ok, pending_driver, Ref} ->
            receive
                {'DOWN', Ref, driver, ?DRIVER_ATOM, unloaded} -> ok;
                {'UP', Ref, driver, ?DRIVER_ATOM, Reason} -> erlang:error({cleanup_driver_up, Reason})
            after ?DEADLINE -> erlang:error(cleanup_driver_monitor_timeout)
            end;
        Other -> erlang:error({unexpected_cleanup_unload, Other})
    end.

remember(Key, Port) -> put({?MODULE, Key}, Port), Port.
open() -> open_port({spawn_driver, ?DRIVER}, [binary]).
submit(P, Held) ->
    Id = binary_to_integer(iolist_to_binary(port_control(P, case Held of true -> 1; false -> 4 end, <<"0">>))),
    true = Id >= 0,
    Id.
release_if_open(undefined) -> ok;
release_if_open(P) ->
    case erlang:port_info(P) of
        undefined -> ok;
        _ -> <<"released">> = iolist_to_binary(port_control(P, 2, <<>>)), ok
    end.
close_if_open(undefined) -> ok;
close_if_open(P) -> case erlang:port_info(P) of undefined -> ok; _ -> port_close(P), ok end.
snapshot(P) ->
    [I, R, D, F, S, J, T, L, Released] =
        [binary_to_integer(V) || V <- binary:split(iolist_to_binary(port_control(P, 3, <<>>)), <<",">>, [global])],
    #{invoked => I, returned => R, ready => D, freed => F,
      live_states => S, live_jobs => J, actual_native_tid => T,
      local_jobs => L, released => Released}.
attempt(Fun) ->
    try Fun() of Value -> {return, Value}
    catch Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.
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
