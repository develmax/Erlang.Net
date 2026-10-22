-module(frame_join_observer394).
-export([run/0, run/1, run_stacks/0, run_stacks/1, worker/2, cases/1]).

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

cases(all) -> [badmatch_small, badmatch_large, case_small, case_large, if_small, if_large,
               error_atom, error_tuple, length_empty, length_pair].

value(badmatch_small) -> frame_join394:branch_badmatch(small);
value(badmatch_large) -> frame_join394:branch_badmatch(large);
value(case_small) -> frame_join394:branch_case(small);
value(case_large) -> frame_join394:branch_case(large);
value(if_small) -> frame_join394:branch_if(small);
value(if_large) -> frame_join394:branch_if(large);
value(error_atom) -> frame_join394:failure_last(failure394);
value(error_tuple) -> frame_join394:failure_last({failure394, payload});
value(length_empty) -> frame_join394:valid_last([]);
value(length_pair) -> frame_join394:valid_last([one, two]).
