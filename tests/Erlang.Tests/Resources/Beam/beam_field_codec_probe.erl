-module(beam_field_codec_probe).
-export([encode_fields/2, decode_fields/2]).
encode_fields([], []) -> [];
encode_fields([{_, optional, _} | Fields], [asn1_NOVALUE | Values]) ->
    encode_fields(Fields, Values);
encode_fields([{_, default_false, _} | Fields], [Value | Values])
        when Value =:= asn1_DEFAULT; Value =:= false ->
    encode_fields(Fields, Values);
encode_fields([{_, _, Shape} | Fields], [Value | Values]) ->
    [to_tlv(Shape, Value) | encode_fields(Fields, Values)].

to_tlv(Shape, Value) -> {Shape, Value}.
find_choice([], _) -> false;
find_choice([{Name, _, Shape} | Fields], Tag) ->
    case accepts(Shape, Tag) of
        true -> {Name, Shape};
        false -> find_choice(Fields, Tag)
    end.

accepts({ref, Name}, Tag) -> accepts(schema(Name), Tag);
accepts({choice, _, Fields}, Tag) -> find_choice(Fields, Tag) =/= false;
accepts(Shape, Tag) -> wire_tag(Shape) =:= Tag.

decode_fields([], Children) -> {[], Children};
decode_fields([{_, required, Shape} | Fields], [Child | Children]) ->
    decode_field(Fields, from_tlv(Shape, Child), Children);
decode_fields([{_, Presence, Shape} | Fields], [{Tag, _} = Child | Children] = All) ->
    case accepts(Shape, Tag) of
        true -> decode_field(Fields, from_tlv(Shape, Child), Children);
        false -> decode_field(Fields, absent(Presence), All)
    end;
decode_fields([{_, Presence, _} | Fields], []) ->
    decode_field(Fields, absent(Presence), []).

decode_field(Fields, Value, Children) -> {Fields, Value, Children}.
from_tlv(Shape, Value) -> {Shape, Value}.
schema(Type) -> Type.
wire_tag(Shape) -> Shape.
absent(optional) -> asn1_NOVALUE;
absent(default_false) -> false.
