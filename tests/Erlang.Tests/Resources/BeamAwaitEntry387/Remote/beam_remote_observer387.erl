-module(beam_remote_observer387).
-export([cases/0, run/0, run_stacks/0, start/1, wait_ready/3, finish/3]).

cases() -> [normal, error, throw, exit].

run() -> rows(cases()).

rows([]) -> [];
rows([Case | Rest]) ->
    {Child, Monitor} = start(Case),
    ok = wait_ready(Child, Monitor, Case),
    Outcome = finish(Child, Monitor, Case),
    [{Case, Outcome} | rows(Rest)].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, run()},
        Zero = at_depth(0),
        One = at_depth(1),
        Two = at_depth(2),
        Eight = at_depth(8),
        [Default, Zero, One, Two, Eight]
    after
        erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run()}.

start(Case) ->
    erlang:spawn_monitor(beam_remote_caller387, worker, [self(), Case]).

wait_ready(Child, Monitor, Case) ->
    receive
        {peer_ready, Child, Case} -> wait_until_waiting(Child, Monitor, Case, 2000);
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({child_down_before_peer_ready, Case, Reason})
    after 5000 ->
        abort(Child, Monitor, Case)
    end.

wait_until_waiting(Child, Monitor, Case, Remaining) ->
    case erlang:process_info(Child, status) of
        {status, waiting} -> ok;
        undefined -> erlang:error({peer_disappeared, Case});
        _ when Remaining > 0 ->
            receive after 1 -> ok end,
            wait_until_waiting(Child, Monitor, Case, Remaining - 1);
        _ -> abort(Child, Monitor, Case)
    end.

finish(Child, Monitor, Case) ->
    Child ! {rejected, Case},
    Child ! {release, Case},
    receive
        {Case, Outcome} -> wait_down(Child, Monitor, Case, Outcome);
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({child_down_without_report, Case, Reason})
    after 5000 ->
        abort(Child, Monitor, Case)
    end.

wait_down(Child, Monitor, Case, Outcome) ->
    receive
        {'DOWN', Monitor, process, Child, normal} -> Outcome;
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({reported_but_abnormal_child, Case, Reason})
    after 5000 ->
        abort(Child, Monitor, Case)
    end.

abort(Child, Monitor, Case) ->
    erlang:exit(Child, kill),
    receive
        {'DOWN', Monitor, process, Child, _} -> erlang:error({child_timeout, Case})
    after 5000 ->
        erlang:error({unsettled_child, Case})
    end.
