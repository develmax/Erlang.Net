-module(string_audit_edges).
-export([write/1, cases/0]).

write(Directory) ->
    ok = filelib:ensure_dir(filename:join(Directory,"placeholder")),
    Seeds = lists:sublist(string_port_cases:cases(),71),
    Rows = lists:usort(Seeds ++ cases()),
    Results = [{F,A,string_port_cases:run(F,A)} || {F,A} <- Rows],
    ok = file:write_file(filename:join(Directory,"cases.etf"),term_to_binary(Rows)),
    ok = file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Results)),
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(
        lists:sort(string:module_info(exports) -- [{module_info,0},{module_info,1}]))),
    ok = file:write_file(filename:join(Directory,"backing.etf"),term_to_binary(string_port_cases:backing())),
    io:format("~p edge cases~n",[length(Rows)]),
    ok.

cases() ->
    Inputs = [[],<<>>,invalid,"abc",<<"abc">>,[<<"abc">>,"d"],
        [<<"abc">>|<<"d">>],[<<0:1>>],[97|tail],[<<97,255>>,"d"],[<<97,255>>],[<<"a">>,[invalid]]],
    Patterns = [[[]],[<<>>],<<255>>,<<16#E2,16#82>>, "a", "b"],
    Search = [{F,[T,P]} || T <- Inputs,P <- Patterns,F <- [prefix,find,split]] ++
        [{find,[T,P,D]} || T <- Inputs,P <- Patterns,D <- [leading,trailing]] ++
        [{split,[T,P,D]} || T <- Inputs,P <- Patterns,D <- [leading,trailing,all]],
    Take = [{take,[T,S,C,D]} || T <- [<<>>,<<"a">>,<<"ab">>,<<"aa">>,
        <<97,255>>,<<13,10,255>>,[<<"a">>,"b"],[<<"a">>|<<"b">>],[<<"a">>,"!",invalid],[<<"a">>,98|<<255>>],[<<"a">>,"!"|<<255>>]],
        S <- [invalid,[97|tail],[97,97],[97,98,97],[[13,10],[13,10]]],
        D <- [leading,trailing], C <- [false,true]],
    Trim = [{trim,[T,D,S]} || T <- [<<"aa">>,<<97,255>>,[<<"a">>,"b"]],
        S <- [[97,97],[97|tail]],D <- [leading,trailing,both]],
    Search ++ Take ++ Trim ++
        [{lexemes,[[],S]} || S <- [invalid,1,1.5,<<>>,<<1:1>>,#{},fun erlang:abs/1]] ++
        malformed_cases().

malformed_cases() ->
    InvalidLists = [[-1],[16#D800],[16#110000],[97,-1],[97,16#D800],
        [-1,97],[97,1.0],[97,{}],[97,#{a=>1}],[97,fun erlang:abs/1],
        [<<0:1>>],[97,<<0:1>>],[<<16#E2>>,<<16#82,16#AC>>]],
    Bytes = [<<B>> || B <- lists:seq(0,255)],
    Broken = [<<16#C0,16#80>>,<<16#C1,16#BF>>,<<16#ED,16#A0,16#80>>,
        <<16#F4,16#90,16#80,16#80>>,<<16#F5,16#80,16#80,16#80>>,
        <<16#E2,16#82>>,<<16#F0,16#9F,16#92>>,<<97,255>>],
    [{F,[T]} || T <- InvalidLists ++ Bytes ++ Broken,
        F <- [is_empty,length,to_graphemes,reverse,uppercase,lowercase,titlecase,
            casefold,next_codepoint,next_grapheme,to_float,to_integer]] ++
        [{F,["abc",P]} || P <- InvalidLists ++ Bytes ++ Broken,
            F <- [prefix,find,split]] ++
        [{F,[T]} || T <- [lists:duplicate(4096,$9),
            unicode:characters_to_binary(lists:duplicate(4096,$9))],
            F <- [to_integer,list_to_integer]].
