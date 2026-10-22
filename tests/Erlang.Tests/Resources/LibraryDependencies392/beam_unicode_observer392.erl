-module(beam_unicode_observer392).
-export([cases/0,run/0,run_stacks/0,worker/1,unaligned/0]).

cases() -> [empty_binary, empty_list, ascii_binary, ascii_list, latin_binary_binary, latin_binary_list, scalars_binary, scalars_list, mixed_binary, mixed_list, split_four_binary, split_four_list, binary_tail_binary, binary_tail_list, empty_between_binary, empty_between_list, short_two_binary, short_two_list, short_three_binary, short_three_list, short_four_binary, short_four_list, short_nested_binary, short_nested_list, overlong_binary, overlong_list, short_overlong_binary, short_overlong_list, bad_continuation_binary, bad_continuation_list, short_bad_continuation_binary, short_bad_continuation_list, encoded_surrogate_binary, encoded_surrogate_list, above_max_binary, above_max_list, unexpected_continuation_binary, unexpected_continuation_list, pending_integer_binary, pending_integer_list, pending_bad_binary_binary, pending_bad_binary_list, pending_short_bad_binary, pending_short_bad_list, invalid_scalar_binary, invalid_scalar_list, negative_scalar_binary, negative_scalar_list, above_scalar_binary, above_scalar_list, latin_scalar_binary, latin_scalar_list, atom_top_binary, atom_top_list, atom_nested_binary, atom_nested_list, improper_binary, improper_list, bitstring_binary, bitstring_list, nested_bitstring_binary, nested_bitstring_list, scalar_then_atom_binary, scalar_then_atom_list, big_then_atom_binary, big_then_atom_list, utf16_big_binary, utf16_big_list, utf16_little_binary, utf16_little_list, utf16_short_binary, utf16_short_list, utf16_unpaired_binary, utf16_unpaired_list, utf32_big_binary, utf32_big_list, utf32_little_binary, utf32_little_list, utf32_short_binary, utf32_short_list, invalid_encoding_binary, invalid_encoding_list, invalid_endian_binary, invalid_endian_list, bad_encoding_before_data_binary, bad_encoding_before_data_list, singleton_error_binary, singleton_error_list, seven_empty, seven_ascii, seven_high, seven_nested, seven_atom, seven_bit, seven_unaligned, binary_one, list_one, binary_three, latin_output_error].

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

