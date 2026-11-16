// BER helpers adapted from OTP-29.1.1 lib/asn1/src/asn1rtt_ber.erl.
// Copyright Ericsson AB 2012-2026, Apache-2.0; see licenses/OTP-LICENSE.txt.
internal static class Asn1BerErlangRuntime
{
    internal const string PublicApi = """

encoding_rule() -> ber.

maps() -> false.

bit_string_format() -> bitstring.

legacy_erlang_types() -> true.

encode(Type, Data) ->
try iolist_to_binary(element(1, encode_disp(Type, Data))) of
  Bytes ->
    {ok,Bytes}
  catch
    Class:Exception:Stk when Class =:= error; Class =:= exit ->
      case Exception of
        {error,{asn1,Reason}} ->
          {error,{asn1,{Reason,Stk}}};
        Reason ->
         {error,{asn1,{Reason,Stk}}}
      end
end.


decode(Type, Data) ->
try
   Result = decode_disp(Type, element(1, ber_decode_nif(Data))),
   {ok,Result}
  catch
    Class:Exception:Stk when Class =:= error; Class =:= exit ->
      case Exception of
        {error,{asn1,Reason}} ->
          {error,{asn1,{Reason,Stk}}};
        Reason ->
         {error,{asn1,{Reason,Stk}}}
      end
end.

""";

    internal const string Info = """
info() ->
   case ?MODULE:module_info(attributes) of
     Attributes when is_list(Attributes) ->
       case lists:keyfind(asn1_info, 1, Attributes) of
         {_,Info} when is_list(Info) ->
           Info;
         _ ->
           []
       end;
     _ ->
       []
   end.
""";

    internal const string Source = """

ber_decode_nif(B) ->
    asn1rt_nif:decode_ber_tlv(B).

ber_encode([Tlv]) ->
    ber_encode(Tlv);
ber_encode(Tlv) when is_binary(Tlv) ->
    Tlv;
ber_encode(Tlv) ->
    asn1rt_nif:encode_ber_tlv(Tlv).

collect_parts(TlvList) ->
    collect_parts(TlvList, []).

collect_parts([{_, L} | Rest], Acc) when is_list(L) ->
    collect_parts(Rest, [collect_parts(L) | Acc]);
collect_parts([{3, <<Unused,Bits/binary>>} | Rest], _Acc) ->
    collect_parts_bit(Rest, [Bits], Unused);
collect_parts([{_T, V} | Rest], Acc) ->
    collect_parts(Rest, [V | Acc]);
collect_parts([], Acc) ->
    list_to_binary(lists:reverse(Acc)).

collect_parts_bit([{3, <<Unused,Bits/binary>>} | Rest], Acc, Uacc) ->
    collect_parts_bit(Rest, [Bits | Acc], Unused + Uacc);
collect_parts_bit([], Acc, Uacc) ->
    list_to_binary([Uacc | lists:reverse(Acc)]).

decode_boolean(Tlv, TagIn) ->
    Val = match_tags(Tlv, TagIn),
    case Val of
        <<0:8>> ->
            false;
        <<_:8>> ->
            true;
        _ ->
            exit({error, {asn1, {decode_boolean, Val}}})
    end.

decode_integer(Tlv, TagIn) ->
    Bin = match_tags(Tlv, TagIn),
    Len = byte_size(Bin),
    <<Int:Len/signed-unit:8>> = Bin,
    Int.

decode_null(Tlv, Tags) ->
    Val = match_tags(Tlv, Tags),
    case Val of
        <<>> ->
            'NULL';
        _ ->
            exit({error, {asn1, {decode_null, Val}}})
    end.

decode_restricted_string(Tlv, TagsIn) ->
    match_and_collect(Tlv, TagsIn).

encode_boolean(true, TagIn) ->
    encode_tags(TagIn, [255], 1);
encode_boolean(false, TagIn) ->
    encode_tags(TagIn, [0], 1);
encode_boolean(X, _) ->
    exit({error, {asn1, {encode_boolean, X}}}).

encode_integer(Val) ->
    Bytes =
        if
            Val >= 0 ->
                encode_integer_pos(Val, []);
            true ->
                encode_integer_neg(Val, [])
        end,
    {Bytes, length(Bytes)}.

encode_integer(Val, Tag) when is_integer(Val) ->
    encode_tags(Tag, encode_integer(Val));
encode_integer(Val, _Tag) ->
    exit({error, {asn1, {encode_integer, Val}}}).

encode_integer_neg(-1, [B1 | _T] = L) when B1 > 127 ->
    L;
encode_integer_neg(N, Acc) ->
    encode_integer_neg(N bsr 8, [N band 255 | Acc]).

encode_integer_pos(0, [B | _Acc] = L) when B < 128 ->
    L;
encode_integer_pos(N, Acc) ->
    encode_integer_pos(N bsr 8, [N band 255 | Acc]).

encode_length(L) when L =< 127 ->
    {[L], 1};
encode_length(L) ->
    Oct = minimum_octets(L),
    Len = length(Oct),
    if
        Len =< 126 ->
            {[128 bor Len | Oct], Len + 1};
        true ->
            exit({error, {asn1, too_long_length_oct, Len}})
    end.

encode_null(_Val, TagIn) ->
    encode_tags(TagIn, [], 0).

encode_restricted_string(OctetList, TagIn) when is_binary(OctetList) ->
    encode_tags(TagIn, OctetList, byte_size(OctetList));
encode_restricted_string(OctetList, TagIn) when is_list(OctetList) ->
    encode_tags(TagIn, OctetList, length(OctetList)).

encode_tags(TagIn, {BytesSoFar, LenSoFar}) ->
    encode_tags(TagIn, BytesSoFar, LenSoFar).

encode_tags([Tag | Trest], BytesSoFar, LenSoFar) ->
    {Bytes2, L2} = encode_length(LenSoFar),
    encode_tags(Trest,
                [Tag, Bytes2 | BytesSoFar],
                LenSoFar + byte_size(Tag) + L2);
encode_tags([], BytesSoFar, LenSoFar) ->
    {BytesSoFar, LenSoFar}.

match_and_collect(Tlv, TagsIn) ->
    Val = match_tags(Tlv, TagsIn),
    case Val of
        [_ | _] = PartList ->
            collect_parts(PartList);
        Bin when is_binary(Bin) ->
            Bin
    end.

match_tags({T, V}, [T]) ->
    V;
match_tags({T, V}, [T | Tt]) ->
    match_tags(V, Tt);
match_tags([{T, V}], [T | Tt]) ->
    match_tags(V, Tt);
match_tags([{T, _V} | _] = Vlist, [T]) ->
    Vlist;
match_tags(Tlv, []) ->
    Tlv;
match_tags({Tag, _V} = Tlv, [T | _Tt]) ->
    exit({error, {asn1, {wrong_tag, {{expected, T}, {got, Tag, Tlv}}}}}).

minimum_octets(0, Acc) ->
    Acc;
minimum_octets(Val, Acc) ->
    minimum_octets(Val bsr 8, [Val band 255 | Acc]).

minimum_octets(Val) ->
    minimum_octets(Val, []).

""";
}
