-module(beam_debugger_observer413).
-export([cases/0, run/0, run_stacks/0, verify/1, mode_probe/0]).

cases() -> [supported0,instrumentations0,toggle_instrumentations1,register1,unregister2,whereis0,breakpoint3,breakpoints1,breakpoints3,stack_frames2,peek_stack_frame_slot4,xregs_count1,peek_xreg3].

run() -> rows(cases()).

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
    {Child, Monitor} = erlang:spawn_monitor(beam_debugger_subject413, worker, [self(), Case]),
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

verify(Rows) -> verify_rows(cases(),Rows).
verify_rows([],[]) -> ok;
verify_rows([supported0|Cs],[{supported0,{returned,false}}|Rs]) -> verify_rows(Cs,Rs);
verify_rows([Case|Cs],[{Case,{caught,error,undef,Stack}}|Rs]) when is_list(Stack) -> verify_rows(Cs,Rs);
verify_rows(_,_) -> erlang:error(debugger_disabled_gate_failed).
mode_probe() ->
    Supported=erl_debugger:supported(),
    Result=try erl_debugger:instrumentations() of V->{returned,V}
           catch C:R:S->{caught,C,R,S} end,
    {Supported,Result}.
