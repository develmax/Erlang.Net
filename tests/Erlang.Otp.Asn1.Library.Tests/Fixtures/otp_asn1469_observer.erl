-module(otp_asn1469_observer).
-export([worker/1, run/0]).
-file("otp_asn1469_observer.erl", 1).

worker(Parent) -> Parent ! {finished, run()}.
run() ->
    [observe(tuple_syntax_binary, fun() -> tuple({1,2,840}, asn1_NOVALUE, asn1_NOVALUE, {'octet-aligned', <<1,2>>}) end),
     observe(tuple_syntax_list, fun() -> tuple({1,2}, asn1_NOVALUE, "description", {'octet-aligned', [1,2]}) end),
     observe(tuple_presentation, fun() -> tuple(asn1_NOVALUE, 7, asn1_NOVALUE, {'octet-aligned', <<>>}) end),
     observe(tuple_negotiation, fun() -> tuple({1,3}, 9, "desc", {'octet-aligned', <<3>>}) end),
     observe(tuple_none, fun() -> tuple(asn1_NOVALUE, asn1_NOVALUE, asn1_NOVALUE, {'octet-aligned', []}) end),
     observe(tuple_other_encoding, fun() -> tuple({1,2}, 3, asn1_NOVALUE, {'single-ASN1-type', <<7>>}) end),
     observe(tuple_invalid_octets, fun() -> tuple({1,2}, 3, asn1_NOVALUE, {'octet-aligned', 42}) end),
     observe(tuple_bad, fun() -> asn1ct_eval_ext:transform_to_EXTERNAL1994(42) end),
     observe(tuple_wrong_size, fun() -> asn1ct_eval_ext:transform_to_EXTERNAL1994({'EXTERNAL', 1}) end),
     observe(map_syntax_binary, fun() -> map({1,2}, asn1_NOVALUE, asn1_NOVALUE, {'octet-aligned', <<1,2>>}) end),
     observe(map_syntax_list, fun() -> map({1,2}, asn1_NOVALUE, "desc", {'octet-aligned', [1,2]}) end),
     observe(map_presentation, fun() -> map(asn1_NOVALUE, 8, asn1_NOVALUE, {'octet-aligned', <<>>}) end),
     observe(map_negotiation, fun() -> map({2,5}, 4, "desc", {'octet-aligned', <<9>>}) end),
     observe(map_none, fun() -> map(asn1_NOVALUE, asn1_NOVALUE, asn1_NOVALUE, {'octet-aligned', []}) end),
     observe(map_other_encoding, fun() -> map({1,2}, 3, asn1_NOVALUE, {'single-ASN1-type', <<7>>}) end),
     observe(map_invalid_octets, fun() -> map({1,2}, 3, asn1_NOVALUE, {'octet-aligned', 42}) end),
     observe(map_bad, fun() -> asn1ct_eval_ext:transform_to_EXTERNAL1994_maps(42) end),
     observe(map_missing_refs, fun() -> asn1ct_eval_ext:transform_to_EXTERNAL1994_maps(#{}) end),
     observe(map_missing_description, fun() -> asn1ct_eval_ext:transform_to_EXTERNAL1994_maps(#{'direct-reference'=>{1,2}, 'indirect-reference'=>3, encoding=>{'octet-aligned',<<>>}}) end),
     observe(map_extra, fun() -> asn1ct_eval_ext:transform_to_EXTERNAL1994_maps(#{'direct-reference'=>{1,2}, 'indirect-reference'=>asn1_NOVALUE, extra=>keep, omitted=>asn1_NOVALUE, encoding=>{other,42}}) end),
     observe(table_missing, fun() -> asn1ct_table:exists(asn1469_table) end),
     observe(table_lifecycle, fun table_lifecycle/0),
     observe(table_missing_again, fun() -> asn1ct_table:exists(asn1469_table) end),
     observe(table_delete_missing, fun() -> asn1ct_table:delete(asn1469_table) end),
     observe(table_delete_list, fun() -> asn1ct_table:delete([asn1469_table,asn1469_other]) end),
     observe(table_delete_bad, fun() -> asn1ct_table:delete(42) end)].

tuple(D,I,V,E) -> asn1ct_eval_ext:transform_to_EXTERNAL1994({'EXTERNAL',D,I,V,E}).
map(D,I,V,E) -> asn1ct_eval_ext:transform_to_EXTERNAL1994_maps(#{'direct-reference'=>D,'indirect-reference'=>I,'data-value-descriptor'=>V,encoding=>E}).
table_lifecycle() ->
    A=asn1ct_table:new(asn1469_table),
    B=asn1ct_table:exists(asn1469_table),
    C=asn1ct_table:new_reuse(asn1469_table),
    D=asn1ct_table:insert(asn1469_table,{key,42}),
    E=asn1ct_table:lookup(asn1469_table,key),
    F=asn1ct_table:lookup(asn1469_table,missing),
    G=asn1ct_table:size(asn1469_table),
    H=asn1ct_table:to_list(asn1469_table),
    I=asn1ct_table:match(asn1469_table,{key,'$1'}),
    J=asn1ct_table:delete(asn1469_table),
    {A,B,C,D,E,F,G,H,I,J,asn1ct_table:exists(asn1469_table)}.
observe(Name, Fun) ->
    try {Name, {returned, Fun()}}
    catch Class:Reason:Stack -> {Name, {caught, Class, Reason, Stack}}
    end.
