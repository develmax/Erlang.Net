-module(beam_pair_length_bounds).
-export([bounded/2, maybe_empty/2, unproved/2, joined_control/2, empty_tags/1]).

bounded(Input, Tags) ->
    {_, A, B, C} = Input,
    {First, Len1} = positive(A, []),
    {Second, Len2} = positive(B, []),
    {Third, Len3} = case C of
        absent -> {[], 0};
        _ -> positive(C, [])
    end,
    Items = [First, Second, Third],
    Length = Len1 + Len2 + Len3,
    finish(Tags, Items, Length).

maybe_empty(Input, Tags) ->
    {_, A, B} = Input,
    {First, Len1} = maybe_zero(A, []),
    {Second, Len2} = positive(B, []),
    Items = [First, Second],
    Length = Len1 + Len2,
    finish(Tags, Items, Length).

unproved(Input, Tags) ->
    {_, A, B} = Input,
    {First, Len1} = arbitrary(A, []),
    {Second, Len2} = positive(B, []),
    Items = [First, Second],
    Length = Len1 + Len2,
    finish_unproved(Tags, Items, Length).

positive(Value, Tags) ->
    Bits = bits(Value, []),
    finish(Tags, Bits, length(Bits)).

maybe_zero(0, Tags) -> {Tags, 0};
maybe_zero(Value, Tags) -> positive(Value, Tags).

arbitrary(Value, _) -> {[], Value}.

bits(0, [_|_] = Acc) -> Acc;
bits(Value, Acc) -> bits(Value bsr 1, [Value band 1|Acc]).

finish([Tag|Tags], Bytes, Length) ->
    finish(Tags, [Tag|Bytes], Length + byte_size(Tag));
finish([], Bytes, Length) ->
    {Bytes, Length}.

finish_unproved([Tag|Tags], Bytes, Length) ->
    finish_unproved(Tags, [Tag|Bytes], Length + byte_size(Tag));
finish_unproved([], Bytes, Length) ->
    {Bytes, Length}.

joined_control(Input, Tags) ->
    {_, A, B} = Input,
    {First, Len1} = through_shared(A, []),
    {Second, Len2} = through_shared(B, []),
    Items = [First, Second],
    Length = Len1 + Len2,
    finish_shared(Tags, Items, Length).

through_shared(Value, Tags) ->
    Bits = bits(Value, []),
    finish_shared(Tags, Bits, length(Bits)).

empty_tags(Tags) ->
    finish_shared(Tags, [], 0).

finish_shared([Tag|Tags], Bytes, Length) ->
    finish_shared(Tags, [Tag|Bytes], Length + byte_size(Tag));
finish_shared([], Bytes, Length) ->
    {Bytes, Length}.
