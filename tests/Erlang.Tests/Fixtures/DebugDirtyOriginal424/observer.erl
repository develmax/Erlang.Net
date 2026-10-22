-module(beam_debug_dirty424).
-export([run/0, run_stacks/0, run_own/0, worker/2, alive_worker/3, capture/1, send_self/1, ready/1, alive/2, magic_copy/0, heap_copy_sharing/0]).

routes() -> [cpu, io, normal, dispatch_cpu, dispatch_io].
scenarios() ->
    [scheduler_type, scheduler_bad, error_notsup, error_undef,
     error_badarith, error_noproc, error_limit, error_badarg, error_other,
     copy_atom, copy_nested, copy_emptytuple, send_invalid, copy_big,
     copy_binary, copy_bits, copy_fun, wait_zero, wait_negative, wait_short,
     wait_bad, wait_longmax, wait_too_big, reschedule_zero, reschedule_two, reschedule_four,
     reschedule_eight, reschedule_odd, reschedule_negative, reschedule_big,
     unknown].
cases() -> [{Route, Scenario} || Route <- routes(), Scenario <- scenarios()] ++
    [{dispatch_bad, atom}, {dispatch_bad, integer}, {dispatch_bad, tuple}].

run() -> [{Name, observe(Name)} || Name <- cases()].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try [{default, Old, run()} | [depth(D) || D <- [0, 1, 2, 8]]]
    after erlang:system_flag(backtrace_depth, Old) end.

depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    Effective = erlang:system_flag(backtrace_depth, Depth),
    {Depth, Effective, run()}.

observe(Name) ->
    {Pid, Ref} = spawn_monitor(?MODULE, worker, [self(), Name]),
    receive
        {Name, Value} ->
            receive {'DOWN', Ref, process, Pid, normal} -> Value end
    after 15000 ->
        exit(Pid, kill),
        receive {'DOWN', Ref, process, Pid, _} -> ok end,
        exit({worker_timeout, Name})
    end.

worker(Parent, Name) ->
    Value = try {value, perform(Name)} catch Class:Reason:Stack -> {exception, Class, Reason, Stack} end,
    Parent ! {Name, Value}.

perform({dispatch_bad, atom}) -> erts_debug:dirty(wrong, scheduler, type);
perform({dispatch_bad, integer}) -> erts_debug:dirty(42, scheduler, type);
perform({dispatch_bad, tuple}) -> erts_debug:dirty({normal}, scheduler, type);
perform({Route, Scenario}) ->
    {Operation, Argument} = arguments(Scenario),
    invoke(Route, Operation, Argument).

