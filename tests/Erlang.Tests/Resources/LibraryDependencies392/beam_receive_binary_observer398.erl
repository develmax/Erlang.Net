-module(beam_receive_binary_observer398).
-export([cases/0,run/0,run_stacks/0,worker/1,rbin/1]).

cases() -> [empty_tail, binary_tail, nonbinary, short, wrong_serial, wrong_owner, nonbyte_tail, empty_input, malformed_tuple, skip_then_accept, repeat, unaligned_source, large_serial, exact_owner_key, structured_owner, active_try, selected_error_stack, empty_mailbox].

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

value(empty_tail) -> mail([{owner,{data,<<1:32>>}},tail],fun()->rbin(owner)end);
value(binary_tail) -> mail([{owner,{data,<<1:32,42,43>>}},tail],fun()->rbin(owner)end);
value(nonbinary) -> mail([{owner,{data,bad}},tail],fun()->rbin(owner)end);
value(short) -> mail([{owner,{data,<<1:24>>}},tail],fun()->rbin(owner)end);
value(wrong_serial) -> mail([{owner,{data,<<2:32,42>>}},tail],fun()->rbin(owner)end);
value(wrong_owner) -> mail([{wrong,{data,<<1:32,42>>}},tail],fun()->rbin(owner)end);
value(nonbyte_tail) -> mail([{owner,{data,<<1:32,1:1>>}},tail],fun()->rbin(owner)end);
value(empty_input) -> mail([{owner,{data,<<>>}},tail],fun()->rbin(owner)end);
value(malformed_tuple) -> mail([bad,{owner}, {owner,{data}}, {owner,{wrong,<<1:32>>}},tail],fun()->rbin(owner)end);
value(skip_then_accept) -> mail([{owner,{data,<<1:24>>}},{owner,{data,<<2:32>>}},{owner,{data,<<1:32,42>>}},tail],fun()->rbin(owner)end);
value(repeat) -> mail([{owner,{data,<<2:32>>}},{owner,{data,<<1:32,41>>}},{owner,{data,<<1:32,42>>}},tail],fun()->{rbin(owner),rbin(owner)}end);
value(unaligned_source) -> begin <<_:3,Bits:40/bitstring>> = <<5:3,1:32,42:8>>, mail([{owner,{data,Bits}},tail],fun()->rbin(owner)end) end;
value(large_serial) -> mail([{owner,{data,<<4294967295:32,42>>}},tail],fun()->rbin(owner)end);
value(exact_owner_key) -> mail([{1.0,{data,<<1:32,41>>}},{1,{data,<<1:32,42>>}},tail],fun()->rbin(1)end);
value(structured_owner) -> mail([{{owner,1},{data,<<1:32,42>>}},tail],fun()->rbin({owner,1})end);
value(active_try) -> mail([{owner,{data,<<1:32,42>>}},tail],fun()->try rbin(owner) after ok end end);
value(selected_error_stack) -> mail([{owner,{data,<<1:32,42>>}},tail],fun()->Data=rbin(owner),erlang:error({selected,Data})end);
value(empty_mailbox) -> mail([],fun()->rbin(owner)end).

mail(Messages,Fun) ->
    send_all(Messages),
    try Fun() of Value -> {Value,drain()}
    after drain()
    end.
send_all([]) -> ok;
send_all([M|Rest]) -> self() ! M, send_all(Rest).
drain() -> receive M -> [M|drain()] after 0 -> [] end.

rbin(Owner) ->
    receive
        {Owner,{data,<<Serial:32,Data/binary>>}} when Serial =:= 1 -> Data
    after 0 -> timeout
    end.
