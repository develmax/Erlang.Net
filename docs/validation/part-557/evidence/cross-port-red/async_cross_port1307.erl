-module(async_cross_port1307).
-export([main/0, capture/2, run/0]).

-define(DRIVER, "async_msacc1127_drv").
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
            Unload = attempt(fun() -> erl_ddll:unload_driver(?DRIVER) end),
            Write = attempt(fun() -> file:write_file(Output, term_to_binary(
                #{fixture => ?MODULE, load => Load, outcome => Outcome, unload => Unload},
                [{minor_version, 2}])) end),
            case {Outcome, Unload, Write} of
                {{return, _}, {return, ok}, {return, ok}} -> ok;
                _ -> {failed, Outcome, Unload, Write}
            end;
        _ -> {load_failed, Load}
    end.

run() ->
    1 = erlang:system_info(thread_pool_size),
    Body = attempt(fun() ->
        C = remember(controller, open()),
        A = remember(a, open()),
        B = remember(b, open()),
        Baseline = snapshot(C),
        put({?MODULE, baseline}, Baseline),
        submit(A, true),
        wait(fun() ->
            S = snapshot(A),
            maps:get(invoked, S) =:= maps:get(invoked, Baseline) + 1 andalso
            maps:get(returned, S) =:= maps:get(returned, Baseline) andalso
            maps:get(local_jobs, S) =:= 1
        end),
        submit(B, false),
        Queued = snapshot(B),
        true = maps:get(local_jobs, Queued) =:= 1,
        true = maps:get(live_jobs, Queued) =:= maps:get(live_jobs, Baseline) + 2,
        true = maps:get(invoked, Queued) =:= maps:get(invoked, Baseline) + 1,
        PortMon = erlang:monitor(port, B),
        Parent = self(), Ref = make_ref(),
        {Closer, CloserMon} = spawn_monitor(fun() ->
            Parent ! {Ref, attempt(fun() -> port_close(B) end)}
        end),
        put({?MODULE, closer}, {Closer, CloserMon}),
        {CloseReply, DownReason} = close_witnesses(Ref, PortMon, B, undefined, undefined,
                                                 now_ms() + ?DEADLINE),
        {return, true} = CloseReply,
        normal = DownReason,
        Held = snapshot(A),
        true = maps:get(released, Held) =:= 0,
        true = maps:get(local_jobs, Held) =:= 1,
        true = maps:get(invoked, Held) =:= maps:get(invoked, Baseline) + 1,
        true = maps:get(returned, Held) =:= maps:get(returned, Baseline),
        true = maps:get(live_jobs, Held) =:= maps:get(live_jobs, Baseline) + 2,
        #{actual_thread_pool_size => 1, baseline => Baseline, queued_b => Queued,
          close_b_reply => CloseReply, b_down => DownReason,
          a_still_held_after_b_closed => Held,
          close_b_before_release_a => true, down_b_before_release_a => true}
    end),
    %% Release the unrelated held job FIRST, even after a close-witness timeout.
    %% Never synchronously close B behind held A in failure cleanup.
    Release = attempt(fun() -> release_if_open(get({?MODULE, a})) end),
    CloserJoin = attempt(fun join_closer/0),
    NativeJoin = attempt(fun() -> wait(fun() ->
        maps:get(live_jobs, snapshot(get({?MODULE, controller}))) =:= 0
    end) end),
    CloseB = attempt(fun() -> close_if_open(get({?MODULE, b})) end),
    CloseA = attempt(fun() -> close_if_open(get({?MODULE, a})) end),
    Final = attempt(fun() ->
        C = get({?MODULE, controller}), Baseline = get({?MODULE, baseline}),
        wait(fun() -> maps:get(live_states, snapshot(C)) =:= 1 end),
        S = snapshot(C),
        true = maps:get(invoked, S) =:= maps:get(invoked, Baseline) + 2,
        true = maps:get(returned, S) =:= maps:get(returned, Baseline) + 2,
        true = maps:get(freed, S) =:= maps:get(freed, Baseline) + 2,
        true = maps:get(ready, S) =:= maps:get(ready, Baseline) + 1,
        0 = maps:get(live_jobs, S),
        S
    end),
    CloseController = attempt(fun() -> close_if_open(get({?MODULE, controller})) end),
    [erase({?MODULE, Key}) || Key <- [controller, a, b, baseline, closer]],
    Cleanup = [Release, CloserJoin, NativeJoin, CloseB, CloseA, Final, CloseController],
    case lists:all(fun({return, _}) -> true; (_) -> false end, Cleanup) of
        true ->
            {return, FinalNative} = Final,
            case Body of
                {return, Value} -> Value#{final_native => FinalNative};
                {exception, Class, Reason, Stack} -> erlang:raise(Class, Reason, Stack)
            end;
        false -> erlang:error({body_and_independent_cleanup, Body, Cleanup})
    end.

close_witnesses(_, _, _, Reply, Reason, _) when Reply =/= undefined, Reason =/= undefined ->
    {Reply, Reason};
close_witnesses(Ref, PortMon, B, Reply, Reason, Until) ->
    Remaining = erlang:max(0, Until - now_ms()),
    receive
        {Ref, R} -> close_witnesses(Ref, PortMon, B, R, Reason, Until);
        {'DOWN', PortMon, port, B, R} -> close_witnesses(Ref, PortMon, B, Reply, R, Until)
    after Remaining -> erlang:error({queued_port_close_waited_for_unrelated_held_job, Reply, Reason})
    end.

join_closer() ->
    case get({?MODULE, closer}) of
        undefined -> ok;
        {Pid, Mon} ->
            receive
                {'DOWN', Mon, process, Pid, normal} -> ok;
                {'DOWN', Mon, process, Pid, Reason} -> erlang:error({closer_failed, Reason})
            after ?DEADLINE -> erlang:error(closer_cleanup_timeout)
            end
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
