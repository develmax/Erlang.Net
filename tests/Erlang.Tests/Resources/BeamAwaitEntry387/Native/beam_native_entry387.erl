-module(beam_native_entry387).
-export([cases/0, run/0, run_stacks/0, capture/2]).

cases() ->
    [reverse_valid, reverse_badarg, member_valid, member_badarg,
     keyfind_valid, keyfind_badarg, keymember_valid, keymember_badarg,
     keysearch_valid, keysearch_badarg].

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
    try primitive(Case) of
        Value -> {primitive_returned, Value}
    catch
        Class:Reason:Stack -> {primitive_caught, Class, Reason, Stack}
    end.

primitive(reverse_valid) -> lists:reverse([first, second], [tail]);
primitive(reverse_badarg) -> lists:reverse([first | invalid_tail], [tail]);
primitive(member_valid) -> lists:member(second, [first, second]);
primitive(member_badarg) -> lists:member(missing, [first | invalid_tail]);
primitive(keyfind_valid) -> lists:keyfind(second, 1, [{first, 1}, {second, 2}]);
primitive(keyfind_badarg) -> lists:keyfind(second, 0, [{second, 2}]);
primitive(keymember_valid) -> lists:keymember(second, 1, [{first, 1}, {second, 2}]);
primitive(keymember_badarg) -> lists:keymember(second, 0, [{second, 2}]);
primitive(keysearch_valid) -> lists:keysearch(second, 1, [{first, 1}, {second, 2}]);
primitive(keysearch_badarg) -> lists:keysearch(second, 0, [{second, 2}]).

