-module(beam_delay387).
-export([cases/0, run/0, run_stacks/0, capture/2, literal_delay/0]).

cases() ->
    [literal_finite_normal, literal_finite_error, dynamic_finite_normal,
     atom_invalid, negative_invalid, zero_queued].

run() -> rows(cases()).

rows([]) -> [];
rows([Case | Rest]) ->
    Outcome = observe(Case),
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

observe(Case) ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try observe_child(Case)
    after erlang:system_flag(backtrace_depth, Old)
    end.

observe_child(Case) ->
    {Child, Monitor} = erlang:spawn_monitor(?MODULE, capture, [self(), Case]),
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

capture(Owner, Case) ->
    Outcome = try run_case(Case) of
        Value -> {returned, Value}
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end,
    Owner ! {Case, Outcome},
    ok.

run_case(Case) ->
    self() ! {retained_message, {delay_case, Case}},
    Result = dispatch(Case),
    Retained = receive
        {retained_message, Marker} -> {retained, Marker}
    after 0 ->
        message_was_consumed
    end,
    {Result, Retained}.

dispatch(literal_finite_normal) -> literal_delay();
dispatch(literal_finite_error) -> literal_failure();
dispatch(dynamic_finite_normal) -> protected_delay(normal, 2);
dispatch(atom_invalid) -> protected_delay(normal, invalid_timeout);
dispatch(negative_invalid) -> protected_delay(normal, -1);
dispatch(zero_queued) -> protected_delay(normal, 0).

literal_delay() ->
    receive after 2 -> literal_delay_returned end.

literal_failure() ->
    try
        receive after 2 -> erlang:error(literal_delay_failed) end
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

protected_delay(Mode, Timeout) ->
    try
        receive after Timeout -> {delay_returned, Mode} end
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

