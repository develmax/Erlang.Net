-module(beam_chunk_subject418).
-export([cases/0, worker/2, expected/1]).

cases() -> [raw_target, empty_form, unknown_id, empty_target, duplicate_last, duplicate_empty_last, duplicate_empty_first, embedded_zero_id, arbitrary_padding, unknown_before_after, outer_trailing, bad_suffix_after_match, short_chunk_header, truncated_payload, truncated_padding, negative_chunk, overflow_chunk, form_not_beam, short_form_type, wrong_form_id, short_header, negative_form, declared_too_large, zero_form, empty_binary, nonbinary_atom, nonbinary_list, nonbyte_bitstring, id_atom, id_binary, id_empty, id_short, id_long, id_improper, id_negative, id_large, id_float, id_bignum, both_bad, unaligned_input, opaque_code, gzip_input, id_high_first, id_all_high, id_high_positive].

worker(Parent, Case) ->
    Outcome = try
        {Binary, Id, _Expected} = spec(Case),
        {returned, erts_internal:beamfile_chunk(Binary, Id)}
    catch Class:Reason:Stack -> {raised,Class,Reason,Stack}
    end,
    Parent ! {Case, Outcome}.

expected(Case) -> {_,_,Expected} = spec(Case), Expected.

spec(raw_target) -> {form([chunk(<<"DATA">>,<<1,2,3>>)]), "DATA", {value,<<1,2,3>>}};
spec(empty_form) -> {form([]), "DATA", {value,undefined}};
spec(unknown_id) -> {form([chunk(<<"DATA">>,<<1>>)]), "NONE", {value,undefined}};
spec(empty_target) -> {form([chunk(<<"DATA">>,<<>>)]), "DATA", {value,undefined}};
spec(duplicate_last) -> {form([chunk(<<"DATA">>,<<1>>),chunk(<<"DATA">>,<<2,3>>)]), "DATA", {value,<<2,3>>}};
spec(duplicate_empty_last) -> {form([chunk(<<"DATA">>,<<1>>),chunk(<<"DATA">>,<<>>)]), "DATA", {value,undefined}};
spec(duplicate_empty_first) -> {form([chunk(<<"DATA">>,<<>>),chunk(<<"DATA">>,<<7>>)]), "DATA", {value,<<7>>}};
spec(embedded_zero_id) -> {form([chunk(<<0,255,65,66>>,<<9,8>>)]), [0,255,65,66], {value,<<9,8>>}};
spec(arbitrary_padding) -> {form([<<"DATA",1:32,42,255,128,1>>]), "DATA", {value,<<42>>}};
spec(unknown_before_after) -> {form([chunk(<<"ZZZZ">>,<<1>>),chunk(<<"DATA">>,<<4>>),chunk(<<"YYYY">>,<<2>>)]), "DATA", {value,<<4>>}};
spec(outer_trailing) -> {<<(form([chunk(<<"DATA">>,<<5>>)]))/binary,255,1,2>>, "DATA", {value,<<5>>}};
spec(bad_suffix_after_match) -> {form([chunk(<<"DATA">>,<<5>>),<<1>>]), "DATA", {value,undefined}};
spec(short_chunk_header) -> {form([<<"DATA",0,0,0>>]), "DATA", {value,undefined}};
spec(truncated_payload) -> {form([<<"DATA",9:32,1,2,3,4>>]), "DATA", {value,undefined}};
spec(truncated_padding) -> {form([<<"DATA",1:32,1>>]), "DATA", {value,undefined}};
spec(negative_chunk) -> {form([<<"DATA",-1:32>>]), "DATA", {value,undefined}};
spec(overflow_chunk) -> {form([<<"DATA",2147483647:32>>]), "DATA", {value,undefined}};
spec(form_not_beam) -> {<<"FOR1",4:32,"NOPE">>, "DATA", {value,undefined}};
spec(short_form_type) -> {<<"FOR1",3:32,"BEA">>, "DATA", {value,undefined}};
spec(wrong_form_id) -> {<<"FORM",4:32,"BEAM">>, "DATA", {value,undefined}};
spec(short_header) -> {<<"FOR1",0,0,0>>, "DATA", {value,undefined}};
spec(negative_form) -> {<<"FOR1",-1:32,"BEAM">>, "DATA", {value,undefined}};
spec(declared_too_large) -> {<<"FOR1",100:32,"BEAM">>, "DATA", {value,undefined}};
spec(zero_form) -> {<<"FOR1",0:32>>, "DATA", {value,undefined}};
spec(empty_binary) -> {<<>>, "DATA", {value,undefined}};
spec(nonbinary_atom) -> {not_binary, "DATA", badarg};
spec(nonbinary_list) -> {[], "DATA", badarg};
spec(nonbyte_bitstring) -> {<<1:1>>, "DATA", badarg};
spec(id_atom) -> {form([]), data, badarg};
spec(id_binary) -> {form([]), <<"DATA">>, badarg};
spec(id_empty) -> {form([]), [], badarg};
spec(id_short) -> {form([]), "DAT", badarg};
spec(id_long) -> {form([]), "DATAx", badarg};
spec(id_improper) -> {form([]), [68,65,84,65|tail], badarg};
spec(id_negative) -> {form([]), [-1,65,84,65], badarg};
spec(id_large) -> {form([]), [256,65,84,65], badarg};
spec(id_float) -> {form([]), [68.0,65,84,65], badarg};
spec(id_bignum) -> {form([]), [1 bsl 100,65,84,65], badarg};
spec(both_bad) -> {not_binary, id, badarg};
spec(unaligned_input) -> {unaligned(form([chunk(<<"DATA">>,<<1,2,3,4,5>>)])), "DATA", {value,<<1,2,3,4,5>>}};
spec(opaque_code) -> {form([chunk(<<"Code">>,<<255,255,1,2>>)]), "Code", {value,<<255,255,1,2>>}};
spec(gzip_input) -> {<<31,139,8,0,0,0,0,0,2,10,115,243,15,50,100,96,96,96,113,114,117,244,5,0,134,129,83,54,12,0,0,0>>, "DATA", {value,undefined}};
spec(id_high_first) -> {form([chunk(<<128,0,0,1>>,<<7>>)]), [128,0,0,1], high_id_expected(<<7>>)};
spec(id_all_high) -> {form([chunk(<<255,255,255,255>>,<<8>>)]), [255,255,255,255], high_id_expected(<<8>>)};
spec(id_high_positive) -> {form([chunk(<<127,255,255,255>>,<<9>>)]), [127,255,255,255], {value,<<9>>}}.

form(Chunks) ->
    Body = iolist_to_binary([<<"BEAM">>|Chunks]),
    <<"FOR1",(byte_size(Body)):32,Body/binary>>.

chunk(Id, Payload) ->
    Size = byte_size(Payload), Pad = (4 - (Size band 3)) band 3,
    <<Id/binary,Size:32,Payload/binary,0:(Pad*8)>>.

unaligned(Binary) ->
    Size = byte_size(Binary), Container = <<1:1,Binary/binary,0:7>>,
    <<_:1,Result:Size/binary,_:7>> = Container,
    Result.

high_id_expected(Value) ->
    case erlang:system_info(wordsize) of 8 -> {value,undefined}; 4 -> {value,Value} end.
