-module(beam_numeric_pairs).
-export([numeric/2, mixed/2]).

numeric(Value, Tag) ->
    {_, First, Second} = Value,
    {Bytes1, Length1} = numeric_pair(First, [<<1>>]),
    {Bytes2, Length2} = numeric_pair(Second, [<<2>>]),
    Bytes = [Bytes1, Bytes2],
    Length = Length1 + Length2,
    finish(Tag, Bytes, Length).

mixed(Value, Tag) ->
    {_, First, Second} = Value,
    {Bytes1, Length1} = raw_pair(First, [<<1>>]),
    {Bytes2, Length2} = numeric_pair(Second, [<<2>>]),
    Bytes = [Bytes1, Bytes2],
    Length = Length1 + Length2,
    finish(Tag, Bytes, Length).

numeric_pair(Value, Tag) -> {Tag, Value + 0}.
raw_pair(Value, Tag) -> {Tag, Value}.
finish(Tag, Bytes, Length) -> {Tag, Bytes, Length}.
