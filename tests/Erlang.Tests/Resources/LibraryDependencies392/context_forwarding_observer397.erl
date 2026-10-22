-module(context_forwarding_observer397).
-export([run/0,run/1,run_stacks/0,run_stacks/1,worker/2,cases/1,identity/1]).

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

cases(all) -> [empty_tail,byte_tail,partial_tail,zeroes,direct_binary,invalid_source,short_prefix,no_payload].

value(empty_tail) -> entry(<<9,65>>);
value(byte_tail) -> entry(<<9,65,66>>);
value(partial_tail) -> entry(<<9,65,3:2>>);
value(zeroes) -> entry(<<0,0,0>>);
value(direct_binary) -> forward(<<17,18>>);
value(invalid_source) -> entry(invalid397);
value(short_prefix) -> entry(<<1:2>>);
value(no_payload) -> entry(<<9>>).

entry(B) ->
    <<_:8,Rest/bitstring>> = B,
    forward(Rest).

forward(B) ->
    {N,Tail} = decode(B),
    Value = ?MODULE:identity(N),
    {Value,Tail}.

decode(<<N:8,Tail/bitstring>>) -> {N,Tail}.

identity(V) -> V.
