-module(small_opcodes_observer393d).
-export([run/0, run/1, run_stacks/0, run_stacks/1, worker/2, cases/1,
         ne/2, negate_sum/2, receive_next/1]).

run() -> run(all).

run(Group) ->
    {Child, Monitor} = erlang:spawn_monitor(?MODULE, worker, [self(), Group]),
    receive
        {Child, Values} ->
            receive
                {'DOWN', Monitor, process, Child, normal} -> Values;
                {'DOWN', Monitor, process, Child, Reason} -> erlang:error({worker_failure, Reason})
            after 5000 -> erlang:error(worker_not_settled)
            end;
        {'DOWN', Monitor, process, Child, Reason} -> erlang:error({worker_failure, Reason})
    after 30000 ->
        erlang:exit(Child, kill),
        receive {'DOWN', Monitor, process, Child, _} -> erlang:error(worker_timeout)
        after 5000 -> erlang:error(worker_unsettled)
        end
    end.

worker(Owner, Group) ->
    Values = rows(cases(Group)),
    Owner ! {self(), Values},
    ok.

rows([]) -> [];
rows([Case | Rest]) -> [{Case, observe(Case)} | rows(Rest)].

observe(Case) ->
    try value(Case) of Value -> {returned, Value}
    catch Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

run_stacks() -> run_stacks(all).

run_stacks(Group) ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, run(Group)},
        Zero = at_depth(0, Group),
        One = at_depth(1, Group),
        Two = at_depth(2, Group),
        Eight = at_depth(8, Group),
        [Default, Zero, One, Two, Eight]
    after erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth, Group) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run(Group)}.

cases(comparison) ->
    [numeric_equal, numeric_unequal, tuple_numeric_equal, list_numeric_equal,
     large_integer_float, map_exact_keys, map_numeric_values, different_kinds];
cases(receive_guard) ->
    [receive_skips_numeric_equal, receive_times_out_on_numeric_equal,
     receive_probe_equal, receive_probe_unequal];
cases(negation) ->
    [negate_positive, negate_negative, negate_positive_zero, negate_negative_zero];
cases(errors) ->
    [negate_bad_first, negate_bad_second, overflow_before_negate];
cases(all) ->
    cases(comparison) ++ cases(receive_guard) ++ cases(negation) ++ cases(errors).

value(numeric_equal) -> compare(1, 1.0);
value(numeric_unequal) -> compare(1, 2.0);
value(tuple_numeric_equal) -> compare({1}, {1.0});
value(list_numeric_equal) -> compare([1], [1.0]);
value(large_integer_float) -> compare(9007199254740993, 9007199254740992.0);
value(map_exact_keys) -> compare(#{1 => same}, #{1.0 => same});
value(map_numeric_values) -> compare(#{same => 1}, #{same => 1.0});
value(different_kinds) -> compare(1, same);
value(receive_skips_numeric_equal) -> receive_case([1, 2], 1.0);
value(receive_times_out_on_numeric_equal) -> receive_case([1], 1.0);
value(receive_probe_equal) -> receive_case([{comparison_probe, 1}], 1.0);
value(receive_probe_unequal) -> receive_case([{comparison_probe, 2}], 1.0);
value(negate_positive) -> negate_sum(1.0, 1.5);
value(negate_negative) -> negate_sum(-1.0, -1.5);
value(negate_positive_zero) -> negate_sum(0.0, 0.0);
value(negate_negative_zero) -> negate_sum(-0.0, -0.0);
value(negate_bad_first) -> negate_sum(1, 2.0);
value(negate_bad_second) -> negate_sum(1.0, not_float);
value(overflow_before_negate) -> negate_sum(1.0e308, 1.0e308).

compare(A, B) ->
    case ne([{A, marker}], B) of true -> different; false -> same end.

ne([{E, _} | Es], E0) when E /= E0 -> ne(Es, E);
ne(L, _E) -> L =:= [].

equal_success({A, B}) when A == B -> same;
equal_success(_) -> erlang:error(inconsistent_equal_branch).

equal_probe_result(Value, Threshold) when Value == Threshold ->
    {equal_checked, Value, Threshold};
equal_probe_result(_, _) -> erlang:error(inconsistent_equal_probe).

different_probe_result(Value, Threshold) when Value /= Threshold ->
    {different_checked, Value, Threshold};
different_probe_result(_, _) -> erlang:error(inconsistent_different_probe).

negate_sum(A, B) when is_float(A), is_float(B) ->
    Negated = -(A + B),
    <<Negated:64/float>>.

receive_next(Threshold) ->
    receive
        {comparison_probe, Value} when Value /= Threshold ->
            different_probe_result(Value, Threshold);
        Value when Value /= Threshold -> Value
    after 0 -> no_match end.

receive_case(Values, Threshold) ->
    send_values(Values),
    Result = receive_next(Threshold),
    Left = receive V -> V after 0 -> empty end,
    {Result, Left}.

send_values([]) -> ok;
send_values([V | Vs]) -> self() ! V, send_values(Vs).
