-module(beam_ber_lists_probe).
-export([encode_tagged/3,octets/1]).
encode_tagged(false, {_, Content}, []) when is_list(Content) ->
    iolist_to_binary([asn1rt_nif:encode_ber_tlv(Child) || Child <- Content]);
encode_tagged(false, {_, Content}, []) -> Content;
encode_tagged(Choice, Tlv, Tags) ->
    asn1rt_nif:encode_ber_tlv(replace_tags(Choice, Tlv, Tags)).

octets(Value) when is_binary(Value) -> Value;
octets(Children) -> iolist_to_binary([octets(Value) || {4, Value} <- Children]).

replace_tags(Choice, Tlv, Tags) -> {Choice,Tlv,Tags}.
