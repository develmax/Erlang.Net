-module(binary_position_observer398).
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

cases(all) -> [short_empty,short_tail,long_empty,long_tail,fallback_tag,fallback_empty,fallback_partial,invalid,short_large,long_large].

value(short_empty) -> decode(<<3:4,0:1,0:3>>);
value(short_tail) -> decode(<<3:4,0:1,0:3,66,67>>);
value(long_empty) -> decode(<<3:3,1:2,0:3,65>>);
value(long_tail) -> decode(<<3:3,1:2,0:3,65,66,67>>);
value(fallback_tag) -> decode(<<255,66>>);
value(fallback_empty) -> decode(<<>>);
value(fallback_partial) -> decode(<<3:3>>);
value(invalid) -> decode(invalid398);
value(short_large) -> decode(<<15:4,0:1,0:3,0:512>>);
value(long_large) -> decode(<<7:3,1:2,0:3,255,0:512>>).

decode(<<N:4,0:1,_:3,Tail/binary>>) -> {short,N,Tail};
decode(<<N:3,1:2,_:3,M:8,Tail/binary>>) -> {long,(N bsl 8) bor M,Tail};
decode(B) when is_bitstring(B) -> {fallback,B};
decode(B) -> erlang:error({invalid_binary,B}).
