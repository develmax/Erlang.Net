-module(beam_eldap_try_probe).
-export([encode/2, decode/2, encode_with_tags/3]).
encode(Type, Value) ->
    try {ok, beam_eldap_try_api:encode_ber_tlv(to_tlv(schema(Type), Value))}
    catch Class:Reason:Stack when Class =:= error; Class =:= exit ->
        {error, {asn1, {Reason, Stack}}}
    end.

decode(Type, Bytes) ->
    try
        {Tlv, _Rest} = beam_eldap_try_api:decode_ber_tlv(Bytes),
        {ok, from_tlv(schema(Type), Tlv)}
    catch Class:Reason:Stack when Class =:= error; Class =:= exit ->
        {error, {asn1, {Reason, Stack}}}
    end.

schema(Type) -> Type.
to_tlv(Type, Value) -> {Type, Value}.
from_tlv(Type, Value) -> {Type, Value}.
encode_with_tags(Type, Value, Tags) ->
    Shape = schema(Type),
    Tlv = to_tlv(Shape, Value),
    Bytes = encode_tagged(is_choice(Shape), Tlv, Tags),
    {Bytes, byte_size(Bytes)}.

is_choice(Shape) -> Shape.
encode_tagged(_, Bytes, _) -> beam_eldap_try_api:encode_ber_tlv(Bytes).