arguments(scheduler_type) -> {scheduler, type};
arguments(scheduler_bad) -> {scheduler, wrong};
arguments(error_notsup) -> {error, notsup};
arguments(error_undef) -> {error, undef};
arguments(error_badarith) -> {error, badarith};
arguments(error_noproc) -> {error, noproc};
arguments(error_limit) -> {error, system_limit};
arguments(error_badarg) -> {error, badarg};
arguments(error_other) -> {error, {other, 42}};
arguments(copy_atom) -> {copy, hello};
arguments(copy_nested) -> {copy, {a, [1, 2 | tail], #{b => 3}, <<5:3>>, 1.25}};
arguments(copy_emptytuple) -> {copy, {}};
arguments(send_invalid) -> {send, not_a_pid};
arguments(copy_big) -> {copy, 1 bsl 130};
arguments(copy_binary) -> {copy, binary:copy(<<42>>, 128)};
arguments(copy_bits) -> {copy, <<17:5, 24:5>>};
arguments(copy_fun) -> {copy, fun erlang:self/0};
arguments(wait_zero) -> {wait, 0};
arguments(wait_negative) -> {wait, -9223372036854775808};
arguments(wait_short) -> {wait, 1};
arguments(wait_bad) -> {wait, 1.5};
arguments(wait_longmax) -> {wait, 9223372036854775807};
arguments(wait_too_big) -> {wait, 9223372036854775808};
arguments(reschedule_zero) -> {reschedule, 0};
arguments(reschedule_two) -> {reschedule, 2};
arguments(reschedule_four) -> {reschedule, 4};
arguments(reschedule_eight) -> {reschedule, 8};
arguments(reschedule_odd) -> {reschedule, 9};
arguments(reschedule_negative) -> {reschedule, -2};
arguments(reschedule_big) -> {reschedule, 9223372036854775808};
arguments(unknown) -> {unknown, any}.

invoke(cpu, Operation, Argument) -> erts_debug:dirty_cpu(Operation, Argument);
invoke(io, Operation, Argument) -> erts_debug:dirty_io(Operation, Argument);
invoke(normal, Operation, Argument) -> erts_debug:dirty(normal, Operation, Argument);
invoke(dispatch_cpu, Operation, Argument) -> erts_debug:dirty(dirty_cpu, Operation, Argument);
invoke(dispatch_io, Operation, Argument) -> erts_debug:dirty(dirty_io, Operation, Argument).

run_own() ->
    [{send, Route, send_self(Route)} || Route <- [cpu, io, normal]] ++
    [{ready, Route, ready(Route)} || Route <- [cpu, io, normal]] ++
    [{alive, Route, Operation, alive(Route, Operation)} || Route <- [cpu, io],
         Operation <- [alive_waitexiting, alive_waitexitingonly]] ++
    [{magic_copy, magic_copy()}, {heap_copy_sharing, heap_copy_sharing()}].

send_self(Route) ->
    Result = invoke(Route, send, self()),
    Message = receive M -> M after 1000 -> exit(send_timeout) end,
    {value, Result, Message, self()}.

ready(Route) ->
    Parent = self(),
    {Pid, Ref} = spawn_monitor(fun() -> Parent ! {returned, self(), invoke(Route, ready_wait6_done, Parent)} end),
    First = receive {ready, Pid} = R -> R after 12000 -> exit(ready_timeout) end,
    Second = receive {done, Pid} = D -> D after 12000 -> exit(done_timeout) end,
    Return = receive {returned, Pid, Value} -> Value after 1000 -> exit(return_timeout) end,
    Down = receive {'DOWN', Ref, process, Pid, Reason} -> Reason after 1000 -> exit(down_timeout) end,
    {value, Return, First, Second, Down, Pid}.

alive(Route, Operation) ->
    {Pid, Ref} = spawn_monitor(?MODULE, alive_worker, [Route, Operation, self()]),
    Message = receive {alive, Pid} = A -> A after 10000 -> exit(alive_timeout) end,
    exit(Pid, kill),
    Down = receive {'DOWN', Ref, process, Pid, Reason} -> Reason after 5000 -> exit(alive_down_timeout) end,
    {value, Message, Down, Pid}.

alive_worker(Route, Operation, Parent) -> invoke(Route, Operation, Parent).

magic_copy() ->
    Original = atomics:new(1, []),
    Copies = erts_debug:dirty_cpu(copy, Original),
    [First | _] = Copies,
    ok = atomics:put(First, 1, 123),
    {value, Copies, Original, atomics:get(Original, 1), erts_debug:same(Original, First)}.

heap_copy_sharing() ->
    Child = list_to_tuple([42, 43]),
    Original = {Child, Child},
    [First, Second | _] = Copies = erts_debug:dirty_io(copy, Original),
    {value, Copies, erts_debug:same(Original, First), erts_debug:same(First, Second),
     erts_debug:same(element(1, First), element(2, First))}.

capture([Directory]) ->
    ok = prim_file:write_file(Directory ++ "/values.etf", term_to_binary(run(), [{minor_version, 2}])),
    ok = prim_file:write_file(Directory ++ "/stacks.etf", term_to_binary(run_stacks(), [{minor_version, 2}])),
    ok = prim_file:write_file(Directory ++ "/own-state.etf", term_to_binary(run_own(), [{minor_version, 2}])),
    erlang:halt(0).
