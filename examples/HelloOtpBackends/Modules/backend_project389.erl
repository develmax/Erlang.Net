-module(backend_project389).
-export([run/0, run_stacks/0, worker/1]).

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
    Owner ! {self(), rows()},
    ok.

rows() ->
    [{Case, observe(Case)} || Case <- [sort, queue_roundtrip, queue_join,
        queue_callback, list_callback, sort_error, queue_error, callback_error]].

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

value(sort) -> lists:sort([3, 1, 2, 1]);
value(queue_roundtrip) -> queue:to_list(queue:in_r(0, queue:from_list([1, 2, 3])));
value(queue_join) -> queue:to_list(queue:join(queue:from_list([1, 2]), queue:from_list([3, 4])));
value(queue_callback) -> queue:to_list(queue:filter(fun(X) -> X rem 2 =:= 0 end, queue:from_list([1, 2, 3, 4])));
value(list_callback) -> lists:map(fun(X) -> X * 2 end, [1, 2, 3]);
value(sort_error) -> lists:sort(invalid);
value(queue_error) -> queue:out(invalid);
value(callback_error) -> lists:map(fun(_) -> erlang:error(callback_failure) end, [1]).
