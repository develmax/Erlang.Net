using Erlang;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamGeneralTryEmissionChecks
{
    internal const string Source = """
        -module(beam_general_try_probe).
        -export([outer/1]).
        run(Value) ->
            try {ok, element(1, make(Value))}
            catch error:Reason:Stack -> {caught,Reason,is_list(Stack)} end.
        outer(guard) -> {range(0,1),range(1,1),range(1.0,1),range(2,1)};
        outer(dispatch) -> {forward(second, value),forward(first, value),forward(unknown, value)};
        outer(choice) -> {choose({7,a}),choose({2,b}),choose({7,a,b}),choose({3,a}),choose(other)};
        outer(arguments) ->
            Result = pair(identity(first), Bound = identity(second), third),
            {Result, Bound};
        outer(of_case) -> {observe_of(ready),observe_of(other),observe_of(fail)};
        outer(protected_iolist) -> {encode_iolist([65,<<66>>]),encode_iolist(bad)};
        outer(wrapped_catch) ->
            {observe_wrapped(value, [65]), observe_wrapped(value, bad),
             observe_wrapped(exit, tagged), observe_wrapped(throw, ball)};
        outer(wrapped_decode) -> {observe_decode(key, {payload, rest}), observe_decode(key, bad)};
        outer(decoded_fields) -> {decode_fields(0, 3), decode_fields(7, 1.5), observe_range(4)};
        outer(terminal_decoded_range) ->
            {terminal_decoded_range(0, tag), terminal_decoded_range(1.5, tag),
             observe_terminal_range(-1), observe_terminal_range(4)};
        outer(length_encoding) -> {length_probe(127),length_probe(128),length_probe(1.5),observe_length(129)};
        outer(tagged_length) -> {tagged_length_probe([<<4>>], [65], 1), tagged_length_probe([], [65], 1)};
        outer(octet_accumulator) -> {octet_probe(65537, []), octet_probe(0, []), octet_probe(1, tail)};
        outer(signed_octet_accumulator) ->
            {signed_dispatch_guarded(128), signed_dispatch_guarded(-129), signed_pos_probe(0, [127.5])};
        outer(match_collect) ->
            {match_collect_probe([{4,<<65>>}], tag), match_collect_probe(<<66>>, tag), observe_match_collect(bad)};
        outer(collect_parts) ->
            {collect_probe([{4,[{4,<<65>>},{4,<<66>>}]},{4,<<67>>}]),
             collect_probe([{3,<<2,252>>},{3,<<1,128>>}]),
             collect_probe([{3,<<>>},{4,<<65>>}])};
        outer(tag_match) ->
            {match_tag_probe({tag,value}, [tag]),
             match_tag_probe([{tag,{inner,value}}], [tag,inner]),
             match_tag_probe([{tag,value},extra], [tag]),
             match_tag_probe(value, []),
             observe_tag_match({other,value}, [tag]),
             observe_tag_match(bad, [tag])};
        outer(decoded_list) ->
            {decode_list([first,second]), observe_list([]), observe_list([first|bad]),
             keep_list([first,second])};
        outer(decoded_list_checks) ->
            {decode_list_checks([0,3]), decode_list_checks([7,1.5]),
             observe_list_checks([1,4]), observe_list_checks([1|bad])};
        outer(decoded_list_entry) ->
            {decode_list_entry([first,second], tag), observe_list_entry([]), observe_list_entry(bad),
             decode_list_return([first,second|extra], tag), observe_list_return([]),
             observe_list_return([first|bad])};
        outer(two_required_fields) ->
            {decode_required([<<65>>,second|extra], tag), observe_required([]), observe_required([<<65>>|bad])};
        outer(three_required_and_optional) ->
            {decode_three_required([first,second,third,{9,last}|ignored], tag),
             decode_three_required([first,second,third], tag),
             observe_three_required([]), observe_three_required([first|bad]), observe_three_required([first,second])};
        outer(enum_required_and_optional) ->
            {decode_enum_required([0,second,third,{7,fourth},{8,fifth},{9,last}], tag),
             decode_enum_required([1,second,third,{8,fifth},{9,last}|ignored], tag),
             decode_enum_required([7,second,third], tag)};
        outer(ranged_required_and_optional) ->
            {decode_ranged_required([2,second,{9,last}|ignored], tag),
             decode_ranged_required([0,second], tag),
             observe_ranged_required([-1,second]), observe_ranged_required([4,second]),
             observe_ranged_required([]), observe_ranged_required([1|bad])};
        outer(terminal_ranged_required) ->
            {decode_terminal_range([2,second,third|ignored], tag),
             observe_terminal_required([4,second,third]), observe_terminal_required([1,second|bad])};
        outer(arithmetic) -> {arith(3),arith(bad),arith_guard(3),arith_guard(bad)};
        outer(fallback) -> {check_if(bad),check_case(other, ready),check_case(ready, ready)};
        outer(bytes) ->
            {check_bytes(<<7,9,9>>),check_bytes(<<7,9,8>>),check_bytes(<<7,9,9,0>>),check_bytes(<<7:4>>)};
        outer(projection) -> {project(ready),project_unsafe(other)};
        outer(signed_bytes) -> {read_signed(<<255>>, tag),observe_signed(<<7:4>>),observe_signed(bad)};
        outer(fixed_decode) ->
            {read_boolean(<<0>>, tag),read_boolean(<<255>>, tag),
             observe_fixed(<<0,0>>),observe_fixed(<<255,0>>),read_null(<<>>, tag)};
        outer(scalar_encoder) ->
            {flag(true, tag), flag(false, tag), observe_flag(other),
             measured(<<1,2>>,tag), measured([a,b,c],tag), observe_measure(other),
             empty(other, tag), number(-7, tag), observe_number(1.0)};
        outer(projected_choice) ->
            {fixed_choice({second, value}, []), tagged_choice({first, value}, tag),
             observe_choice({other}), observe_choice({first}), pair_result(value)};
        outer(deferred_fields) ->
            {fields({record, left, right, last}, tag), observe_fields({record, left}),
             observe_fields({record, left, invalid, last}),
             observe_pair_binding({record, left, right}, other)};
        outer(two_deferred_fields) ->
            {two_fields({record,left,right}, tag), observe_two_fields({record,left})};
        outer(two_deferred_with_optional) ->
            {two_fields_optional({record,left,right}, tag), two_fields_optional({record,left,missing}, tag)};
        outer(two_optional_or_enum) ->
            {two_optional({record,missing,right}, tag), two_optional({record,left,missing}, tag),
             enum_and_field({record,first,right}, tag), observe_enum_field({record,invalid,right})};
        outer(three_deferred_with_optional) ->
            {three_fields_optional({record,left,middle,right}, tag),
             three_fields_optional({record,left,middle,missing}, tag)};
        outer(deferred_optional_suffix) ->
            {optional_suffix({record,left,middle,third,last}, tag),
             optional_suffix({record,left,middle,missing,last}, tag)};
        outer(optional_fields) ->
            {optional_fields({record,missing,required,false}, tag),
             optional_fields({record,left,required,right}, tag),
             optional_fields({record,missing,required,default}, tag)};
        outer(single_optional) ->
            {single_optional({record,missing}, tag),single_optional({record,value}, tag),observe_single_optional(bad)};
        outer(single_optional_decode) ->
            {single_optional_decode([{7,<<65>>}|other], tag),
             single_optional_decode([{8,<<65>>}], tag), single_optional_decode([], tag)};
        outer(multiple_optional_decode) ->
            {multiple_optional_decode([{7,<<65>>},{8,<<66>>},{9,<<67>>}|other], tag),
             multiple_optional_decode([{8,<<66>>},{9,<<67>>}], tag),
             multiple_optional_decode([{7,<<65>>},{9,<<67>>}], tag),
             multiple_optional_decode([{7,<<65>>},{8,<<66>>}], tag),
             multiple_optional_decode([], tag)};
        outer(mixed_decode) ->
            {mixed_decode([{7,<<65>>},<<66>>,{9,last}], tag),
             mixed_decode([<<66>>], tag), observe_mixed_decode([])};
        outer(required_then_optional) ->
            {required_then_optional([<<65>>,{9,last}|ignored], tag),
             required_then_optional([<<65>>], tag), observe_required_then_optional([])};
        outer(required_then_two_optional) ->
            {required_then_two_optional([<<65>>,{8,middle},{9,last}|ignored], tag),
             required_then_two_optional([<<65>>,{9,last}], tag),
             required_then_two_optional([<<65>>,{8,middle}], tag),
             observe_required_then_two_optional([])};
        outer(closed_mapping) -> {closed_mapping([first,second]),closed_mapping([]),observe_closed_mapping([first|bad])};
        outer(closed_binary_mapping) ->
            {closed_binary_mapping([<<65>>,<<66>>]), closed_binary_mapping([]),
             observe_closed_binary_mapping([<<65>>|bad])};
        outer(sequence_encoding) ->
            {encode_sequence([first,second], [], 0), encode_sequence([], [], 0),
             observe_sequence_encoding([first|bad])};
        outer(sequence_forwarding) ->
            {wrap_sequence([first,second], tag),
             forward_sequence(tag, encode_sequence([first], [], 0)),
             observe_unproven_pair(bad)};
        outer(decoder_forwarding) ->
            {forward_decoded(value, tag), decode_to_list(<<65,66>>, tag)};
        outer(singleton_forwarding) ->
            {unwrap_singleton([[<<65>>]]), unwrap_singleton([first,second]), unwrap_singleton([])};
        outer(list_results) ->
            {lookup_list(), measure_signed(5), measure_signed(-5)};
        outer(constant_return) ->
            {constant_pair(ok), observe_constant_pair(other)};
        outer(case_pair) ->
            {case_pair({present, value}), case_pair(other), tagged_call([one,two])};
        outer(tagged_dispatch) ->
            {tagged_dispatch({2,value}), tagged_dispatch({7,value}), tagged_dispatch({2}), tagged_dispatch(other)};
        outer(singleton_unwrap) ->
            {unwrap([one]), unwrap([one,two]), unwrap([one|two]), unwrap([]), unwrap(other),
             unwrap_choice([{tag,value}], tag), unwrap_choice([one|two], tag),
             unwrap_choice_original([{other,value}], tag),
             unwrap_decoder([{2,<<65>>}], tag), unwrap_decoder({7,value}, tag), unwrap_decoder(other, tag),
             unwrap_decoder({9,0}, tag), unwrap_decoder({9,7}, tag), unwrap_decoder({9,1.5}, tag),
             unwrap_decoder({10,<<7>>}, tag), observe_integer(<<255>>), observe_integer(<<8>>),
             observe_bounded(-1), observe_bounded(8)};
        outer(Value) ->
            try run(Value)
            catch Class:Reason:Stack -> {Class,Reason,is_list(Stack)} end.
        range(Value, Limit) when Value >= 0, Value =< Limit, Value =/= Limit -> in_range;
        range(_, _) -> outside.
        forward(second, Value) -> wrap(Value);
        forward(first, Value) -> identity(Value);
        forward(Key, _) -> {missing,Key}.
        wrap(Value) -> {wrapped,Value}.
        identity(Value) -> Value.
        pair(First, Second, Third) -> {First,Second,Third}.
        arith(Value) ->
            try (Value + 5) bsr 1
            catch Class:Reason:Stack -> {Class,Reason,is_list(Stack)} end.
        arith_guard(Value) when Value + 1 > 2 -> yes;
        arith_guard(_) -> no.
        check_if(Value) ->
            if is_integer(Value) -> number; true -> other end.
        check_bytes(Value) ->
            Binary = identity(Value),
            case Binary of <<7,X,X>> -> {yes,X}; _ -> no end.
        project(Value) -> element(2, pair(Value, result, tail)).
        project_unsafe(Value) ->
            try element(2, maybe_pair(Value))
            catch error:Reason:Stack -> {Reason,is_list(Stack)} end.
        maybe_pair(ready) -> {ready,result};
        maybe_pair(Other) -> Other.
        read_signed(Value, Tag) ->
            Raw = keep_bytes(Value, Tag),
            Len = byte_size(Raw),
            <<Number:Len/signed-unit:8>> = Raw,
            Number.
        keep_bytes(Value, _) -> Value.
        read_boolean(Value, Tag) ->
            Raw = keep_bytes(Value, Tag),
            case Raw of <<0:8>> -> false; <<_:8>> -> true; _ -> exit({invalid,Raw}) end.
        read_null(Value, Tag) ->
            Raw = keep_bytes(Value, Tag),
            case Raw of <<>> -> null; _ -> exit({invalid,Raw}) end.
        flag(true, Tag) -> package(Tag,[255],1);
        flag(false, Tag) -> package(Tag,[0],1);
        flag(Value, _) -> exit({invalid,Value}).
        measured(Value, Tag) when is_binary(Value) -> package(Tag,Value,byte_size(Value));
        measured(Value, Tag) when is_list(Value) -> package(Tag,Value,length(Value)).
        package(Tag, Value, Length) -> {Tag,Value,Length}.
        empty(_Ignored, Tag) -> package(Tag,[],0).
        number(Value, Tag) when is_integer(Value) -> package_number(Tag, wrap(Value));
        number(Value, _) -> exit({invalid,{integer,Value}}).
        package_number(Tag, Value) -> {Tag,Value}.
        constant_pair(Value) -> begin {tag, validate_constant(Value)} end.
        tagged_call(Value) -> begin {tag, identity(Value)} end.
        unwrap(Input) ->
            Checked = identity(Input),
            case Checked of [Item] -> Item; _ -> Checked end.
        unwrap_choice(Input, Tag) ->
            Checked = keep_bytes(Input, Tag),
            case (case Checked of [Item] -> Item; _ -> Checked end) of
                {tag,Value} -> Value;
                Other -> Other
            end.
        unwrap_choice_original(Input, Tag) ->
            Checked = keep_bytes(Input, Tag),
            case (case Checked of [Item] -> Item; _ -> Checked end) of
                {tag,Value} -> Value;
                _ -> Checked
            end.
        unwrap_decoder(Input, Tag) ->
            Checked = keep_bytes(Input, Tag),
            case (case Checked of [Item] -> Item; _ -> Checked end) of
                {2,Value} -> {decoded, begin binary_to_list(keep_bytes(Value, [])) end};
                {7,Value} -> {other, keep_bytes(Value, [])};
                {9,Value} -> {bounded, begin
                    Number = keep_bytes(Value, []),
                    if 0 =< Number, Number =< 7 -> Number;
                       true -> exit(out_of_range)
                    end
                end};
                {10,Value} -> {integer, begin
                    Number = signed_payload(Value, []),
                    if 0 =< Number, Number =< 7 -> Number;
                       true -> exit(out_of_range)
                    end
                end};
                Else -> Else
            end.
        signed_payload(Value, Tag) ->
            Bin = keep_bytes(Value, Tag),
            Size = byte_size(Bin),
            <<Number:Size/signed-unit:8>> = Bin,
            Number.
        observe_bounded(Value) ->
            try unwrap_decoder({9,Value}, tag)
            catch Class:Reason -> {Class,Reason} end.
        observe_integer(Bytes) ->
            try unwrap_decoder({10,Bytes}, tag)
            catch Class:Reason -> {Class,Reason} end.
        tagged_dispatch(Input) ->
            begin case identity(Input) of
                {2,Value} -> {second, package_number(Value, 2)};
                {7,Value} -> {seventh, package_number(Value, 7)};
                Else -> Else
            end end.
        case_pair(Input) ->
            {Value, Rest} = case Input of
                {present, Item} -> {Item, done};
                _ -> {missing, Input}
            end,
            {Rest, Value}.
        validate_constant(Value) ->
            Input = identity(Value),
            case Input of ok -> accepted; _ -> exit({invalid,Value}) end.
        observe_constant_pair(Value) ->
            try constant_pair(Value)
            catch exit:Reason:_ -> Reason end.
        fields(Input, Tag) ->
            {_, Left, Right, Last} = Input,
            {A, N} = package_number(Left, 1),
            {B, M} = case Right of
                right -> package_number(right, 2);
                other -> package_number(other, 3);
                Key -> exit({invalid,Key})
            end,
            {C, K} = package_number(Last, 4),
            Bytes = [A,B,C],
            Length = N+M+K,
            package(Tag, Bytes, Length).
        two_fields(Input, Tag) ->
            {_, Left, Right} = Input,
            {A, N} = package_number(Left, 1),
            {B, M} = package_number(Right, 2),
            Bytes = [A,B],
            Length = N+M,
            package(Tag, Bytes, Length).
        observe_two_fields(Input) ->
            try two_fields(Input, tag)
            catch error:{badmatch,Value}:_ -> Value end.
        two_fields_optional(Input, Tag) ->
            {_, Left, Right} = Input,
            {A, N} = package_number(Left, 1),
            {B, M} = case Right of
                missing -> {<<>>,0};
                _ -> package_number(Right, 2)
            end,
            Bytes = [A,B],
            Length = N+M,
            package(Tag, Bytes, Length).
        three_fields_optional(Input, Tag) ->
            {_, Left, Middle, Right} = Input,
            {A, N} = package_number(Left, 1),
            {B, M} = package_number(Middle, 2),
            {C, K} = case Right of
                missing -> {<<>>,0};
                _ -> package_number(Right, 3)
            end,
            Bytes = [A,B,C],
            Length = N+M+K,
            package(Tag, Bytes, Length).
        two_optional(Input, Tag) ->
            {_, Left, Right} = Input,
            {A, N} = case Left of missing -> {<<>>,0}; _ -> package_number(Left, 1) end,
            {B, M} = case Right of missing -> {<<>>,0}; _ -> package_number(Right, 2) end,
            Bytes = [A,B],
            Length = N+M,
            package(Tag, Bytes, Length).
        enum_and_field(Input, Tag) ->
            {_, Left, Right} = Input,
            {A, N} = case Left of
                first -> package_number(first, 1);
                second -> package_number(second, 2);
                Key -> exit({invalid,Key})
            end,
            {B, M} = package_number(Right, 3),
            Bytes = [A,B],
            Length = N+M,
            package(Tag, Bytes, Length).
        observe_enum_field(Input) ->
            try enum_and_field(Input, tag)
            catch exit:Reason:_ -> Reason end.
        optional_suffix(Input, Tag) ->
            {_, Left, Middle, Third, Last} = Input,
            {A, N} = package_number(Left, 1),
            {B, M} = package_number(Middle, 2),
            {C, K} = case Third of
                missing -> {<<>>,0};
                _ -> package_number(Third, 3)
            end,
            {D, L} = case Last of
                missing -> {<<>>,0};
                _ -> package_number(Last, 4)
            end,
            Bytes = [A,B,C,D],
            Length = N+M+K+L,
            package(Tag, Bytes, Length).
        optional_fields(Input, Tag) ->
            {_, Left, Middle, Right} = Input,
            {A, N} = case Left of missing -> {<<>>,0}; _ -> package_number(Left, 1) end,
            {B, M} = package_number(Middle, 2),
            {C, K} = case Right of
                default -> {<<>>,0};
                _ when Right =:= false -> {<<>>,0};
                _ -> package_number(Right, 3)
            end,
            Bytes = [A,B,C],
            Length = N+M+K,
            package(Tag, Bytes, Length).
        single_optional(Input, Tag) ->
            {_, Field} = Input,
            {Bytes, Size} = case Field of missing -> {<<>>,0}; _ -> package_number(Field, 2) end,
            List = [Bytes],
            Length = Size,
            package(Tag, List, Length).
        observe_single_optional(Input) ->
            try single_optional(Input, tag)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        single_optional_decode(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            {Value,Tail} = case Items of
                [{7,Bytes}|Rest] -> {begin binary_to_list(keep_bytes(Bytes, [])) end,Rest};
                _ -> {missing,Items}
            end,
            case Tail of [] -> true; _ -> true end,
            Result = {record,Value},
            Result.
        multiple_optional_decode(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            {First,Tail1} = case Items of
                [{7,Bytes1}|Rest1] -> {begin binary_to_list(keep_bytes(Bytes1, [])) end,Rest1};
                _ -> {missing,Items}
            end,
            {Second,Tail2} = case Tail1 of
                [{8,Bytes2}|Rest2] -> {begin binary_to_list(keep_bytes(Bytes2, [])) end,Rest2};
                _ -> {missing,Tail1}
            end,
            {Third,Tail3} = case Tail2 of
                [{9,Bytes3}|Rest3] -> {begin binary_to_list(keep_bytes(Bytes3, [])) end,Rest3};
                _ -> {missing,Tail2}
            end,
            case Tail3 of [] -> true; _ -> true end,
            Result = {record,First,Second,Third},
            Result.
        mixed_decode(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            {First,Tail1} = case Items of
                [{7,Bytes1}|Rest1] -> {begin binary_to_list(keep_bytes(Bytes1, [])) end,Rest1};
                _ -> {missing,Items}
            end,
            [Bytes2|Tail2] = Tail1,
            Second = begin binary_to_list(keep_bytes(Bytes2, [])) end,
            {Third,Tail3} = case Tail2 of
                [{9,Value}|Rest2] -> {keep_bytes(Value, []),Rest2};
                _ -> {missing,Tail2}
            end,
            case Tail3 of [] -> true; _ -> true end,
            Result = {record,First,Second,Third},
            Result.
        observe_mixed_decode(Input) ->
            try mixed_decode(Input, tag)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        required_then_optional(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [Bytes|Rest] = Items,
            First = begin binary_to_list(keep_bytes(Bytes, [])) end,
            {Second,Tail} = case Rest of
                [{9,Value}|Remaining] -> {keep_bytes(Value, []),Remaining};
                _ -> {missing,Rest}
            end,
            case Tail of [] -> true; _ -> true end,
            Result = {record,First,Second},
            Result.
        observe_required_then_optional(Input) ->
            try required_then_optional(Input, tag)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        required_then_two_optional(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [Bytes|Rest] = Items,
            First = begin binary_to_list(keep_bytes(Bytes, [])) end,
            {Second,Tail1} = case Rest of
                [{8,Value1}|Remaining1] -> {keep_bytes(Value1, []),Remaining1};
                _ -> {missing,Rest}
            end,
            {Third,Tail2} = case Tail1 of
                [{9,Value2}|Remaining2] -> {keep_bytes(Value2, []),Remaining2};
                _ -> {missing,Tail1}
            end,
            case Tail2 of [] -> true; _ -> true end,
            Result = {record,First,Second,Third},
            Result.
        observe_required_then_two_optional(Input) ->
            try required_then_two_optional(Input, tag)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        closed_mapping(Input) ->
            Items = keep_bytes(Input, tag),
            [package_number(Item, 2) || Item <- Items].
        observe_closed_mapping(Input) ->
            try closed_mapping(Input)
            catch error:Reason:_ -> Reason end.
        closed_binary_mapping(Input) ->
            Items = keep_bytes(Input, tag),
            [begin
                 binary_to_list(keep_bytes(Item, tag))
             end
             || Item <- Items].
        observe_closed_binary_mapping(Input) ->
            try closed_binary_mapping(Input)
            catch error:Reason:_ -> Reason end.
        encode_sequence([], Acc, Count) ->
            {lists:reverse(Acc),Count};
        encode_sequence([Head|Tail], Acc, Count) ->
            {Part,Size} = encode_sequence_item(Head, 2),
            encode_sequence(Tail, [Part|Acc], Count + Size).
        encode_sequence_item(Value, _) -> {Value,2}.
        wrap_sequence(Input, Tag) ->
            {Bytes,Count} = encode_sequence(Input, [], 0),
            finish_sequence(Tag, Bytes, Count).
        forward_sequence(Tag, {Bytes,Count}) ->
            finish_sequence(Tag, Bytes, Count).
        unproven_pair(Tag, {Bytes,Count}) ->
            finish_sequence(Tag, Bytes, Count).
        observe_unproven_pair(Input) ->
            try unproven_pair(tag, Input)
            catch error:Reason:_ -> Reason end.
        finish_sequence(Tag, Bytes, Count) -> {Tag,Bytes,Count}.
        forward_decoded(Input, Tag) ->
            Value = keep_bytes(Input, Tag),
            package_decoded(Value).
        package_decoded(Value) -> {decoded,Value}.
        decode_to_list(Input, Tag) ->
            begin binary_to_list(keep_bytes(Input, Tag)) end.
        unwrap_singleton([Value]) -> unwrap_singleton(Value);
        unwrap_singleton(Value) when is_binary(Value) -> Value;
        unwrap_singleton(Value) -> lists:reverse(Value).
        lookup_list() ->
            case lists:reverse([{tag,[first,second]}]) of
                Items when is_list(Items) ->
                    case lists:keyfind(tag, 1, Items) of
                        {_, Found} when is_list(Found) -> Found;
                        _ -> []
                    end;
                _ -> []
            end.
        measure_signed(Input) ->
            Bytes = if Input >= 0 -> positive_bytes(Input, []);
                       true -> negative_bytes(Input, [])
                    end,
            {Bytes,length(Bytes)}.
        positive_bytes(Input, Tail) -> [Input|Tail].
        negative_bytes(Input, Tail) -> [Input|Tail].
        observe_sequence_encoding(Input) ->
            try encode_sequence(Input, [], 0)
            catch error:Reason:_ -> Reason end.
        observe_fields(Input) ->
            try fields(Input, tag)
            catch error:{badmatch,Value}:_ -> Value;
                  exit:{invalid,Key}:_ -> {invalid,Key}
            end.
        checked_fields(Input, Expected) ->
            {_, Left, Right} = Input,
            {A, N} = package_number(Left, 1),
            {B, M} = package_number(Right, 2),
            A = Expected,
            package(tag, [A,B], N+M).
        observe_pair_binding(Input, Expected) ->
            try checked_fields(Input, Expected)
            catch error:{badmatch,Value}:_ -> Value end.
        pair_result(Input) ->
            {First, Second} = package_number(Input, 3),
            {Second, First}.
        fixed_choice(Input, Tag) ->
            {Bytes, Length} = case element(1, Input) of
                first -> package_number(element(2, Input), 1);
                second -> package_number(element(2, Input), 2);
                Key -> exit({invalid,Key})
            end,
            package(Tag, Bytes, Length).
        tagged_choice(Input, Tag) ->
            {Bytes, Length} = case element(1, Input) of
                first -> package_number(element(2, Input), 1);
                second -> package_number(element(2, Input), 2);
                Key -> exit({invalid,Key})
            end,
            package(Tag, Bytes, Length).
        observe_choice(Input) ->
            try fixed_choice(Input, [])
            catch exit:{invalid,Key} -> {invalid,Key};
                  error:Reason -> {error,Reason}
            end.
        observe_number(Value) ->
            try number(Value,tag)
            catch exit:{invalid,{integer,Payload}}:Stack -> {Payload,is_list(Stack)} end.
        observe_flag(Value) ->
            try flag(Value,tag)
            catch exit:{invalid,Payload}:Stack -> {Payload,is_list(Stack)} end.
        observe_measure(Value) ->
            try measured(Value,tag)
            catch error:Reason:Stack -> {Reason,is_list(Stack)} end.
        observe_fixed(Value) ->
            try read_boolean(Value, tag)
            catch exit:{invalid,Payload}:Stack -> {Payload,is_list(Stack)} end.
        observe_signed(Value) ->
            try read_signed(Value, tag)
            catch error:{badmatch,_}:Stack -> {badmatch,is_list(Stack)};
                  error:Reason:Stack -> {Reason,is_list(Stack)}
            end.
        check_case(Value, Bound) ->
            try case Value of fixed -> fixed; Bound -> bound end
            catch error:Reason:Stack -> {Reason,is_list(Stack)} end.
        observe_of(Value) ->
            try match_of(Value)
            catch Class:Reason:Stack -> {Class,Reason,is_list(Stack)} end.
        match_of(Value) ->
            try identity(Value) of
                ready -> done;
                fail -> exit(after_try)
            catch Class:Reason:Stack -> {inner,Class,Reason,Stack} end.
        choose(Input) ->
            Tagged = identity(Input),
            case Tagged of
                {7,Value} -> {first,Value};
                {2,Value} -> {second,Value};
                _ -> unknown
            end.
        encode_iolist(Value) ->
            try iolist_to_binary(element(1, make_iolist(Value))) of
                Bytes -> {ok,Bytes}
            catch error:Reason:Stack -> {caught,Reason,is_list(Stack)} end.
        make_iolist(Value) -> {Value,tag}.
        length_probe(L) when L =< 127 -> {[L],1};
        length_probe(L) ->
            Octets = probe_octets(L),
            Length = length(Octets),
            if Length =< 126 -> {[128 bor Length|Octets],Length+1};
               true -> exit({error,{asn1,too_long_length_oct,Length}})
            end.
        probe_octets(129) -> [128|bad];
        probe_octets(_) -> [128].
        observe_length(Value) ->
            try length_probe(Value)
            catch error:Reason:_ -> Reason end.
        tagged_length_probe([Tag|Rest], Bytes, Length) ->
            {Header,Count} = length_probe(Length),
            tagged_length_probe(Rest, [Tag,Header|Bytes], Length + byte_size(Tag) + Count);
        tagged_length_probe([], Bytes, Length) -> {Bytes,Length}.
        octet_probe(0, Acc) -> Acc;
        octet_probe(Value, Acc) -> octet_probe(Value bsr 8, [Value band 255|Acc]).
        signed_pos_probe(0, [Head|_Tail] = Bytes) when Head < 128 -> Bytes;
        signed_pos_probe(Value, Acc) -> signed_pos_probe(Value bsr 8, [Value band 255|Acc]).
        signed_neg_probe(-1, [Head|_Tail] = Bytes) when Head > 127 -> Bytes;
        signed_neg_probe(Value, Acc) -> signed_neg_probe(Value bsr 8, [Value band 255|Acc]).
        signed_dispatch_guarded(Value) when is_integer(Value) -> signed_dispatch(Value).
        signed_dispatch(Value) ->
            if Value >= 0 -> signed_pos_probe(Value, []);
               true -> signed_neg_probe(Value, [])
            end.
        collect_probe(Parts) -> collect_probe(Parts, []).
        match_collect_probe(Input, Tag) ->
            Value = keep_bytes(Input, Tag),
            case Value of
                [_|_] = Parts -> collect_probe(Parts);
                Bin when is_binary(Bin) -> Bin
            end.
        observe_match_collect(Input) ->
            try match_collect_probe(Input, tag)
            catch error:Reason:_ -> Reason end.
        collect_probe([{_,L}|Rest], Acc) when is_list(L) ->
            collect_probe(Rest, [collect_probe(L)|Acc]);
        collect_probe([{3,<<Unused,Bits/binary>>}|Rest], _Acc) ->
            collect_probe_bits(Rest, [Bits], Unused);
        collect_probe([{_T,V}|Rest], Acc) ->
            collect_probe(Rest, [V|Acc]);
        collect_probe([], Acc) ->
            list_to_binary(lists:reverse(Acc)).
        collect_probe_bits([{3,<<Unused,Bits/binary>>}|Rest], Acc, Total) ->
            collect_probe_bits(Rest, [Bits|Acc], Unused+Total);
        collect_probe_bits([], Acc, Total) ->
            list_to_binary([Total|lists:reverse(Acc)]).
        match_tag_probe({T,V}, [T]) -> V;
        match_tag_probe({T,V}, [T|Rest]) -> match_tag_probe(V, Rest);
        match_tag_probe([{T,V}], [T|Rest]) -> match_tag_probe(V, Rest);
        match_tag_probe([{T,_V}|_] = List, [T]) -> List;
        match_tag_probe(Tlv, []) -> Tlv;
        match_tag_probe({Tag,_V} = Tlv, [T|_Rest]) ->
            exit({error,{asn1,{wrong_tag,{{expected,T},{got,Tag,Tlv}}}}}).
        observe_tag_match(Tlv, Tags) ->
            try match_tag_probe(Tlv, Tags)
            catch Class:Reason:_ -> {Class,Reason} end.
        encode_wrapped(Mode, Value) ->
            try iolist_to_binary(element(1, make_wrapped(Mode, Value))) of
                Bytes -> {ok,Bytes}
            catch Class:Exception:Stack when Class =:= error; Class =:= exit ->
                case Exception of
                    {error,{asn1,Reason}} -> {error,{asn1,{Reason,Stack}}};
                    Reason -> {error,{asn1,{Reason,Stack}}}
                end
            end.
        make_wrapped(exit, Value) -> exit({error,{asn1,Value}});
        make_wrapped(throw, Value) -> {throw(Value),tag};
        make_wrapped(value, Value) -> {Value,tag}.
        observe_wrapped(Mode, Value) ->
            try encode_wrapped(Mode, Value) of
                {error,{asn1,{Reason,Stack}}} -> {Reason,is_list(Stack)};
                Result -> Result
            catch Class:Reason:Stack -> {Class,Reason,is_list(Stack)} end.
        decode_wrapped(Key, Value) ->
            try
                Result = decode_result(Key, element(1, identity(Value))),
                {ok,Result}
            catch Class:Exception:Stack when Class =:= error; Class =:= exit ->
                case Exception of
                    {error,{asn1,Reason}} -> {error,{asn1,{Reason,Stack}}};
                    Reason -> {error,{asn1,{Reason,Stack}}}
                end
            end.
        decode_result(Key, Payload) -> {Key,Payload}.
        decode_fields(Value, Size) -> begin
            Selected = case identity(Value) of
                0 -> zero;
                1 -> one;
                Other -> {unknown,Other}
            end,
            Limit = begin
                Number = identity(Size),
                if 0 =< Number, Number =< 3 -> Number;
                   true -> exit(out_of_range)
                end
            end,
            {Selected,Limit,Number}
        end.
        observe_range(Value) ->
            try decode_fields(0, Value)
            catch exit:Reason:_ -> Reason end.
        terminal_decoded_range(Input, Tag) ->
            begin
                Value = keep_bytes(Input, Tag),
                if 0 =< Value, Value =< 3 -> Value;
                   true -> exit(out_of_range)
                end
            end.
        observe_terminal_range(Value) ->
            try terminal_decoded_range(Value, tag)
            catch exit:Reason:_ -> Reason end.
        decode_list(Input) ->
            [First|Rest] = Input,
            A = keep_bytes(First, []),
            [Second|Tail] = Rest,
            B = keep_bytes(Second, []),
            {A,B,Tail}.
        keep_list(Input) ->
            [First|Rest] = Input,
            A = keep_bytes(First, []),
            {A,Input,Rest}.
        decode_list_checks(Input) ->
            [First|Rest] = Input,
            Selected = case keep_bytes(First, []) of
                0 -> zero;
                1 -> one;
                Other -> {unknown,Other}
            end,
            [Second|Tail] = Rest,
            Limit = begin
                Number = keep_bytes(Second, []),
                if 0 =< Number, Number =< 3 -> Number;
                   true -> exit(out_of_range)
                end
            end,
            {Selected,Limit,Number,Tail}.
        observe_list_checks(Input) ->
            try decode_list_checks(Input)
            catch exit:Reason:_ -> Reason;
                  error:{badmatch,Value}:_ -> {badmatch,Value} end.
        decode_list_entry(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [First|Rest] = Items,
            Value = keep_bytes(First, []),
            Result = {Value,Rest},
            Result.
        observe_list_entry(Input) ->
            try decode_list_entry(Input, tag)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        decode_list_return(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [First|Rest] = Items,
            Value = keep_bytes(First, []),
            [Second|Tail] = Rest,
            Last = keep_bytes(Second, []),
            case Tail of [] -> true; _ -> true end,
            Result = {returned,Value,Last,Last},
            Result.
        observe_list_return(Input) ->
            try decode_list_return(Input, tag)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        decode_required(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [First|Rest] = Items,
            Value = begin binary_to_list(keep_bytes(First, [])) end,
            [Second|Tail] = Rest,
            Last = keep_bytes(Second, []),
            case Tail of [] -> true; _ -> true end,
            Result = {returned,Value,Last},
            Result.
        observe_required(Input) ->
            try decode_required(Input, tag)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        decode_three_required(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [A|Rest1] = Items,
            First = keep_bytes(A, []),
            [B|Rest2] = Rest1,
            Second = keep_bytes(B, []),
            [C|Rest3] = Rest2,
            Third = keep_bytes(C, []),
            {Last,Tail} = case Rest3 of
                [{9,Value}|Rest4] -> {keep_bytes(Value, []),Rest4};
                _ -> {missing,Rest3}
            end,
            case Tail of [] -> true; _ -> true end,
            Result = {record,First,Second,Third,Last},
            Result.
        observe_three_required(Input) ->
            try decode_three_required(Input, tag)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        decode_enum_required(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [A|Rest1] = Items,
            First = case keep_bytes(A, []) of
                0 -> success;
                1 -> failure;
                Other -> {unknown,Other}
            end,
            [B|Rest2] = Rest1,
            Second = keep_bytes(B, []),
            [C|Rest3] = Rest2,
            Third = keep_bytes(C, []),
            {Fourth,Rest4} = case Rest3 of
                [{7,Value4}|Tail4] -> {keep_bytes(Value4, []),Tail4};
                _ -> {missing,Rest3}
            end,
            {Fifth,Rest5} = case Rest4 of
                [{8,Value5}|Tail5] -> {keep_bytes(Value5, []),Tail5};
                _ -> {missing,Rest4}
            end,
            {Last,Tail} = case Rest5 of
                [{9,Value}|Rest6] -> {keep_bytes(Value, []),Rest6};
                _ -> {missing,Rest5}
            end,
            case Tail of [] -> true; _ -> true end,
            Result = {record,First,Second,Third,Fourth,Fifth,Last},
            Result.
        decode_ranged_required(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [A|Rest1] = Items,
            First = begin
                Number = keep_bytes(A, []),
                if 0 =< Number, Number =< 3 -> Number;
                   true -> exit(out_of_range)
                end
            end,
            [B|Rest2] = Rest1,
            Second = keep_bytes(B, []),
            {Last,Tail} = case Rest2 of
                [{9,Value}|Rest3] -> {keep_bytes(Value, []),Rest3};
                _ -> {missing,Rest2}
            end,
            case Tail of [] -> true; _ -> true end,
            Result = {record,First,Second,Last},
            Result.
        observe_ranged_required(Input) ->
            try decode_ranged_required(Input, tag)
            catch exit:Reason:_ -> Reason;
                  error:{badmatch,Value}:_ -> {badmatch,Value}
            end.
        decode_terminal_range(Input, Tag) ->
            Items = keep_bytes(Input, Tag),
            [A|Rest1] = Items,
            First = begin
                Number = keep_bytes(A, []),
                if 0 =< Number, Number =< 3 -> Number;
                   true -> exit(out_of_range)
                end
            end,
            [B|Rest2] = Rest1,
            Second = keep_bytes(B, []),
            [C|Tail] = Rest2,
            Third = keep_bytes(C, []),
            case Tail of [] -> true; _ -> true end,
            Result = {record,First,Second,Third},
            Result.
        observe_terminal_required(Input) ->
            try decode_terminal_range(Input, tag)
            catch exit:Reason:_ -> Reason;
                  error:{badmatch,Value}:_ -> {badmatch,Value}
            end.
        observe_list(Input) ->
            try decode_list(Input)
            catch error:{badmatch,Value}:_ -> {badmatch,Value} end.
        observe_decode(Key, Value) ->
            try decode_wrapped(Key, Value) of
                {error,{asn1,{Reason,Stack}}} -> {Reason,is_list(Stack)};
                Result -> Result
            catch Class:Reason:Stack -> {Class,Reason,is_list(Stack)} end.
        make(ok) -> {yes};
        make(bad) -> erlang:error(bad);
        make(exit) -> exit(stop);
        make(throw) -> throw(ball).
        """;

    internal static async Task Check()
    {
        BeamModule image = BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(Source, "beam_general_try_probe.erl"),
            new BeamDependencySelection(["lists"])
        );
        await using var runtime = new ProcessRuntime();
        Term expectedTag = Term.Tuple(Term.A("expected"), Term.A("tag"));
        Term actualTag = Term.Tuple(
            Term.A("got"), Term.A("other"), Term.Tuple(Term.A("other"), Term.A("value")));
        Term wrongTag = Term.Tuple(Term.A("wrong_tag"), Term.Tuple(expectedTag, actualTag));
        foreach (var (input, expected) in new[]
        {
            ("ok", Term.Tuple(Term.A("ok"), Term.A("yes"))),
            ("bad", Term.Tuple(Term.A("caught"), Term.A("bad"), Term.A("true"))),
            ("exit", Term.Tuple(Term.A("exit"), Term.A("stop"), Term.A("true"))),
            ("throw", Term.Tuple(Term.A("throw"), Term.A("ball"), Term.A("true"))),
            ("guard", Term.Tuple(Term.A("in_range"), Term.A("outside"), Term.A("in_range"), Term.A("outside"))),
            ("dispatch", Term.Tuple(Term.Tuple(Term.A("wrapped"), Term.A("value")), Term.A("value"),
                Term.Tuple(Term.A("missing"), Term.A("unknown")))),
            ("choice", Term.Tuple(Term.Tuple(Term.A("first"), Term.A("a")), Term.Tuple(Term.A("second"), Term.A("b")),
                Term.A("unknown"), Term.A("unknown"), Term.A("unknown"))),
            ("arguments", Term.Tuple(Term.Tuple(Term.A("first"), Term.A("second"), Term.A("third")), Term.A("second"))),
            ("protected_iolist", Term.Tuple(
                Term.Tuple(Term.A("ok"), new BitString([65, 66])),
                Term.Tuple(Term.A("caught"), Term.A("badarg"), Term.A("true")))),
            ("wrapped_catch", Term.Tuple(
                Term.Tuple(Term.A("ok"), new BitString([65])),
                Term.Tuple(Term.A("badarg"), Term.A("true")),
                Term.Tuple(Term.A("tagged"), Term.A("true")),
                Term.Tuple(Term.A("throw"), Term.A("ball"), Term.A("true")))),
            ("wrapped_decode", Term.Tuple(
                Term.Tuple(Term.A("ok"), Term.Tuple(Term.A("key"), Term.A("payload"))),
                Term.Tuple(Term.A("badarg"), Term.A("true")))),
            ("decoded_fields", Term.Tuple(
                Term.Tuple(Term.A("zero"), Term.I(3), Term.I(3)),
                Term.Tuple(Term.Tuple(Term.A("unknown"), Term.I(7)), new FloatTerm(1.5), new FloatTerm(1.5)),
                Term.A("out_of_range"))),
            ("terminal_decoded_range", Term.Tuple(
                Term.I(0), new FloatTerm(1.5), Term.A("out_of_range"), Term.A("out_of_range"))),
            ("length_encoding", Term.Tuple(
                Term.Tuple(Term.List(Term.I(127)), Term.I(1)),
                Term.Tuple(Term.List(Term.I(129), Term.I(128)), Term.I(2)),
                Term.Tuple(Term.List(new FloatTerm(1.5)), Term.I(1)),
                Term.A("badarg"))),
            ("tagged_length", Term.Tuple(
                Term.Tuple(Term.List(new BitString([4]), Term.List(Term.I(1)), Term.I(65)), Term.I(3)),
                Term.Tuple(Term.List(Term.I(65)), Term.I(1)))),
            ("octet_accumulator", Term.Tuple(
                Term.List(Term.I(1), Term.I(0), Term.I(1)), Term.List(), new Cons(Term.I(1), Term.A("tail")))),
            ("signed_octet_accumulator", Term.Tuple(
                Term.List(Term.I(0), Term.I(128)), Term.List(Term.I(255), Term.I(127)),
                Term.List(new FloatTerm(127.5)))),
            ("match_collect", Term.Tuple(
                new BitString([65]), new BitString([66]), Term.Tuple(Term.A("case_clause"), Term.A("bad")))),
            ("collect_parts", Term.Tuple(
                new BitString([65, 66, 67]),
                new BitString([3, 252, 128]),
                new BitString([65]))),
            ("tag_match", Term.Tuple(
                Term.A("value"), Term.A("value"),
                Term.List(Term.Tuple(Term.A("tag"), Term.A("value")), Term.A("extra")),
                Term.A("value"),
                Term.Tuple(Term.A("exit"), Term.Tuple(Term.A("error"), Term.Tuple(Term.A("asn1"), wrongTag))),
                Term.Tuple(Term.A("error"), Term.A("function_clause")))),
            ("decoded_list", Term.Tuple(
                Term.Tuple(Term.A("first"), Term.A("second"), Term.List()),
                Term.Tuple(Term.A("badmatch"), Term.List()),
                Term.Tuple(Term.A("badmatch"), Term.A("bad")),
                Term.Tuple(Term.A("first"), Term.List(Term.A("first"), Term.A("second")), Term.List(Term.A("second"))))),
            ("decoded_list_checks", Term.Tuple(
                Term.Tuple(Term.A("zero"), Term.I(3), Term.I(3), Term.List()),
                Term.Tuple(Term.Tuple(Term.A("unknown"), Term.I(7)), new FloatTerm(1.5), new FloatTerm(1.5), Term.List()),
                Term.A("out_of_range"),
                Term.Tuple(Term.A("badmatch"), Term.A("bad")))),
            ("decoded_list_entry", Term.Tuple(
                Term.Tuple(Term.A("first"), Term.List(Term.A("second"))),
                Term.Tuple(Term.A("badmatch"), Term.List()),
                Term.Tuple(Term.A("badmatch"), Term.A("bad")),
                Term.Tuple(Term.A("returned"), Term.A("first"), Term.A("second"), Term.A("second")),
                Term.Tuple(Term.A("badmatch"), Term.List()),
                Term.Tuple(Term.A("badmatch"), Term.A("bad")))),
            ("two_required_fields", Term.Tuple(
                Term.Tuple(Term.A("returned"), Term.List(Term.I(65)), Term.A("second")),
                Term.Tuple(Term.A("badmatch"), Term.List()),
                Term.Tuple(Term.A("badmatch"), Term.A("bad")))),
            ("three_required_and_optional", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.A("first"), Term.A("second"), Term.A("third"), Term.A("last")),
                Term.Tuple(Term.A("record"), Term.A("first"), Term.A("second"), Term.A("third"), Term.A("missing")),
                Term.Tuple(Term.A("badmatch"), Term.List()),
                Term.Tuple(Term.A("badmatch"), Term.A("bad")),
                Term.Tuple(Term.A("badmatch"), Term.List()))),
            ("enum_required_and_optional", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.A("success"), Term.A("second"), Term.A("third"),
                    Term.A("fourth"), Term.A("fifth"), Term.A("last")),
                Term.Tuple(Term.A("record"), Term.A("failure"), Term.A("second"), Term.A("third"),
                    Term.A("missing"), Term.A("fifth"), Term.A("last")),
                Term.Tuple(Term.A("record"), Term.Tuple(Term.A("unknown"), Term.I(7)),
                    Term.A("second"), Term.A("third"), Term.A("missing"), Term.A("missing"), Term.A("missing")))),
            ("ranged_required_and_optional", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.I(2), Term.A("second"), Term.A("last")),
                Term.Tuple(Term.A("record"), Term.I(0), Term.A("second"), Term.A("missing")),
                Term.A("out_of_range"), Term.A("out_of_range"),
                Term.Tuple(Term.A("badmatch"), Term.List()),
                Term.Tuple(Term.A("badmatch"), Term.A("bad")))),
            ("terminal_ranged_required", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.I(2), Term.A("second"), Term.A("third")),
                Term.A("out_of_range"), Term.Tuple(Term.A("badmatch"), Term.A("bad")))),
            ("of_case", Term.Tuple(Term.A("done"),
                Term.Tuple(Term.A("error"), Term.Tuple(Term.A("try_clause"), Term.A("other")), Term.A("true")),
                Term.Tuple(Term.A("exit"), Term.A("after_try"), Term.A("true")))),
            ("arithmetic", Term.Tuple(Term.I(4),
                Term.Tuple(Term.A("error"), Term.A("badarith"), Term.A("true")), Term.A("yes"), Term.A("no"))),
            ("fallback", Term.Tuple(Term.A("other"),
                Term.Tuple(Term.Tuple(Term.A("case_clause"), Term.A("other")), Term.A("true")), Term.A("bound"))),
            ("bytes", Term.Tuple(Term.Tuple(Term.A("yes"), Term.I(9)), Term.A("no"), Term.A("no"), Term.A("no"))),
            ("projection", Term.Tuple(Term.A("result"), Term.Tuple(Term.A("badarg"), Term.A("true")))),
            ("signed_bytes", Term.Tuple(Term.I(-1), Term.Tuple(Term.A("badmatch"), Term.A("true")),
                Term.Tuple(Term.A("badarg"), Term.A("true")))),
            ("fixed_decode", Term.Tuple(Term.A("false"), Term.A("true"),
                Term.Tuple(new BitString([0, 0]), Term.A("true")),
                Term.Tuple(new BitString([255, 0]), Term.A("true")), Term.A("null"))),
            ("scalar_encoder", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(Term.I(255)), Term.I(1)),
                Term.Tuple(Term.A("tag"), Term.List(Term.I(0)), Term.I(1)),
                Term.Tuple(Term.A("other"), Term.A("true")),
                Term.Tuple(Term.A("tag"), new BitString([1, 2]), Term.I(2)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("a"), Term.A("b"), Term.A("c")), Term.I(3)),
                Term.Tuple(Term.A("function_clause"), Term.A("true")),
                Term.Tuple(Term.A("tag"), Term.List(), Term.I(0)),
                Term.Tuple(Term.A("tag"), Term.Tuple(Term.A("wrapped"), Term.I(-7))),
                Term.Tuple(new FloatTerm(1.0), Term.A("true")))),
            ("projected_choice", Term.Tuple(
                Term.Tuple(Term.List(), Term.A("value"), Term.I(2)),
                Term.Tuple(Term.A("tag"), Term.A("value"), Term.I(1)),
                Term.Tuple(Term.A("invalid"), Term.A("other")),
                Term.Tuple(Term.A("error"), Term.A("badarg")),
                Term.Tuple(Term.I(3), Term.A("value")))),
            ("deferred_fields", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), Term.A("right"), Term.A("last")), Term.I(7)),
                Term.Tuple(Term.A("record"), Term.A("left")),
                Term.Tuple(Term.A("invalid"), Term.A("invalid")),
                Term.A("other"))),
            ("two_deferred_fields", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), Term.A("right")), Term.I(3)),
                Term.Tuple(Term.A("record"), Term.A("left")))),
            ("two_deferred_with_optional", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), Term.A("right")), Term.I(3)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), new BitString([])), Term.I(1)))),
            ("two_optional_or_enum", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(new BitString([]), Term.A("right")), Term.I(2)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), new BitString([])), Term.I(1)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("first"), Term.A("right")), Term.I(4)),
                Term.Tuple(Term.A("invalid"), Term.A("invalid")))),
            ("three_deferred_with_optional", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), Term.A("middle"), Term.A("right")), Term.I(6)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), Term.A("middle"), new BitString([])), Term.I(3)))),
            ("deferred_optional_suffix", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), Term.A("middle"), Term.A("third"), Term.A("last")), Term.I(10)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), Term.A("middle"), new BitString([]), Term.A("last")), Term.I(7)))),
            ("optional_fields", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(new BitString([]), Term.A("required"), new BitString([])), Term.I(2)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("left"), Term.A("required"), Term.A("right")), Term.I(6)),
                Term.Tuple(Term.A("tag"), Term.List(new BitString([]), Term.A("required"), new BitString([])), Term.I(2)))),
            ("single_optional", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(new BitString([])), Term.I(0)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("value")), Term.I(2)),
                Term.Tuple(Term.A("badmatch"), Term.A("bad")))),
            ("single_optional_decode", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.List(Term.I(65))),
                Term.Tuple(Term.A("record"), Term.A("missing")),
                Term.Tuple(Term.A("record"), Term.A("missing")))),
            ("multiple_optional_decode", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.List(Term.I(66)), Term.List(Term.I(67))),
                Term.Tuple(Term.A("record"), Term.A("missing"), Term.List(Term.I(66)), Term.List(Term.I(67))),
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.A("missing"), Term.List(Term.I(67))),
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.List(Term.I(66)), Term.A("missing")),
                Term.Tuple(Term.A("record"), Term.A("missing"), Term.A("missing"), Term.A("missing")))),
            ("mixed_decode", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.List(Term.I(66)), Term.A("last")),
                Term.Tuple(Term.A("record"), Term.A("missing"), Term.List(Term.I(66)), Term.A("missing")),
                Term.Tuple(Term.A("badmatch"), Term.List()))),
            ("required_then_optional", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.A("last")),
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.A("missing")),
                Term.Tuple(Term.A("badmatch"), Term.List()))),
            ("required_then_two_optional", Term.Tuple(
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.A("middle"), Term.A("last")),
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.A("missing"), Term.A("last")),
                Term.Tuple(Term.A("record"), Term.List(Term.I(65)), Term.A("middle"), Term.A("missing")),
                Term.Tuple(Term.A("badmatch"), Term.List()))),
            ("closed_mapping", Term.Tuple(
                Term.List(Term.Tuple(Term.A("first"), Term.I(2)), Term.Tuple(Term.A("second"), Term.I(2))),
                Term.List(), Term.Tuple(Term.A("bad_generator"), Term.A("bad")))),
            ("closed_binary_mapping", Term.Tuple(
                Term.List(Term.List(Term.I(65)), Term.List(Term.I(66))),
                Term.List(), Term.Tuple(Term.A("bad_generator"), Term.A("bad")))),
            ("sequence_encoding", Term.Tuple(
                Term.Tuple(Term.List(Term.A("first"), Term.A("second")), Term.I(4)),
                Term.Tuple(Term.List(), Term.I(0)), Term.A("function_clause"))),
            ("sequence_forwarding", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.List(Term.A("first"), Term.A("second")), Term.I(4)),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("first")), Term.I(2)),
                Term.A("function_clause"))),
            ("decoder_forwarding", Term.Tuple(
                Term.Tuple(Term.A("decoded"), Term.A("value")), Term.List(Term.I(65), Term.I(66)))),
            ("singleton_forwarding", Term.Tuple(
                new BitString([65]), Term.List(Term.A("second"), Term.A("first")), Term.List())),
            ("list_results", Term.Tuple(
                Term.List(Term.A("first"), Term.A("second")),
                Term.Tuple(Term.List(Term.I(5)), Term.I(1)),
                Term.Tuple(Term.List(Term.I(-5)), Term.I(1)))),
            ("constant_return", Term.Tuple(
                Term.Tuple(Term.A("tag"), Term.A("accepted")),
                Term.Tuple(Term.A("invalid"), Term.A("other")))),
            ("case_pair", Term.Tuple(
                Term.Tuple(Term.A("done"), Term.A("value")),
                Term.Tuple(Term.A("other"), Term.A("missing")),
                Term.Tuple(Term.A("tag"), Term.List(Term.A("one"), Term.A("two"))))),
            ("tagged_dispatch", Term.Tuple(
                Term.Tuple(Term.A("second"), Term.Tuple(Term.A("value"), Term.I(2))),
                Term.Tuple(Term.A("seventh"), Term.Tuple(Term.A("value"), Term.I(7))),
                Term.Tuple(Term.I(2)), Term.A("other"))),
            ("singleton_unwrap", Term.Tuple(
                Term.A("one"), Term.List(Term.A("one"), Term.A("two")),
                new Cons(Term.A("one"), Term.A("two")), Term.List(), Term.A("other"),
                Term.A("value"), new Cons(Term.A("one"), Term.A("two")),
                Term.List(Term.Tuple(Term.A("other"), Term.A("value"))),
                Term.Tuple(Term.A("decoded"), Term.List(Term.I(65))),
                Term.Tuple(Term.A("other"), Term.A("value")), Term.A("other"),
                Term.Tuple(Term.A("bounded"), Term.I(0)), Term.Tuple(Term.A("bounded"), Term.I(7)),
                Term.Tuple(Term.A("bounded"), new FloatTerm(1.5)),
                Term.Tuple(Term.A("integer"), Term.I(7)),
                Term.Tuple(Term.A("exit"), Term.A("out_of_range")),
                Term.Tuple(Term.A("exit"), Term.A("out_of_range")),
                Term.Tuple(Term.A("exit"), Term.A("out_of_range")),
                Term.Tuple(Term.A("exit"), Term.A("out_of_range"))))
        })
        {
            Term? actual = null;
            ProcessHandle process = runtime.Spawn(async context =>
            {
                actual = await image.Invoke(context, "outer", Term.A(input));
                return actual;
            });
            Term exitReason = await process.Completion;
            if (actual is null || !actual.Equals(expected))
            {
                throw new InvalidOperationException($"General BEAM try/catch for {input}: expected {expected}, got {actual}; exit {exitReason}.");
            }
        }
    }
}
