-module(beam_pure_receive_observer396).
-export([cases/0,run/0,run_stacks/0,worker/1,re/1,rm/0,rn/0,rl/0,rb/1,rs/0,rt/2,rtry/0]).

cases() -> [element1_skips_badargs, element2_skips_short, element_zero_timeout, element_huge_timeout, element_noninteger_timeout, map_predicate, nil_predicate, list_predicate, byte_size_nonbyte, byte_size_two, byte_size_timeout, self_guard, tuple_guard, tuple_guard_timeout, active_try_success, active_try_timeout, repeat_after_rejection, byte_size_zero].

run() ->
    {Child, Ref} = erlang:spawn_monitor(?MODULE, worker, [self()]),
    receive
        {Child, Values} ->
            receive
                {'DOWN', Ref, process, Child, normal} -> Values;
                {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
            after 5000 -> erlang:error(child_not_settled)
            end;
        {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
    after 30000 -> erlang:exit(Child, kill), erlang:error(child_timeout)
    end.

worker(Owner) ->
    Owner ! {self(), rows(cases())},
    ok.

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
    after erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run()}.

observe(Case) ->
    try value(Case) of Value -> {returned, Value}
    catch Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

value(element1_skips_badargs) -> mail([bad,{}, {skip}, {ok,42},tail],fun()->re(1)end);
value(element2_skips_short) -> mail([{ok},{x,ok},99,tail],fun()->re(2)end);
value(element_zero_timeout) -> mail([{ok,42},bad],fun()->re(0)end);
value(element_huge_timeout) -> mail([{ok,42}],fun()->re(1 bsl 128)end);
value(element_noninteger_timeout) -> mail([{ok,42}],fun()->re(wrong)end);
value(map_predicate) -> mail([bad,[],#{answer=>42},tail],fun rm/0);
value(nil_predicate) -> mail([bad,[a],[],tail],fun rn/0);
value(list_predicate) -> mail([bad,#{},[1|2],tail],fun rl/0);
value(byte_size_nonbyte) -> mail([bad,<<1:1>>,<<1,2>>,tail],fun()->rb(1)end);
value(byte_size_two) -> mail([bad,<<1>>,<<1,2>>,tail],fun()->rb(2)end);
value(byte_size_timeout) -> mail([bad,[],#{},tail],fun()->rb(2)end);
value(self_guard) -> mail([{from,wrong,ignored},{from,self(),42},tail],fun rs/0);
value(tuple_guard) -> mail([{bad,42},{{prefix,tag},99},tail],fun()->rt(prefix,tag)end);
value(tuple_guard_timeout) -> mail([{bad,42},{{prefix,other},99},tail],fun()->rt(prefix,tag)end);
value(active_try_success) -> mail([bad,{ok,77},tail],fun rtry/0);
value(active_try_timeout) -> mail([bad,{},tail],fun rtry/0);
value(repeat_after_rejection) -> mail([bad,{ok,1},{ok,2},tail],fun()->{re(1),re(1)}end);
value(byte_size_zero) -> mail([bad,<<>>,tail],fun()->rb(0)end).

mail(Messages,Fun) ->
    send_all(Messages),
    try Fun() of Value -> {Value,drain()}
    after drain()
    end.
send_all([]) -> ok;
send_all([M|Rest]) -> self() ! M, send_all(Rest).
drain() -> receive M -> [M|drain()] after 0 -> [] end.
re(Index) -> receive M when element(Index,M) =:= ok -> M after 0 -> timeout end.
rm() -> receive M when is_map(M) -> M after 0 -> timeout end.
rn() -> receive M when M =:= [] -> M after 0 -> timeout end.
rl() -> receive M when is_list(M) -> M after 0 -> timeout end.
rb(Size) -> receive M when byte_size(M) =:= Size -> M after 0 -> timeout end.
rs() -> receive {from,Pid,Value} when Pid =:= self() -> Value after 0 -> timeout end.
rt(Prefix,Tag) -> receive M when element(1,M) =:= {Prefix,Tag} -> element(2,M) after 0 -> timeout end.
rtry() ->
    try receive M when element(1,M) =:= ok -> element(2,M)
        after 0 -> erlang:error(timed_out)
        end
    catch error:timed_out -> handled_timeout
    end.
