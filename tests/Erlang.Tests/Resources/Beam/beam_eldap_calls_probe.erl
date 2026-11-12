-module(beam_eldap_calls_probe).
-export([decode_with_tags/3, decode_field/3, schema/1, decode_tagged/3, decode_fields/2, wire_tag/1]).
decode_with_tags(Type, Tlv, Tags) ->
    Shape = schema(Type),
    decode_tagged(Shape, Tlv, Tags).
decode_field(Fields, Value, Children) ->
    {Values, Rest} = decode_fields(Fields, Children),
    {[Value | Values], Rest}.
schema(Type) -> Type.
decode_tagged(Shape, Tlv, Tags) -> {Shape, Tlv, Tags}.
decode_fields(Fields, Children) -> pair_source:read(Fields, Children).
wire_tag({ref, Name}) -> wire_tag(schema(Name));
wire_tag({sequence, _, Tag, _, _}) -> Tag;
wire_tag({enumerated, _, _}) -> 10;
wire_tag(Shape) -> element(2, Shape).
