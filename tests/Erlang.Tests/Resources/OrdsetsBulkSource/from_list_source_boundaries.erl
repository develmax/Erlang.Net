-module(from_list_source_boundaries).
-export([run/0]).
run() ->
    A=fake(0),B=fake(1),L=#{A=>a,B=>b},
    Specs=[{empty,[]},{small_forward,[{A,a},{B,b}]},
        {small_reverse,[{B,b},{A,a}]},{small_A_last,[{A,a},{B,b},{A,c}]},
        {small_32,[{I,I}||I<-lists:seq(1,30)]++[{A,a},{B,b}]},
        {bulk_33,[{I,I}||I<-lists:seq(1,31)]++[{A,a},{B,b}]},
        {bulk_exact_33,lists:duplicate(33,{A,c})},
        {small_repeat_32,lists:flatten(lists:duplicate(16,[{A,a},{B,b}]))},
        {bulk_repeat_33,lists:flatten(lists:duplicate(16,[{A,a},{B,b}]))++[{A,c}]},
        {integers_last_wins,[{1,a},{2,b},{1,c}]},
        {invalid_tuple,[bad]},{improper,[{A,a}|tail]}],
    term_to_binary([{Tag,Pairs,observe(Tag,Pairs,A,B,L)}||{Tag,Pairs}<-Specs],[deterministic]).
observe(Tag,Pairs,A,B,L) ->
    try maps:from_list(Pairs) of M ->
        Ordsets=case Tag of
            bulk_33 -> not_probed;
            bulk_exact_33 -> not_probed;
            bulk_repeat_33 -> not_probed;
            _ -> {ordsets:is_set([M,L]),ordsets:is_equal([M],[L]),
                ordsets:is_element(M,[L]),length(ordsets:from_list([M,L]))}
        end,
        {ok,map_size(M),maps:find(A,M),maps:find(B,M),Ordsets}
    catch C:R:S -> {exception,C,R,S} end.
fake(K) ->
    Unique= <<K:128>>,
    Payload=iolist_to_binary([<<0,Unique/binary,0:32,0:32>>,
        encoded(from_list_source_boundaries_unloaded),encoded(0),encoded(1),
        encoded(list_to_pid("<0.0.0>"))]),
    binary_to_term(<<131,112,(byte_size(Payload)+4):32,Payload/binary>>).
encoded(T) -> <<131,B/binary>>=term_to_binary(T),B.
