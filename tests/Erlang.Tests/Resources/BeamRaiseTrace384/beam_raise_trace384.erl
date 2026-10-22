-module(beam_raise_trace384).
-export([cases/0, run/0, run_stacks/0, capture/2]).

cases() ->
    [badarg_length, badarg_byte_size, generated_error_info,
     empty_error, empty_throw, empty_exit,
     mfa3_arity, mfa3_arguments, mfa3_arbitrary,
     mfa4_locations, mfa4_improper_location,
     fun2_arguments, fun2_arbitrary, fun3_locations, fun3_improper_location,
     invalid_class_atom, invalid_class_integer,
     invalid_frame_pair, invalid_frame_module, invalid_frame_location,
     invalid_stack_atom, invalid_stack_improper, invalid_after_clip,
     raw_shrink, raw_grow, raw_change_class, raw_invalid_class,
     cooked_shrink, cooked_grow].

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

run_case(badarg_length) -> failing_length(not_a_list);
run_case(badarg_byte_size) -> failing_byte_size(not_a_binary);
run_case(generated_error_info) ->
    erlang:error(custom_error, [first, {second, 384}],
        [{error_info, #{module => ?MODULE, cause => {supplied, 384}}}]);
run_case(empty_error) -> cooked(error, []);
run_case(empty_throw) -> cooked(throw, []);
run_case(empty_exit) -> cooked(exit, []);
run_case(mfa3_arity) -> cooked(error, [{supplied_module, supplied_function, 2}]);
run_case(mfa3_arguments) -> cooked(error, [{supplied_module, supplied_function, [first, second]}]);
run_case(mfa3_arbitrary) -> cooked(error, [{supplied_module, supplied_function, arbitrary_arguments}]);
run_case(mfa4_locations) -> cooked(error,
    [{supplied_module, supplied_function, -1,
        [{file, "supplied384.erl"}, {line, 384},
         {error_info, #{module => supplied_formatter, cause => {details, [a, b]}}},
         {unknown_location, preserved}, {line, 385}]}]);
run_case(mfa4_improper_location) -> cooked(error,
    [{supplied_module, supplied_function, [first | argument_tail], [{line, 384} | location_tail]}]);
run_case(fun2_arguments) -> cooked(error, [{fun erlang:self/0, [first, second]}]);
run_case(fun2_arbitrary) -> cooked(error, [{fun erlang:self/0, arbitrary_arguments}]);
run_case(fun3_locations) -> cooked(error,
    [{fun erlang:self/0, [first | argument_tail], [{file, "supplied_fun384.erl"}, {line, 384}]}]);
run_case(fun3_improper_location) -> cooked(error,
    [{fun erlang:self/0, arbitrary_arguments, [{line, 384} | location_tail]}]);
run_case(invalid_class_atom) -> cooked(invalid_class, []);
run_case(invalid_class_integer) -> cooked(384, []);
run_case(invalid_frame_pair) -> cooked(error, [{not_a_fun, []}]);
run_case(invalid_frame_module) -> cooked(error, [{384, supplied_function, 0, []}]);
run_case(invalid_frame_location) -> cooked(error, [{supplied_module, supplied_function, 0, not_a_list}]);
run_case(invalid_stack_atom) -> cooked(error, not_a_list);
run_case(invalid_stack_improper) -> cooked(error,
    [{supplied_module, supplied_function, 0, []} | stack_tail]);
run_case(invalid_after_clip) -> cooked(error, valid_prefix(10, [{invalid_frame}]));
run_case(raw_shrink) -> raw_change(8, 0, error);
run_case(raw_grow) -> raw_change(1, 8, error);
run_case(raw_change_class) -> raw_change(8, 8, throw);
run_case(raw_invalid_class) -> raw_change(8, 8, invalid_class);
run_case(cooked_shrink) -> cooked_change(8, 0);
run_case(cooked_grow) -> cooked_change(1, 8).

failing_length(Value) -> erlang:length(Value).

failing_byte_size(Value) -> erlang:byte_size(Value).

cooked(Class, Stack) -> erlang:raise(Class, supplied_reason, Stack).

valid_prefix(0, Tail) -> Tail;
valid_prefix(Count, Tail) ->
    [{supplied_module, supplied_function, Count, []} | valid_prefix(Count - 1, Tail)].

raw_change(CaptureDepth, NextDepth, Class) ->
    erlang:system_flag(backtrace_depth, CaptureDepth),
    try chain_first() catch
        _:Reason:Stack ->
            erlang:system_flag(backtrace_depth, NextDepth),
            erlang:raise(Class, Reason, Stack)
    end.

cooked_change(CaptureDepth, NextDepth) ->
    erlang:system_flag(backtrace_depth, CaptureDepth),
    try chain_first() catch
        Class:Reason:Stack ->
            Saved = {Class, Reason, Stack},
            erlang:system_flag(backtrace_depth, NextDepth),
            cooked_saved(Saved)
    end.

cooked_saved({Class, Reason, Stack}) -> erlang:raise(Class, Reason, Stack).

chain_first() ->
    Value = chain_second(),
    {first, Value}.

chain_second() ->
    Value = chain_third(),
    {second, Value}.

chain_third() ->
    erlang:error(raw_source_reason).
