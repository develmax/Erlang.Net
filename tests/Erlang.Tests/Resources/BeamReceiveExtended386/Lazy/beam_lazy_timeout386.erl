-module(beam_lazy_timeout386).
-export([cases/0, run/0, run_stacks/0, capture/2]).

cases() ->
    [atom_selected, atom_miss, negative_selected, negative_miss,
     oversized_selected, oversized_miss, float_selected, float_miss,
     timeout_throw_zero, timeout_throw_finite, infinity_selected, zero_selected].
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
    self() ! {rejected, {preserved, Case}},
    seed(Case),
    Result = dispatch(Case),
    Preserved = receive
        {rejected, Marker} -> {retained, Marker}
    after 0 ->
        rejected_message_missing
    end,
    {Result, Preserved}.

seed(atom_miss) -> ok;
seed(negative_miss) -> ok;
seed(oversized_miss) -> ok;
seed(float_miss) -> ok;
seed(timeout_throw_zero) -> ok;
seed(timeout_throw_finite) -> ok;
seed(Case) -> self() ! {selected, Case}.

dispatch(atom_selected) -> protected_receive(atom_selected, invalid_timeout);
dispatch(atom_miss) -> protected_receive(atom_miss, invalid_timeout);
dispatch(negative_selected) -> protected_receive(negative_selected, -1);
dispatch(negative_miss) -> protected_receive(negative_miss, -1);
dispatch(oversized_selected) -> protected_receive(oversized_selected, 4294967296);
dispatch(oversized_miss) -> protected_receive(oversized_miss, 4294967296);
dispatch(float_selected) -> protected_receive(float_selected, 0.5);
dispatch(float_miss) -> protected_receive(float_miss, 0.5);
dispatch(timeout_throw_zero) -> protected_receive(timeout_throw_zero, 0);
dispatch(timeout_throw_finite) -> protected_receive(timeout_throw_finite, 3);
dispatch(infinity_selected) -> protected_receive(infinity_selected, infinity);
dispatch(zero_selected) -> protected_receive(zero_selected, 0).

protected_receive(Mode, Timeout) ->
    try
        receive
            {selected, Mode} -> {selected, Mode}
        after Timeout ->
            erlang:throw({timeout_body_throw, Mode})
        end
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

