-module(beam_binary_observer390).
-export([cases/0, run/0, run_stacks/0, worker/1]).

cases() -> [at2_valid, at2_invalid, bin_to_list1_valid, bin_to_list1_invalid, bin_to_list2_valid, bin_to_list2_invalid, bin_to_list3_valid, bin_to_list3_invalid, compile_pattern1_valid, compile_pattern1_invalid, copy1_valid, copy1_invalid, copy2_valid, copy2_invalid, decode_hex1_valid, decode_hex1_invalid, decode_unsigned1_valid, decode_unsigned1_invalid, decode_unsigned2_valid, decode_unsigned2_invalid, encode_hex1_valid, encode_hex1_invalid, encode_hex2_valid, encode_hex2_invalid, encode_unsigned1_valid, encode_unsigned1_invalid, encode_unsigned2_valid, encode_unsigned2_invalid, first1_valid, first1_invalid, join2_valid, join2_invalid, last1_valid, last1_invalid, list_to_bin1_valid, list_to_bin1_invalid, longest_common_prefix1_valid, longest_common_prefix1_invalid, longest_common_suffix1_valid, longest_common_suffix1_invalid, match2_valid, match2_invalid, match3_valid, match3_invalid, matches2_valid, matches2_invalid, matches3_valid, matches3_invalid, part2_valid, part2_invalid, part3_valid, part3_invalid, referenced_byte_size1_valid, referenced_byte_size1_invalid, replace3_valid, replace3_invalid, replace4_valid, replace4_invalid, split2_valid, split2_invalid, split3_valid, split3_invalid, bin_list_zero_end, bin_list_bad_subject_and_range, bin_list_negative_range, hex_encode_long, hex_uppercase, hex_decode_long, hex_decode_odd, hex_decode_bits, join_empty, join_singleton, join_improper, join_bad_separator, replace_nomatch, replace_insert_single, replace_insert_sorted, replace_insert_outside, replace_insert_nomatch, replace_callback_binary, replace_callback_iodata, replace_callback_bad, replace_callback_error, replace_callback_throw, replace_callback_exit, replace_scope, replace_compiled, match_compiled, unsigned_empty, unsigned_zero, copy_zero, split_trim, split_scope, bit_offset_binary].

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
    Value = observe(Case),
    [{Case, Value} | rows(Rest)].

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
    put(binary390_trace, []),
    try value(Case) of Value -> {returned, Value, trace()}
    catch Class:Reason:Stack -> {caught, Class, Reason, Stack, trace()}
    after erase(binary390_trace)
    end.

