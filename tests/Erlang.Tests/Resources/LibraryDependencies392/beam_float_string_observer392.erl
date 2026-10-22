-module(beam_float_string_observer392).
-export([cases/0,run/0,run_stacks/0,worker/1,fb/2,fl/2,fn/2,fo/1,s/1,so/1]).

cases() -> [half_big, half_little, single_big, single_little, double_big, double_little, single_native, zero_width, negative_zero, half_subnormal, single_subnormal, double_subnormal, float_unaligned, half_infinite, half_nan, single_infinite, single_nan, double_infinite, double_nan, short_float, invalid_width, negative_width, atom_width, string_match, string_mismatch, string_short, string_unaligned, string_nonbyte_tail].

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

value(half_big) -> fb(<<16#3e00:16>>,16);
value(half_little) -> fl(<<0,62>>,16);
value(single_big) -> fb(<<16#3fc00000:32>>,32);
value(single_little) -> fl(<<0,0,192,63>>,32);
value(double_big) -> fb(<<16#3ff8000000000000:64>>,64);
value(double_little) -> fl(<<0,0,0,0,0,0,248,63>>,64);
value(single_native) -> fn(<<0,0,192,63>>,32);
value(zero_width) -> fb(<<>>,0);
value(negative_zero) -> fb(<<16#8000000000000000:64>>,64);
value(half_subnormal) -> fb(<<1:16>>,16);
value(single_subnormal) -> fb(<<1:32>>,32);
value(double_subnormal) -> fb(<<1:64>>,64);
value(float_unaligned) -> fo(<<16#3ff8000000000000:64>>);
value(half_infinite) -> fb(<<16#7c00:16>>,16);
value(half_nan) -> fb(<<16#7e01:16>>,16);
value(single_infinite) -> fb(<<16#7f800000:32>>,32);
value(single_nan) -> fb(<<16#7fc00001:32>>,32);
value(double_infinite) -> fb(<<16#7ff0000000000000:64>>,64);
value(double_nan) -> fb(<<16#7ff8000000000001:64>>,64);
value(short_float) -> fb(<<0,0,0>>,32);
value(invalid_width) -> fb(<<0,0,0>>,24);
value(negative_width) -> fb(<<0>>,-1);
value(atom_width) -> fb(<<0>>,wrong);
value(string_match) -> s(<<"application/erlang+html",65>>);
value(string_mismatch) -> s(<<"application/erlang+htmX",65>>);
value(string_short) -> s(<<"application/erlang+htm">>);
value(string_unaligned) -> so(<<"application/erlang+html",65>>);
value(string_nonbyte_tail) -> s(<<"application/erlang+html",1:1>>).

fb(B,N) -> <<F:N/float-big,Rest/bitstring>> = B, {F,Rest}.
fl(B,N) -> <<F:N/float-little,Rest/bitstring>> = B, {F,Rest}.
fn(B,N) -> <<F:N/float-native,Rest/bitstring>> = B, {F,Rest}.
fo(B) ->
    Bits = <<5:3,B/bitstring>>,
    <<_:3,F:64/float-big,Rest/bitstring>> = Bits,
    {F,Rest}.
s(B) ->
    case B of
        <<"application/erlang+html",Rest/bitstring>> -> {match,Rest};
        _ -> no_match
    end.
so(B) ->
    Bits = <<5:3,B/bitstring>>,
    case Bits of
        <<_:3,"application/erlang+html",Rest/bitstring>> -> {match,Rest};
        _ -> no_match
    end.
