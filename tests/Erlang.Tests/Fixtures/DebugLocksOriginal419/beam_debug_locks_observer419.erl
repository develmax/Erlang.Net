-module(beam_debug_locks_observer419).
-export([cases/0,run/0,run_stacks/0,verify/1]).
cases()->beam_debug_locks_subject419:cases().
run()->rows(cases()).
rows([]) -> [];
rows([Case | Rest]) ->
    Outcome = observe(Case),
    [{Case, Outcome} | rows(Rest)].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, Old, run()},
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
    Effective = erlang:system_flag(backtrace_depth, Depth),
    {Depth, Effective, run()}.
observe(Case) ->
    {Child, Monitor} = erlang:spawn_monitor(beam_debug_locks_subject419, worker, [self(), Case]),
    receive
        {Case, Outcome} -> wait_down(Child, Monitor, Case, Outcome);
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({child_down_without_report, Case, Reason})
    after 30000 ->
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

verify(Rows) -> verify_rows(cases(), Rows).
verify_rows([], []) -> ok;
verify_rows([Case|Cases], [{Case,Outcome}|Rows]) ->
    Expected = beam_debug_locks_subject419:expected(Case),
    case {Expected,Outcome} of
        {{value,Value},{returned,Value}} -> ok;
        {badarg,{raised,error,badarg,Stack}} when is_list(Stack) -> ok;
        _ -> erlang:error({chunk_semantic_gate_failed,Case,Expected,Outcome})
    end,
    verify_rows(Cases,Rows);
verify_rows(_, _) -> erlang:error(chunk_row_gate_failed).

