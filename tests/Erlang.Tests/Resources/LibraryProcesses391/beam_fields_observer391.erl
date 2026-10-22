-module(beam_fields_observer391).
-export([cases/0, run/0, run_stacks/0, worker/1,
    read_one/1, skip_one/1, read_offset/1, skip_offset/1,
    construct_utf8/1, construct_float/1, append_fields/3, float_ops/2]).

cases() -> [utf8_ascii, utf8_empty, utf8_multibyte, utf8_four_bytes, utf8_invalid_lead, utf8_truncated, utf8_overlong, utf8_surrogate, utf8_above_maximum, utf8_unaligned, utf8_skip, utf8_skip_invalid, utf8_skip_unaligned, construct_utf8, construct_utf8_invalid, construct_float64, construct_float64_invalid, construct_append_fields, private_float_operations, private_float_divide_zero, original_string_unicode_length, original_string_invalid_utf8, native_float_prefix, native_float_exponent_rollback, native_float_comma, native_float_atom, native_float_improper_tail, native_float_empty, native_float_overflow, native_float_underflow, native_float_negative_zero].

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

value(utf8_ascii) -> read_one(<<65,66>>);
value(utf8_empty) -> read_one(<<>>);
value(utf8_multibyte) -> read_one(<<16#e2,16#82,16#ac,33>>);
value(utf8_four_bytes) -> read_one(<<16#f0,16#9f,16#98,16#80,33>>);
value(utf8_invalid_lead) -> read_one(<<16#80,65>>);
value(utf8_truncated) -> read_one(<<16#e2,16#82>>);
value(utf8_overlong) -> read_one(<<16#e0,16#80,16#80>>);
value(utf8_surrogate) -> read_one(<<16#ed,16#a0,16#80>>);
value(utf8_above_maximum) -> read_one(<<16#f4,16#90,16#80,16#80>>);
value(utf8_unaligned) -> read_offset(<<0:3,16#e2,16#82,16#ac,33>>);
value(utf8_skip) -> skip_one(<<16#e2,16#82,16#ac,33>>);
value(utf8_skip_invalid) -> skip_one(<<16#e2,65,16#ac>>);
value(utf8_skip_unaligned) -> skip_offset(<<0:3,16#e2,16#82,16#ac,33>>);
value(construct_utf8) -> construct_utf8(16#1f600);
value(construct_utf8_invalid) -> construct_utf8(16#d800);
value(construct_float64) -> construct_float(1.25);
value(construct_float64_invalid) -> construct_float(invalid);
value(construct_append_fields) -> append_fields(<<1,2>>,16#3bb,0.5);
value(private_float_operations) -> float_ops(5.5,2.0);
value(private_float_divide_zero) -> float_ops(1.0,0.0);
value(original_string_unicode_length) -> string:length(<<101,16#cc,16#81,16#f0,16#9f,16#98,16#80>>);
value(original_string_invalid_utf8) -> string:length(<<16#e2,16#82>>);
value(native_float_prefix) -> string:list_to_float("12.5tail");
value(native_float_exponent_rollback) -> string:list_to_float("12.5e+tail");
value(native_float_comma) -> string:list_to_float("12,5tail");
value(native_float_atom) -> string:list_to_float(invalid);
value(native_float_improper_tail) -> string:list_to_float([49,50,46,53|improper_tail]);
value(native_float_empty) -> string:list_to_float([]);
value(native_float_overflow) -> string:list_to_float("1.0e309tail");
value(native_float_underflow) -> string:list_to_float("1.0e-400tail");
value(native_float_negative_zero) -> string:list_to_float("-0.0tail").

read_one(Bin) ->
    case Bin of
        <<Code/utf8, Rest/binary>> -> {Code, Rest};
        _ -> nomatch
    end.

skip_one(Bin) ->
    case Bin of
        <<_/utf8, Rest/binary>> -> Rest;
        _ -> nomatch
    end.

read_offset(Bits) ->
    case Bits of
        <<_:3, Code/utf8, Rest/bitstring>> -> {Code, Rest};
        _ -> nomatch
    end.

skip_offset(Bits) ->
    case Bits of
        <<_:3, _/utf8, Rest/bitstring>> -> Rest;
        _ -> nomatch
    end.

construct_utf8(Code) -> <<Code/utf8>>.

construct_float(Value) -> <<Value:64/float>>.

append_fields(Prefix, Code, Value) -> <<Prefix/binary, Code/utf8, Value:64/float>>.

float_ops(A, B) when is_float(A), is_float(B) ->
    {A + B, A - B, A / B}.
