-module(beam_unicode_fields_observer392).
-export([cases/0,run/0,run_stacks/0,worker/1,g16b/1,g16l/1,g32b/1,g32l/1,offset/1,c16b/1,c16l/1,c32b/1,c32l/1,mixed16/2,mixed32/2]).

cases() -> [get16_big, get16_little, get32_big, get32_little, get16_unaligned, get16_low, get16_short, get16_bad_pair, get32_surrogate, get32_above, get32_short, put16_big, put16_little, put32_big, put32_little, put16_type, put32_type, put16_surrogate, put32_surrogate, precedence16, precedence32].

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

value(get16_big) -> g16b(<<216,61,222,0>>);
value(get16_little) -> g16l(<<61,216,0,222>>);
value(get32_big) -> g32b(<<0,1,246,0>>);
value(get32_little) -> g32l(<<0,246,1,0>>);
value(get16_unaligned) -> offset(<<216,61,222,0>>);
value(get16_low) -> g16b(<<220,0>>);
value(get16_short) -> g16b(<<216,61>>);
value(get16_bad_pair) -> g16b(<<216,61,0,65>>);
value(get32_surrogate) -> g32b(<<0,0,216,0>>);
value(get32_above) -> g32b(<<0,17,0,0>>);
value(get32_short) -> g32b(<<0,1,246>>);
value(put16_big) -> c16b(16#1f600);
value(put16_little) -> c16l(16#1f600);
value(put32_big) -> c32b(16#1f600);
value(put32_little) -> c32l(16#1f600);
value(put16_type) -> c16b(wrong);
value(put32_type) -> c32b(wrong);
value(put16_surrogate) -> c16b(16#d800);
value(put32_surrogate) -> c32b(16#d800);
value(precedence16) -> mixed16(wrong,wrong);
value(precedence32) -> mixed32(wrong,wrong).

g16b(B) -> <<C/utf16-big,Rest/bitstring>> = B, {C,Rest}.
g16l(B) -> <<C/utf16-little,Rest/bitstring>> = B, {C,Rest}.
g32b(B) -> <<C/utf32-big,Rest/bitstring>> = B, {C,Rest}.
g32l(B) -> <<C/utf32-little,Rest/bitstring>> = B, {C,Rest}.
offset(B) ->
    Bits = <<5:3,B/bitstring>>,
    <<_:3,C/utf16-big,Rest/bitstring>> = Bits,
    {C,Rest}.
c16b(C) -> <<C/utf16-big>>.
c16l(C) -> <<C/utf16-little>>.
c32b(C) -> <<C/utf32-big>>.
c32l(C) -> <<C/utf32-little>>.
mixed16(C,N) -> <<C/utf16,0:N>>.
mixed32(C,N) -> <<C/utf32,0:N>>.
