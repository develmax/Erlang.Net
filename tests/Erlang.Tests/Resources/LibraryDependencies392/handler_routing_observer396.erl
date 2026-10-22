-module(handler_routing_observer396).
-export([run/0,run/1,run_stacks/0,run_stacks/1,worker/2,cases/1,cleanup_remote/1]).

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

cases(all) -> [tuple_atom,tuple_tuple,local_atom,local_tuple,remote_atom,remote_tuple,returned_control].

value(tuple_atom) -> protected_tuple(reason396);
value(tuple_tuple) -> protected_tuple({reason396,payload});
value(local_atom) -> protected_local(reason396);
value(local_tuple) -> protected_local({reason396,payload});
value(remote_atom) -> protected_remote(reason396);
value(remote_tuple) -> protected_remote({reason396,payload});
value(returned_control) ->
    try cleanup_remote(returned396) of V -> {returned,V}
    catch C:R:S -> {unexpected_caught,C,R,S} end.

protected_tuple(V) ->
    try fail_tuple(V) of R -> {unexpected_return,R}
    catch C:R:S -> {caught,C,R,S} end.

protected_local(V) ->
    try fail_local(V) of R -> {unexpected_return,R}
    catch C:R:S -> {caught,C,R,S} end.

protected_remote(V) ->
    try fail_remote(V) of R -> {unexpected_return,R}
    catch C:R:S -> {caught,C,R,S} end.

fail_tuple(V) -> erlang:throw({wrapped396,V}).

fail_local(V) ->
    Cleaned = cleanup_local(V),
    erlang:exit(Cleaned).

fail_remote(V) ->
    Cleaned = ?MODULE:cleanup_remote(V),
    erlang:error(Cleaned).

cleanup_local(V) ->
    erlang:put(cleanup_marker396,V),
    {cleaned396,V}.

cleanup_remote(V) ->
    erlang:put(cleanup_marker396,V),
    {cleaned396,V}.
