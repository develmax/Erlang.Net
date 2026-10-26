-module(retirement_probe502).
-export([load/1, hold/0, release/0, status/0, run/0]).
load(Path) -> erlang:load_nif(Path, []).
hold() -> erlang:nif_error(not_loaded).
release() -> erlang:nif_error(not_loaded).
status() -> erlang:nif_error(not_loaded).
wait_enter(0) -> erlang:error(hold_not_entered);
wait_enter(N) ->
    case status() of
        {1, 0, -1} = Entered -> Entered;
        _ -> receive after 1 -> wait_enter(N - 1) end
    end.
wait_done(0) -> erlang:error(hold_not_finished);
wait_done(N) ->
    case status() of
        {1, 1, _} = Done -> Done;
        _ -> receive after 1 -> wait_done(N - 1) end
    end.
observe(Pid, Monitor) ->
    Entered = wait_enter(1000),
    true = exit(Pid, kill),
    Parent = self(),
    Query = spawn(fun() -> Parent ! {query_started, self()}, Parent ! {query_result, self(), erlang:is_process_alive(Pid)} end),
    Started = receive {query_started, Query} -> true after 1000 -> missing end,
    Alive = receive {query_result, Query, Result} -> Result after 2000 -> still_pending end,
    Ref = make_ref(),
    ok = erts_internal:is_process_alive(Pid, Ref),
    Native = receive {Ref, Value} -> Value after 1000 -> still_pending end,
    Name = whereis(retirement_probe502_target),
    Down = receive {'DOWN', Monitor, process, Pid, Reason} -> Reason after 1000 -> still_pending end,
    BeforeRelease = status(),
    {before_release, Entered, Started, Alive, Native, Name, Down, BeforeRelease}.
run() ->
    {Pid, Monitor} = spawn_monitor(fun() -> true = register(retirement_probe502_target, self()), hold() end),
    Observed = try observe(Pid, Monitor)
        catch C:R:S -> {raised, C, R, S}
        after release()
    end,
    Finished = wait_done(2000),
    {Observed, {after_release, Finished}}.
