-module(beam_tag_decoding_probe).
-export([decode_tagged/3]).
decode_tagged(Shape, Content, []) ->
    case is_choice(Shape) of
        true -> from_tlv(Shape, Content);
        false -> from_tlv(Shape, {wire_tag(Shape), Content})
    end;
decode_tagged(Shape, {Tag, Content}, [Tag]) ->
    case is_choice(Shape) of
        true -> [Child] = Content, from_tlv(Shape, Child);
        false -> from_tlv(Shape, {wire_tag(Shape), Content})
    end;
decode_tagged(Shape, {Tag, [Child]}, [Tag | Rest]) -> decode_tagged(Shape, Child, Rest).

is_choice({ref, Name}) -> is_choice(schema(Name));
is_choice({choice, _, _}) -> true;
is_choice(_) -> false.

wire_tag({ref, Name}) -> wire_tag(schema(Name));
wire_tag({sequence, _, Tag, _, _}) -> Tag;
wire_tag({enumerated, _, _}) -> 10;
wire_tag(Shape) -> element(2, Shape).

from_tlv(Shape, Payload) -> {Shape, Payload}.
schema(Type) -> Type.
