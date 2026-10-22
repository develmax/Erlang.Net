-module(beam_binary_flags_observer395).
-export([cases/0,run/0,run_stacks/0,worker/1,s32/1,l16/1,gb/2,gl/2,gn/2,gu/2,offset/1,cb/2,cl/2,cn/2,put_offset/1,u16/1,u32/1,r16/1,r32/1,fb/2,fl/2,fn/2,mixed/2]).

cases() -> [command_signed32, command_little16, big_signed13, little_signed13, native_signed13, little_dynamic_unsigned, little_unaligned, signed_zero_bits, short_signed, put_little13, put_little_negative, put_big13, put_native13, put_zero_wrong_type, put_truncate, put_little_unaligned, utf16_native, utf32_native, utf16_native_read, utf32_native_read, float_half_big, float_half_little, float_single_big, float_single_little, float_double_native, float_negative_zero, float_half_round_above, float_half_tie, float_half_subnormal, float_half_overflow, float_single_overflow, float_zero_size, float_invalid_size, float_invalid_before_type, float_type, float_bignum_overflow, float_negative_size, float_type_then_bad_size, integer_large, integer_type].

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

value(command_signed32) -> s32(<<16#ffffffff:32,65>>);
value(command_little16) -> l16(<<16#34,16#12,65>>);
value(big_signed13) -> gb(<<16#1abc:13>>,13);
value(little_signed13) -> gl(<<16#bc,16#1a:5>>,13);
value(native_signed13) -> gn(<<16#bc,16#1a:5>>,13);
value(little_dynamic_unsigned) -> gu(<<16#bc,16#1a:5>>,13);
value(little_unaligned) -> offset(<<16#bc,16#1a:5>>);
value(signed_zero_bits) -> gl(<<>>,0);
value(short_signed) -> gl(<<>>,32);
value(put_little13) -> cl(16#1abc,13);
value(put_little_negative) -> cl(-1348,13);
value(put_big13) -> cb(16#1abc,13);
value(put_native13) -> cn(16#1abc,13);
value(put_zero_wrong_type) -> cl(wrong,0);
value(put_truncate) -> cl(16#123456,16);
value(put_little_unaligned) -> put_offset(16#1abc);
value(utf16_native) -> u16(16#1f600);
value(utf32_native) -> u32(16#1f600);
value(utf16_native_read) -> r16(u16(16#1f600));
value(utf32_native_read) -> r32(u32(16#1f600));
value(float_half_big) -> fb(1.5,16);
value(float_half_little) -> fl(1.5,16);
value(float_single_big) -> fb(1.5,32);
value(float_single_little) -> fl(1.5,32);
value(float_double_native) -> fn(1.5,64);
value(float_negative_zero) -> fl(-0.0,32);
value(float_half_round_above) -> fb(1.0004883110523224,16);
value(float_half_tie) -> fb(1.00048828125,16);
value(float_half_subnormal) -> fb(5.960464477539063e-8,16);
value(float_half_overflow) -> fb(1.0e300,16);
value(float_single_overflow) -> fb(1.0e300,32);
value(float_zero_size) -> fb(1.5,0);
value(float_invalid_size) -> fl(1.5,24);
value(float_invalid_before_type) -> fl(wrong,24);
value(float_type) -> fn(wrong,32);
value(float_bignum_overflow) -> fb(1 bsl 1024,64);
value(float_negative_size) -> fb(1.5,-1);
value(float_type_then_bad_size) -> mixed(wrong,-1);
value(integer_large) -> cl(1 bsl 130,128);
value(integer_type) -> cl(wrong,16).

s32(B) -> <<I:32/signed,Rest/bitstring>> = B, {I,Rest}.
l16(B) -> <<I:16/little,Rest/bitstring>> = B, {I,Rest}.
gb(B,N) -> <<I:N/signed-big,Rest/bitstring>> = B, {I,Rest}.
gl(B,N) -> <<I:N/signed-little,Rest/bitstring>> = B, {I,Rest}.
gn(B,N) -> <<I:N/signed-native,Rest/bitstring>> = B, {I,Rest}.
gu(B,N) -> <<I:N/little,Rest/bitstring>> = B, {I,Rest}.
offset(B) -> Bits = <<5:3,B/bitstring>>, <<_:3,I:13/signed-little,Rest/bitstring>> = Bits, {I,Rest}.
cb(I,N) -> <<I:N/big>>.
cl(I,N) -> <<I:N/little>>.
cn(I,N) -> <<I:N/native>>.
put_offset(I) -> <<5:3,I:13/little,1:1>>.
u16(I) -> <<I/utf16-native>>.
u32(I) -> <<I/utf32-native>>.
r16(B) -> <<I/utf16-native,Rest/bitstring>> = B, {I,Rest}.
r32(B) -> <<I/utf32-native,Rest/bitstring>> = B, {I,Rest}.
fb(F,N) -> <<F:N/float-big>>.
fl(F,N) -> <<F:N/float-little>>.
fn(F,N) -> <<F:N/float-native>>.
mixed(F,N) -> <<F:32/float,0:N>>.
