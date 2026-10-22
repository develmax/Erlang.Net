-module(original_sofs_observer398).
-export([run/0,run/1,run_stacks/0,run_stacks/1,worker/2,cases/1]).

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

cases(all) -> [empty_list,unary,binary,ternary,duplicates,mixed_arity,invalid_head,invalid_list,improper_list,wrong_arity].

value(empty_list) -> sofs:relation([]);
value(unary) -> sofs:relation([{a}]);
value(binary) -> sofs:relation([{a,1},{b,2}]);
value(ternary) -> sofs:relation([{a,1,x},{b,2,y}]);
value(duplicates) -> sofs:relation([{b,2},{a,1},{b,2}]);
value(mixed_arity) -> sofs:relation([{a,1},{b}]);
value(invalid_head) -> sofs:relation([17]);
value(invalid_list) -> sofs:relation(invalid398);
value(improper_list) -> sofs:relation([{a,b}|tail398]);
value(wrong_arity) -> erlang:apply(sofs,relation,[]).