value(empty_binary) -> unicode:characters_to_binary([],utf8);
value(empty_list) -> unicode:characters_to_list([],utf8);
value(ascii_binary) -> unicode:characters_to_binary(<<0,65,127>>,unicode);
value(ascii_list) -> unicode:characters_to_list(<<0,65,127>>,unicode);
value(latin_binary_binary) -> unicode:characters_to_binary(<<0,128,255>>,latin1);
value(latin_binary_list) -> unicode:characters_to_list(<<0,128,255>>,latin1);
value(scalars_binary) -> unicode:characters_to_binary([0,127,128,2047,2048,16#d7ff,16#e000,16#ffff,16#10000,16#10ffff],utf8);
value(scalars_list) -> unicode:characters_to_list([0,127,128,2047,2048,16#d7ff,16#e000,16#ffff,16#10000,16#10ffff],utf8);
value(mixed_binary) -> unicode:characters_to_binary([65,[<<226>>,[],[<<130>>,<<172>>]],66],utf8);
value(mixed_list) -> unicode:characters_to_list([65,[<<226>>,[],[<<130>>,<<172>>]],66],utf8);
value(split_four_binary) -> unicode:characters_to_binary([<<240>>,<<159>>,<<152>>,<<128>>],utf8);
value(split_four_list) -> unicode:characters_to_list([<<240>>,<<159>>,<<152>>,<<128>>],utf8);
value(binary_tail_binary) -> unicode:characters_to_binary([65|<<226,130,172>>],utf8);
value(binary_tail_list) -> unicode:characters_to_list([65|<<226,130,172>>],utf8);
value(empty_between_binary) -> unicode:characters_to_binary([<<226>>,[],[[]],<<130,172>>],utf8);
value(empty_between_list) -> unicode:characters_to_list([<<226>>,[],[[]],<<130,172>>],utf8);
value(short_two_binary) -> unicode:characters_to_binary(<<194>>,utf8);
value(short_two_list) -> unicode:characters_to_list(<<194>>,utf8);
value(short_three_binary) -> unicode:characters_to_binary(<<65,226,130>>,utf8);
value(short_three_list) -> unicode:characters_to_list(<<65,226,130>>,utf8);
value(short_four_binary) -> unicode:characters_to_binary(<<240,159,152>>,utf8);
value(short_four_list) -> unicode:characters_to_list(<<240,159,152>>,utf8);
value(short_nested_binary) -> unicode:characters_to_binary([65,[<<226,130>>]],utf8);
value(short_nested_list) -> unicode:characters_to_list([65,[<<226,130>>]],utf8);
value(overlong_binary) -> unicode:characters_to_binary(<<65,192,128>>,utf8);
value(overlong_list) -> unicode:characters_to_list(<<65,192,128>>,utf8);
value(short_overlong_binary) -> unicode:characters_to_binary(<<192>>,utf8);
value(short_overlong_list) -> unicode:characters_to_list(<<192>>,utf8);
value(bad_continuation_binary) -> unicode:characters_to_binary(<<226,65,172>>,utf8);
value(bad_continuation_list) -> unicode:characters_to_list(<<226,65,172>>,utf8);
value(short_bad_continuation_binary) -> unicode:characters_to_binary(<<226,65>>,utf8);
value(short_bad_continuation_list) -> unicode:characters_to_list(<<226,65>>,utf8);
value(encoded_surrogate_binary) -> unicode:characters_to_binary(<<237,160,128>>,utf8);
value(encoded_surrogate_list) -> unicode:characters_to_list(<<237,160,128>>,utf8);
value(above_max_binary) -> unicode:characters_to_binary(<<244,144,128,128>>,utf8);
value(above_max_list) -> unicode:characters_to_list(<<244,144,128,128>>,utf8);
value(unexpected_continuation_binary) -> unicode:characters_to_binary(<<128>>,utf8);
value(unexpected_continuation_list) -> unicode:characters_to_list(<<128>>,utf8);
value(pending_integer_binary) -> unicode:characters_to_binary([<<226>>,65,66],utf8);
value(pending_integer_list) -> unicode:characters_to_list([<<226>>,65,66],utf8);
value(pending_bad_binary_binary) -> unicode:characters_to_binary([65,[<<226>>,<<65,66>>],67],utf8);
value(pending_bad_binary_list) -> unicode:characters_to_list([65,[<<226>>,<<65,66>>],67],utf8);
value(pending_short_bad_binary) -> unicode:characters_to_binary([<<240>>,<<65>>],utf8);
value(pending_short_bad_list) -> unicode:characters_to_list([<<240>>,<<65>>],utf8);
value(invalid_scalar_binary) -> unicode:characters_to_binary([65,16#d800,66],utf8);
value(invalid_scalar_list) -> unicode:characters_to_list([65,16#d800,66],utf8);
value(negative_scalar_binary) -> unicode:characters_to_binary([65,-1,66],utf8);
value(negative_scalar_list) -> unicode:characters_to_list([65,-1,66],utf8);
value(above_scalar_binary) -> unicode:characters_to_binary([65,16#110000,66],utf8);
value(above_scalar_list) -> unicode:characters_to_list([65,16#110000,66],utf8);
value(latin_scalar_binary) -> unicode:characters_to_binary([255,256,65],latin1);
value(latin_scalar_list) -> unicode:characters_to_list([255,256,65],latin1);
value(atom_top_binary) -> unicode:characters_to_binary(bad_data,utf8);
value(atom_top_list) -> unicode:characters_to_list(bad_data,utf8);
value(atom_nested_binary) -> unicode:characters_to_binary([65,[bad_data],66],utf8);
value(atom_nested_list) -> unicode:characters_to_list([65,[bad_data],66],utf8);
value(improper_binary) -> unicode:characters_to_binary([65|66],utf8);
value(improper_list) -> unicode:characters_to_list([65|66],utf8);
value(bitstring_binary) -> unicode:characters_to_binary(<<1:1>>,utf8);
value(bitstring_list) -> unicode:characters_to_list(<<1:1>>,utf8);
value(nested_bitstring_binary) -> unicode:characters_to_binary([65,<<1:1>>],utf8);
value(nested_bitstring_list) -> unicode:characters_to_list([65,<<1:1>>],utf8);
value(scalar_then_atom_binary) -> unicode:characters_to_binary([16#d800,bad_data],utf8);
value(scalar_then_atom_list) -> unicode:characters_to_list([16#d800,bad_data],utf8);
value(big_then_atom_binary) -> unicode:characters_to_binary([1 bsl 100,bad_data],utf8);
value(big_then_atom_list) -> unicode:characters_to_list([1 bsl 100,bad_data],utf8);
value(utf16_big_binary) -> unicode:characters_to_binary([<<0,65,216>>,<<61,222,0>>],{utf16,big});
value(utf16_big_list) -> unicode:characters_to_list([<<0,65,216>>,<<61,222,0>>],{utf16,big});
value(utf16_little_binary) -> unicode:characters_to_binary(<<65,0,61,216,0,222>>,{utf16,little});
value(utf16_little_list) -> unicode:characters_to_list(<<65,0,61,216,0,222>>,{utf16,little});
value(utf16_short_binary) -> unicode:characters_to_binary(<<216>>,utf16);
value(utf16_short_list) -> unicode:characters_to_list(<<216>>,utf16);
value(utf16_unpaired_binary) -> unicode:characters_to_binary(<<216,0,0,65>>,utf16);
value(utf16_unpaired_list) -> unicode:characters_to_list(<<216,0,0,65>>,utf16);
value(utf32_big_binary) -> unicode:characters_to_binary(<<0,1,246,0>>,utf32);
value(utf32_big_list) -> unicode:characters_to_list(<<0,1,246,0>>,utf32);
value(utf32_little_binary) -> unicode:characters_to_binary(<<0,246,1,0>>,{utf32,little});
value(utf32_little_list) -> unicode:characters_to_list(<<0,246,1,0>>,{utf32,little});
value(utf32_short_binary) -> unicode:characters_to_binary(<<0,1>>,utf32);
value(utf32_short_list) -> unicode:characters_to_list(<<0,1>>,utf32);
value(invalid_encoding_binary) -> unicode:characters_to_binary([],wrong_encoding);
value(invalid_encoding_list) -> unicode:characters_to_list([],wrong_encoding);
value(invalid_endian_binary) -> unicode:characters_to_binary(<<0>>,{utf16,wrong});
value(invalid_endian_list) -> unicode:characters_to_list(<<0>>,{utf16,wrong});
value(bad_encoding_before_data_binary) -> unicode:characters_to_binary(bad_data,{bad,wrong});
value(bad_encoding_before_data_list) -> unicode:characters_to_list(bad_data,{bad,wrong});
value(singleton_error_binary) -> unicode:characters_to_binary([<<65,255>>],utf8);
value(singleton_error_list) -> unicode:characters_to_list([<<65,255>>],utf8);
value(seven_empty) -> unicode:bin_is_7bit(<<>>);
value(seven_ascii) -> unicode:bin_is_7bit(<<0,127>>);
value(seven_high) -> unicode:bin_is_7bit(<<128>>);
value(seven_nested) -> unicode:bin_is_7bit([<<65>>]);
value(seven_atom) -> unicode:bin_is_7bit(not_binary);
value(seven_bit) -> unicode:bin_is_7bit(<<1:1>>);
value(seven_unaligned) -> unicode:bin_is_7bit(unaligned());
value(binary_one) -> unicode:characters_to_binary([65,16#20ac]);
value(list_one) -> unicode:characters_to_list(<<65,226,130,172>>);
value(binary_three) -> unicode:characters_to_binary([65,16#1f600],unicode,{utf16,little});
value(latin_output_error) -> unicode:characters_to_binary([65,16#20ac],unicode,latin1).

unaligned() ->
    Bits = <<5:3,65,127,0:2>>,
    <<_:3,Binary:2/binary,_:2>> = Bits,
    Binary.
