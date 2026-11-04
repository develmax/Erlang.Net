-module(string_audit_cases).
-export([write/1, cases/0]).

write(Directory) ->
    Seeds = seeds(),
    Cases = lists:usort(cases()),
    write_chunks(Directory, Cases, Seeds, 1).

write_chunks(_, [], _, _) -> ok;
write_chunks(Directory, Cases, Seeds, Index) ->
    {Batch, Rest} = lists:split(min(4000,length(Cases)), Cases),
    Rows = lists:usort(Seeds ++ Batch),
    Output = filename:join(Directory,"audit-" ++ integer_to_list(Index)),
    ok = filelib:ensure_dir(filename:join(Output,"placeholder")),
    Results = [{F,Args,string_port_cases:run(F,Args)} || {F,Args} <- Rows],
    ok = file:write_file(filename:join(Output,"cases.etf"),term_to_binary(Rows)),
    ok = file:write_file(filename:join(Output,"original.etf"),term_to_binary(Results)),
    ok = file:write_file(filename:join(Output,"exports.etf"),term_to_binary(exports())),
    ok = file:write_file(filename:join(Output,"backing.etf"),term_to_binary(string_port_cases:backing())),
    io:format("audit ~p: ~p complete cases~n",[Index,length(Rows)]),
    write_chunks(Directory, Rest, Seeds, Index+1).

exports() -> lists:sort(string:module_info(exports) -- [{module_info,0},{module_info,1}]).

seeds() ->
    Seeds = lists:sublist(string_port_cases:cases(),71),
    true = exports() =:= lists:sort([{F,length(A)} || {F,A} <- Seeds]),
    Seeds.

cases() ->
    Seeds = seeds(),
    Values = [[],<<>>,invalid,{},#{},#{a=>1},-3,-1,0,1,2,3,5,9,32,97,13,
        0.0,-0.0,1.0,1.5,<<0:1>>,<<0:7>>,<<255>>,<<"a">>,<<"ab">>,
        "a","ab","\r\n",[97|tail],[97|<<"b">>],[[]],[<<>>],
        [invalid],[97,invalid],[[97],<<"b">>],[<<"a">>|<<"b">>],
        true,false,leading,trailing,left,right,both,all,infinity,none,nfc,nfd,nfkc,nfkd,
        fun erlang:abs/1,self(),make_ref(),{bm,make_ref()},{ac,make_ref()}],
    Singles = [{F,set_arg(A,I,V)} || {F,A} <- Seeds,
        I <- lists:seq(1,length(A)), V <- Values],
    PairValues = [[],<<>>,invalid,[97|tail],-1,0,1,1.5,<<"a">>,[97]],
    Pairs = [{F,set_arg(set_arg(A,I,X),J,Y)} || {F,A} <- Seeds,
        I <- lists:seq(1,length(A)),J <- lists:seq(I+1,length(A)),
        X <- PairValues,Y <- PairValues],
    Singles ++ Pairs ++ unicode_cases() ++ boundary_cases().

set_arg(Args, Position, Value) ->
    {Before,[_|After]} = lists:split(Position-1,Args),
    Before ++ [Value|After].

unicode_cases() ->
    Points = [0,10,13,32,65,97,223,255,256,304,329,383,778,931,962,963,
        16#600,16#1100,16#1161,16#11A8,16#1C90,16#200D,16#FE0F,16#1F1E6,
        16#1F1E7,16#1F3FB,16#1F469,16#10FFFF],
    Texts = [[],[13,10],[97,778],[16#600,97],[16#1100,16#1161,16#11A8],
        [16#1F1E6,16#1F1E7],[16#1F469,16#200D,16#1F469],
        [65,16#34F,778],[16#130,778],[931,97,931]] ++
        [[X] || X <- Points] ++ [[X,Y] || X <- Points,Y <- Points],
    Selected = [T || {I,T} <- lists:enumerate(Texts), I =< 40 orelse I rem 11 =:= 0],
    Unary = [{F,[R]} || T <- Selected, R <- forms(T),
        F <- [is_empty,length,to_graphemes,reverse,uppercase,lowercase,titlecase,
            casefold,next_codepoint,next_grapheme,chomp]],
    SearchTexts = lists:sublist(Selected,24),
    Search = [{F,[R,P]} || T <- SearchTexts,R <- forms(T),
        P <- [[],"a",[778],[16#600],[16#1100,16#1161],[16#200D]],
        F <- [prefix,split,find]],
    Slice = [{slice,[R,N,L]} || T <- SearchTexts,R <- forms(T),
        N <- [0,1,2,5],L <- [0,1,2,infinity]],
    Unary ++ Search ++ Slice.

forms([]) -> [[],<<>>,[[],<<>>,[]]];
forms([C|Rest]=Text) ->
    Binary = unicode:characters_to_binary(Text),
    Tail = unicode:characters_to_binary(Rest),
    [Text,Binary,[[C],Tail],[C|Tail]].

boundary_cases() ->
    Huge = 1 bsl 80,
    [{slice,["abc",Huge]}, {slice,[<<"abc">>,1,Huge]},
     {nth_lexeme,["a b",Huge," "]}, {substr,["abc",Huge]},
     {substr,["abc",1,Huge]}, {sub_string,["abc",1,Huge]}] ++
    [{F,[T]} || T <- ["+0tail","-0tail","0000000000000001!",
        "1.0e+1x","1.0e-324x","-0.0x","1.7976931348623157e308x",
        "1.7976931348623159e308x","1.0\0tail",[49,46,48|tail],
        [49,50|tail], [49,<<".0x">>], <<"1.0",0,"x">>],
        F <- [to_integer,to_float,list_to_integer,list_to_float]].
