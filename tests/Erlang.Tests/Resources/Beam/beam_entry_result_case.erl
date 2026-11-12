-module(beam_entry_result_case).
-export([choose/2, bound/2, preserve/2, survive/2, nested/2, atom_chars/0, atom_result/1, size_branch/2, size_gap/2, measure/2, arithmetic_guard/2]).
-export([split_prefix/2, binary_tuple_tail/2, nested_tuple_unused/1]).
-export([guarded_shape/2]).
-export([tuple_entry/2]).
-export([tuple_alias/2]).
-export([literal_entry/1]).
-export([tuple_tag/2]).
-export([tuple_arities/2]).
-export([saved_tuple_case/2, saved_tuple_call/2]).
-export([tagged_nested_tail/2]).
-export([tagged_nested_case/3]).
-export([tagged_nested_retained/3, tagged_nested_identity/3]).
-export([tagged_nested_computed/3]).
-export([deferred_entry_case/2]).
choose(Input, Default) ->
    Result = pick(Input),
    case Result of
        yes -> Default;
        no -> rejected
    end.
bound(Input, Default) ->
    Result = pick(Input),
    Value = case Result of
        yes -> Default;
        _ -> rejected
    end,
    {Value, Default}.
preserve(Input, Expected) ->
    Result = pick(Input),
    case Result of
        {tag, Value} when Result =:= Expected -> Value;
        _ -> Result
    end.
survive(Input, Expected) ->
    Result = pick(Input),
    case Result of
        {tag, Value} when Result =:= Expected ->
            put(seen, Value),
            {Value, Result};
        _ -> Result
    end.
nested(Input, Default) ->
    case pass(pick(Input)) of
        {tag, Value} -> {Value, Default};
        {fail, Reason} -> error({failed, Reason})
    end.

atom_chars() -> atom_to_list('\x{1f642}').
atom_result(Input) ->
    case pick(Input) of
        yes -> yes
    end.

size_branch(Input, Limit) when Limit =< 64 -> pack(Input, Limit);
size_branch(Value, Size) when 64 < Size -> [pack(Value, Size)].

size_gap(Input, Size) when Size < 0 -> pack(Input, Size);
size_gap(Input, Size) when Size > 64 -> [pack(Input, Size)].

measure(Value, Limit) when Limit =< 64 -> pack(Value, Limit);
measure(Value, Limit) when Limit > 64 ->
    Payload = pass(Value),
    Length = byte_size(Payload),
    {Length, Payload}.

arithmetic_guard(A, B) ->
    if
        A - B > 2 -> large;
        A - B < 0 -> negative;
        true -> other
    end.

split_prefix(Bin, Size) ->
    Parts = if
        Size >= 0 ->
            {First, _Rest} = erlang:split_binary(Bin, Size),
            [First];
        true -> []
    end,
    {Parts, Size}.

guarded_shape(Value, Tag) when is_tuple(Value) andalso is_atom(Tag) -> {Tag, Value};
guarded_shape(Value, Tag) when Value =:= Tag -> [Value];
guarded_shape(_Value, _Tag) -> other.

tuple_entry({choose, Value}, Default) ->
    Result = pick(Value),
    case Result of
        yes -> {yes, Default};
        _ -> {other, Value}
    end;
tuple_entry(Value, Default) -> {Value, Default}.

tuple_alias({item, 7, Value} = Whole, Default) when is_atom(Value) ->
    Result = pick(Value),
    {Result, Whole, Default};
tuple_alias(Whole = {item, 8, Value}, Default) -> {Value, Whole, Default};
tuple_alias(Whole = {item, 7, Value}, Default) -> {Value, Whole, Default};
tuple_alias(Whole, Default) -> {fallback, Whole, Default}.

literal_entry(-1) -> <<255>>;
literal_entry(0) -> <<0>>;
literal_entry(Value) when is_integer(Value) andalso Value > 0 -> pass(Value);
literal_entry(2.0) -> float_value;
literal_entry(Value) when Value < 0 -> negative;
literal_entry(_) -> other.

