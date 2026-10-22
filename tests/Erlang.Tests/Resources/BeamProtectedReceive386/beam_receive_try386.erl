-module(beam_receive_try386).
-export([cases/0, run/0, run_stacks/0, capture/2, held/1]).

cases() ->
    [selected_normal, selected_throw, selected_error,
     timeout_zero, timeout_finite, nested_handler, trim_normal, trim_throw].

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

seed(timeout_zero) -> ok;
seed(timeout_finite) -> ok;
seed(Case) -> self() ! {selected, Case}.

dispatch(selected_normal) -> protected_receive(selected_normal, 0);
dispatch(selected_throw) -> protected_receive(selected_throw, 0);
dispatch(selected_error) -> protected_receive(selected_error, 0);
dispatch(timeout_zero) -> protected_receive(timeout_zero, 0);
dispatch(timeout_finite) -> protected_receive(timeout_finite, 7);
dispatch(nested_handler) -> nested_receive();
dispatch(trim_normal) -> trim_receive(trim_normal, {outside, [first, second]});
dispatch(trim_throw) -> trim_receive(trim_throw, {outside, [first, second]}).

protected_receive(Mode, Timeout) ->
    try
        receive
            {selected, Mode} -> selected(Mode)
        after Timeout ->
            {timed_out, Mode}
        end
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

selected(selected_normal) -> {selected, normal_value};
selected(selected_throw) -> erlang:throw({selected_throw_reason, [first, second]});
selected(selected_error) -> erlang:error({selected_error_reason, #{detail => preserved}});
selected(trim_normal) -> {selected, trim_normal_value};
selected(trim_throw) -> erlang:throw({trim_throw_reason, [first, second]}).

nested_receive() ->
    try
        Inner = try
            receive
                {selected, nested_handler} -> erlang:throw(inner_selected)
            after 0 ->
                inner_timeout
            end
        catch
            throw:inner_selected -> inner_handler
        end,
        erlang:error({outer_after_inner, Inner})
    catch
        Class:Reason:Stack -> {outer_caught, Class, Reason, Stack}
    end.

trim_receive(Mode, Outside) ->
    DeadBeforeReceive1 = retain({first_prefix, Mode, Outside}),
    DeadBeforeReceive2 = retain({second_prefix, Mode, Outside}),
    Result = try
        retain({DeadBeforeReceive1, DeadBeforeReceive2}),
        receive
            {selected, Mode} -> selected(Mode)
        after 0 ->
            trim_timeout
        end
    catch
        Class:Reason:Stack -> {trim_caught, Class, Reason, Stack}
    end,
    {Outside, Result}.

retain(Value) -> Value.

held(Owner) ->
    try
        Owner ! {held_ready, self()},
        receive
            release -> held_released
        end
    catch
        Class:Reason:Stack ->
            Owner ! {held_guest_catch, Class, Reason, Stack},
            held_guest_caught
    end.
