-module(beam_wide_integer_observer392).
-export([cases/0,run/0,run_stacks/0,worker/1,extract/1,offset/1]).

cases() -> [zero, maximum, high_bit, ascending, short, long, wrong_type, unaligned].

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

value(zero) -> extract(<<0:128>>);
value(maximum) -> extract(<<16#ffffffffffffffffffffffffffffffff:128>>);
value(high_bit) -> extract(<<16#80000000000000000000000000000000:128>>);
value(ascending) -> extract(<<0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15>>);
value(short) -> extract(<<0:127>>);
value(long) -> extract(<<0:129>>);
value(wrong_type) -> extract(not_binary);
value(unaligned) -> offset(<<5:3,16#fedcba98765432100123456789abcdef:128,1:2>>).

extract(Binary) ->
    case Binary of
        <<Value:128>> -> Value;
        _ -> erlang:error({bad_digest,Binary})
    end.

offset(Binary) ->
    case Binary of
        <<_:3,Value:128,Tail/bitstring>> -> {Value,Tail};
        _ -> erlang:error({bad_offset_digest,Binary})
    end.