tuple_tag(Whole = {first, Value, _}, Default) when is_atom(Value) ->
    Result = pick(Value),
    {Result, Whole, Default};
tuple_tag({second, _, Value}, Default) -> {Value, Default};
tuple_tag({first, _, Value}, Default) -> {Value, Default};
tuple_tag(Whole, Default) -> {Whole, Default}.

tuple_arities(probe, Original) -> {probe, Original};
tuple_arities({first, Value}, {context, Key}) when is_atom(Value) ->
    Result = pick(Value),
    {Result, Key};
tuple_arities({first, Value, Extra}, {context, Key}) -> {Value, Extra, Key};
tuple_arities({second, Value, Extra}, {alternate, Key}) -> {Value, Extra, Key};
tuple_arities({first, Value}, Original) -> {Value, Original};
tuple_arities(Whole, Original) -> {Whole, Original}.

saved_tuple_case(Selector, Token) ->
    put(saved_case, Token),
    Choice = case Selector of
        {first, Value} when is_atom(Value) -> {ok, Value};
        {second, Value, Extra} -> {Value, Extra};
        {first, Value} -> {other, Value};
        {third, Value, Extra} -> {Extra, Value}
    end,
    {Choice, Selector}.

saved_tuple_call(Selector, Key) ->
    put(saved_case, Key),
    {case Selector of
        {item, Key, Value} -> pack(Value, Key);
        {item, Other, Value} -> {Other, Value};
        {value, Value} -> Value
    end, Selector}.

tagged_nested_tail(Items, Token) ->
    case lists:keyfind(item, 1, Items) of
        {item, Value} -> pack(binary_to_list(Value), Token);
        Unmatched -> Unmatched
    end.

tagged_nested_case(Items, Saved, Token) ->
    case lists:keyfind(item, 1, Items) of
        {item, Value} ->
            case lists:member(Token, Value) of
                true -> {found, Saved};
                false -> {missing, Saved}
            end;
        Unmatched -> Unmatched
    end.

tagged_nested_retained(Items, Saved, Token) ->
    case lists:keyfind(item, 1, Items) of
        {item, Value} ->
            case lists:member(Token, Value) of
                true -> {found, Value, Saved};
                false -> {missing, Value, Saved}
            end;
        Unmatched -> Unmatched
    end.

tagged_nested_identity(Items, Key, Fallback) ->
    case lists:keyfind(Key, 1, Items) of
        {item, Value} ->
            case pass(Value) of
                {error, Reason} -> {error, Reason};
                Other -> Other
            end;
        _ -> Fallback
    end.

tagged_nested_computed(Items, Saved, Token) ->
    case lists:keyfind(item, 1, Items) of
        {item, Value} ->
            case lists:member({Token, Value}, Saved) of
                true -> {found, Value};
                false -> missing
            end;
        Unmatched -> Unmatched
    end.

deferred_entry_case([{item, Before, After} | Tail], Token) ->
    case pass(Before) of
        {values, Values} ->
            case lists:member(Token, Values) of
                true -> {After, Values};
                false -> Tail
            end;
        _ -> Tail
    end;
deferred_entry_case([], _) -> empty.

pack(Value, Size) -> {Value, Size}.

pass(Value) -> Value.

pick(one) -> yes;
pick(two) -> no;
pick(Other) -> Other.

binary_tuple_tail(<<Byte, _/binary>>, Rows) when Byte =:= 9; Byte =:= 32 ->
    {_, A, B} = lists:last(Rows),
    pack(B, A);
binary_tuple_tail(<<"!">>, Rows) ->
    {A, B} = lists:last(Rows),
    pack(A, B);
binary_tuple_tail(_, _) ->
    skipped.

nested_tuple_unused(Rows) ->
    {{_Unused, Captured}, {Same, Same}} = lists:last(Rows),
    {Captured, Same}.