value(at2_valid) -> binary:at(<<0,255,7>>,1);
value(at2_invalid) -> binary:at(<<1>>,1);
value(bin_to_list1_valid) -> binary:bin_to_list(<<0,255,7>>);
value(bin_to_list1_invalid) -> binary:bin_to_list(bad);
value(bin_to_list2_valid) -> binary:bin_to_list(<<0,255,7>>,{3,-2});
value(bin_to_list2_invalid) -> binary:bin_to_list(<<1>>,bad);
value(bin_to_list3_valid) -> binary:bin_to_list(<<0,255,7>>,1,2);
value(bin_to_list3_invalid) -> binary:bin_to_list(<<1>>,0,2);
value(compile_pattern1_valid) -> compiled_observable([<<"ab">>,<<"bc">>]);
value(compile_pattern1_invalid) -> binary:compile_pattern(<<>>);
value(copy1_valid) -> binary:copy(<<0,255>>);
value(copy1_invalid) -> binary:copy(bad);
value(copy2_valid) -> binary:copy(<<0,255>>,3);
value(copy2_invalid) -> binary:copy(<<1>>,-1);
value(decode_hex1_valid) -> binary:decode_hex(<<"00aAfF10">>);
value(decode_hex1_invalid) -> binary:decode_hex(<<"G0">>);
value(decode_unsigned1_valid) -> binary:decode_unsigned(<<1,0,255>>);
value(decode_unsigned1_invalid) -> binary:decode_unsigned(bad);
value(decode_unsigned2_valid) -> binary:decode_unsigned(<<1,0,255>>,little);
value(decode_unsigned2_invalid) -> binary:decode_unsigned(<<1>>,bad);
value(encode_hex1_valid) -> binary:encode_hex(<<0,15,16,255>>);
value(encode_hex1_invalid) -> binary:encode_hex(bad);
value(encode_hex2_valid) -> binary:encode_hex(<<0,15,16,255>>,lowercase);
value(encode_hex2_invalid) -> binary:encode_hex(<<1>>,bad);
value(encode_unsigned1_valid) -> binary:encode_unsigned(16#0100FF);
value(encode_unsigned1_invalid) -> binary:encode_unsigned(-1);
value(encode_unsigned2_valid) -> binary:encode_unsigned(16#0100FF,little);
value(encode_unsigned2_invalid) -> binary:encode_unsigned(1,bad);
value(first1_valid) -> binary:first(<<0,255>>);
value(first1_invalid) -> binary:first(<<>>);
value(join2_valid) -> binary:join([<<"a">>,<<>>,<<"b">>],<<",">>);
value(join2_invalid) -> binary:join([<<"a">>,bad],<<",">>);
value(last1_valid) -> binary:last(<<0,255>>);
value(last1_invalid) -> binary:last(<<>>);
value(list_to_bin1_valid) -> binary:list_to_bin([0,[255],<<7>>]);
value(list_to_bin1_invalid) -> binary:list_to_bin([256]);
value(longest_common_prefix1_valid) -> binary:longest_common_prefix([<<"abc">>,<<"abd">>,<<"ab">>]);
value(longest_common_prefix1_invalid) -> binary:longest_common_prefix([bad]);
value(longest_common_suffix1_valid) -> binary:longest_common_suffix([<<"abc">>,<<"dbc">>,<<"bc">>]);
value(longest_common_suffix1_invalid) -> binary:longest_common_suffix([bad]);
value(match2_valid) -> binary:match(<<"ababa">>,[<<"a">>,<<"ab">>]);
value(match2_invalid) -> binary:match(<<"a">>,<<>>);
value(match3_valid) -> binary:match(<<"ababa">>,<<"a">>,[{scope,{1,4}}]);
value(match3_invalid) -> binary:match(<<"a">>,<<"a">>,[bad]);
value(matches2_valid) -> binary:matches(<<"ababa">>,<<"ab">>);
value(matches2_invalid) -> binary:matches(<<"a">>,<<>>);
value(matches3_valid) -> binary:matches(<<"ababa">>,<<"a">>,[{scope,{1,4}}]);
value(matches3_invalid) -> binary:matches(<<"a">>,<<"a">>,[bad]);
value(part2_valid) -> binary:part(<<0,255,7>>,{3,-2});
value(part2_invalid) -> binary:part(<<1>>,{0,2});
value(part3_valid) -> binary:part(<<0,255,7>>,3,-2);
value(part3_invalid) -> binary:part(<<1>>,0,2);
value(referenced_byte_size1_valid) -> binary:referenced_byte_size(<<0,255,7>>);
value(referenced_byte_size1_invalid) -> binary:referenced_byte_size(bad);
value(replace3_valid) -> binary:replace(<<"ababa">>,<<"a">>,<<"X">>);
value(replace3_invalid) -> binary:replace(<<"a">>,<<"a">>,bad);
value(replace4_valid) -> binary:replace(<<"ababa">>,<<"a">>,<<"[]">>,[global,{insert_replaced,[1,1]}]);
value(replace4_invalid) -> binary:replace(<<"a">>,<<"a">>,<<"X">>,[bad]);
value(split2_valid) -> binary:split(<<"ababa">>,<<"b">>);
value(split2_invalid) -> binary:split(<<"a">>,<<>>);
value(split3_valid) -> binary:split(<<"a,,b,,">>,<<",">>,[global,trim_all]);
value(split3_invalid) -> binary:split(<<"a">>,<<"a">>,[bad]);
value(bin_list_zero_end) -> binary:bin_to_list(<<1>>,1,0);
value(bin_list_bad_subject_and_range) -> binary:bin_to_list(bad,-1,0);
value(bin_list_negative_range) -> binary:bin_to_list(<<1,2,3>>,3,-3);
value(hex_encode_long) -> binary:encode_hex(<<0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,255>>);
value(hex_uppercase) -> binary:encode_hex(<<0,15,255>>,uppercase);
value(hex_decode_long) -> binary:decode_hex(<<"000102030405060708090A0B0C0D0E0FFF">>);
value(hex_decode_odd) -> binary:decode_hex(<<"A">>);
value(hex_decode_bits) -> binary:decode_hex(<<0:9>>);
value(join_empty) -> binary:join([],<<",">>);
value(join_singleton) -> binary:join([<<"a">>],<<",">>);
value(join_improper) -> binary:join([<<"a">>|bad],<<",">>);
value(join_bad_separator) -> binary:join([],bad);
value(replace_nomatch) -> binary:replace(<<"abc">>,<<"x">>,<<"z">>,[global]);
value(replace_insert_single) -> binary:replace(<<"ababa">>,<<"a">>,<<"[]">>,[global,{insert_replaced,1}]);
value(replace_insert_sorted) -> binary:replace(<<"ababa">>,<<"a">>,<<"[-]">>,[global,{insert_replaced,[2,1]}]);
value(replace_insert_outside) -> binary:replace(<<"a">>,<<"a">>,<<"x">>,[{insert_replaced,2}]);
value(replace_insert_nomatch) -> binary:replace(<<"a">>,<<"z">>,<<"x">>,[{insert_replaced,2}]);
value(replace_callback_binary) -> binary:replace(<<"ababa">>,<<"a">>,fun callback_binary/1,[global]);
value(replace_callback_iodata) -> binary:replace(<<"ababa">>,<<"a">>,fun callback_iodata/1,[global]);
value(replace_callback_bad) -> binary:replace(<<"ababa">>,<<"a">>,fun callback_bad/1,[global]);
value(replace_callback_error) -> binary:replace(<<"ababa">>,<<"a">>,fun callback_error/1);
value(replace_callback_throw) -> binary:replace(<<"ababa">>,<<"a">>,fun callback_throw/1,[global]);
value(replace_callback_exit) -> binary:replace(<<"ababa">>,<<"a">>,fun callback_exit/1,[global]);
value(replace_scope) -> binary:replace(<<"ababa">>,<<"a">>,<<"X">>,[global,{scope,{1,4}}]);
value(replace_compiled) -> binary:replace(<<"ababa">>,binary:compile_pattern(<<"a">>),<<"X">>);
value(match_compiled) -> binary:match(<<"ababa">>,binary:compile_pattern([<<"a">>,<<"ab">>]));
value(unsigned_empty) -> binary:decode_unsigned(<<>>);
value(unsigned_zero) -> binary:encode_unsigned(0);
value(copy_zero) -> binary:copy(<<"ab">>,0);
value(split_trim) -> binary:split(<<",a,,">>,<<",">>,[global,trim]);
value(split_scope) -> binary:split(<<"banana">>,<<"a">>,[{scope,{2,3}}]);
value(bit_offset_binary) -> binary:bin_to_list(shifted(<<1,127,255>>)).

trace() -> reverse(get(binary390_trace), []).
reverse([], Acc) -> Acc;
reverse([H | T], Acc) -> reverse(T, [H | Acc]).
note(Value) -> put(binary390_trace, [Value | get(binary390_trace)]), ok.
callback_binary(Value) -> note(Value), <<"X">>.
callback_iodata(Value) -> note(Value), [<<"[">>, Value, <<"]">>].
callback_bad(Value) -> note(Value), [256].
callback_error(Value) -> note(Value), erlang:error(binary390_callback_error).
callback_throw(Value) -> note(Value), throw(binary390_callback_throw).
callback_exit(Value) -> note(Value), exit(binary390_callback_exit).

compiled_observable(Pattern) ->
    CP = binary:compile_pattern(Pattern),
    CP2 = binary:compile_pattern(Pattern),
    {Tag, Reference} = CP,
    {Tag2, Reference2} = CP2,
    {Tag, is_reference(Reference), Tag2, is_reference(Reference2), Reference =:= Reference2,
        binary:match(<<"abcabc">>, CP), binary:matches(<<"abcabc">>, CP),
        binary:split(<<"abcabc">>, CP)}.

shifted(Binary) ->
    Size = byte_size(Binary),
    Wrapped = <<0:3, Binary/binary, 0:5>>,
    <<_:3, Result:Size/binary, _:5>> = Wrapped,
    Result.
