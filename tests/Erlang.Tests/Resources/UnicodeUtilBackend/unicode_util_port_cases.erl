-module(unicode_util_port_cases).
-export([write/1,worker/3,outcome/2,run/2,inspect/1,write_long/1]).

write(Directory) ->
    Rows=[{F,A,run(F,A)} || {F,A} <- lists:usort(cases())],
    ok=file:write_file(filename:join(Directory,"contracts.etf"),term_to_binary(Rows)),
    ok=file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(
        lists:sort(unicode_util:module_info(exports)--[{module_info,0},{module_info,1}]))).

run(F,A) ->
    {Pid,Ref}=spawn_monitor(?MODULE,worker,[F,A,self()]),
    receive {Pid,Result} ->
        receive {'DOWN',Ref,process,Pid,normal}->Result after 10000->error(worker_not_settled) end
    after 10000->exit(Pid,kill),error(worker_timeout) end.

worker(F,A,Parent) -> Parent ! {self(),outcome(F,A)}.
outcome(F,A) ->
    try apply(unicode_util,F,arguments(A)) of V -> {ok,V,inspect(V)}
    catch C:R:S -> {exception,C,R,S} end.
inspect(B) when is_binary(B)->{B,binary:referenced_byte_size(B)};
inspect([H|T])->[inspect(H)|inspect(T)];
inspect(T) when is_tuple(T)->list_to_tuple([inspect(V)||V<-tuple_to_list(T)]);
inspect(T)->T.

cases() ->
    Bad=[bad,{},1.0,-1,1 bsl 80,16#110000,<<1:1>>],
    Points=[0,9,13,65,97,127,128,160,173,197,223,229,256,16#301,16#344,16#600,
        16#915,16#94d,16#1100,16#1161,16#11a8,16#200d,16#d7ff,16#d800,16#dfff,
        16#e000,16#fb03,16#ac01,16#ffff,16#10000,16#1f1e6,16#1f600,16#10ffff],
    Clusters=[[65,778,769],[13,10],[16#600,13],[16#600,65,16#301],
        [16#1100,16#1161,16#11a8,16#301],[16#1f1e6,16#1f1e7,16#1f1e8],
        [16#1f469,16#200d,16#1f4bb],[16#915,16#94d,16#200d,16#915],[16#fb03,16#212b],
        [16#301,16#323,65,16#301],[16#600,16#600,65],[16#1100],[16#1161],[16#11a8],
        [16#ac00],[16#ac01],[16#1f469],[16#1f469,16#200d],[16#915],[16#915,16#94d],[13]],
    BinaryShapes=[{binary_backing,I,S}||I<-lists:seq(0,3),S<-lists:seq(0,3)],
    Malformed=lists:append([[lists:foldr(fun(H,T)->[H|T] end,Tail,C),C++[Tail],
        [unicode:characters_to_binary(C)|Tail],[unicode:characters_to_binary(C),Tail]]
        || C<-Clusters,Tail<-[bad,-1,<<255>>,<<1:1>>,[<<255>>,0]]]),
    Strings=[[],<<>>,bad,{},42,1.0,<<1:1>>,[-1],[-1,65],[bad,65],[65|bad],
        [65,-1,66],[[65],-1],[[],[-1]],[[65|bad],66],[<<255>>,0],
        [<<65>>,[-1,65],65],[<<195>>,<<165>>],[<<195>>|<<165>>]] ++
        [[P]||P<-Points++Bad] ++ [<<B>>||B<-lists:seq(0,255)] ++
        BinaryShapes ++ Malformed ++ lists:append([[C,unicode:characters_to_binary(C),[[],[C],<<>>],
            [unicode:characters_to_binary(C)|<<255>>],C++[bad],C++[<<255>>]]||C<-Clusters]),
    [{F,[D]}||F<-[cp,gc,nfd,nfc,nfkd,nfkc,uppercase,lowercase,titlecase,casefold],D<-Strings] ++
    [{F,[D]}||F<-[lookup,category,get_case,is_wide,is_whitespace,is_other_id_start,
        is_other_id_continue,is_letter_not_pattern_syntax],D<-Points++Bad++Strings] ++
    [{spec_version,[]},{pattern_whitespace,[]}] ++
    [{is_wide,[D]}||D<-[[bad,16#fe0e|bad],[bad,16#fe0f],[65|bad],[16#1f600|bad]]].

write_long(Directory)->
    Data=[lists:duplicate(12000,16#600)++[65,769],
        lists:duplicate(12000,16#1100),[65|lists:duplicate(12000,769)],
        lists:foldl(fun(_,Acc)->[Acc] end,[65,778],lists:seq(1,200)),
        lists:duplicate(12000,65)++[bad]],
    Rows=[{F,[D],run(F,[D])}||F<-[cp,gc,nfd,nfc,nfkd,nfkc,uppercase,lowercase,titlecase,casefold,is_wide],D<-Data],
    file:write_file(filename:join(Directory,"long.etf"),term_to_binary(Rows)).


arguments([{binary_backing,I,S}])->[backing(I,S)];
arguments(A)->A.
backing(I,S)->
    Parent=binary:copy(<<65,195,165,240,159,152,128>>,100),
    B=case I of
        0->Parent;
        1->binary:part(Parent,7,350);
        2->binary:part(Parent,7,35);
        3->BitParent= <<0:3,Parent/binary,0:5>>, <<_:3,Unaligned:350/binary,_/bitstring>>=BitParent,Unaligned
    end,
    case S of 0->B;1->[B];2->[B|<<255>>];3->[B,[]] end.
