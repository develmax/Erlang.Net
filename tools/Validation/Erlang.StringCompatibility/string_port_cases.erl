-module(string_port_cases).
-export([write/1, cases/0, run/2, worker/3]).

write(Directory) ->
    Cases = cases(),
    Results = [{F,Args,run(F,Args)} || {F,Args} <- Cases],
    ok = file:write_file(filename:join(Directory,"cases.etf"),term_to_binary(Cases)),
    ok = file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Results)),
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(
        lists:sort(string:module_info(exports) -- [{module_info,0},{module_info,1}]))),
    ok.

run(F, Args) ->
    {Pid, Ref} = spawn_monitor(?MODULE, worker, [F, Args, self()]),
    receive {Pid, Result} ->
        receive {'DOWN',Ref,process,Pid,normal} -> Result
        after 10000 -> error(worker_not_settled) end
    after 10000 -> exit(Pid,kill), error(worker_timeout) end.

worker(F, Args, Parent) ->
    Parent ! {self(), outcome(F, Args)}.

outcome(F,Args) ->
    try apply(string,F,Args) of
        Value -> {ok,Value}
    catch Class:Reason:Stack -> {exception,Class,Reason,Stack}
    end.

cases() ->
    Smoke = [
        {is_empty,[[]]}, {length,["hello"]}, {to_graphemes,[[97,778,98]]},
        {reverse,[[97,778,98]]}, {equal,["hello",<<"hello">>]},
        {equal,["HELLO","hello",true]}, {equal,[[229],[97,778],false,nfc]},
        {slice,["hello",1]}, {slice,["hello",1,2]},
        {pad,["x",4]}, {pad,[<<"x">>,4,leading]}, {pad,["x",4,both,46]},
        {trim,[" x "]}, {trim,[<<" x ">>,leading]}, {trim,["..x..",trailing,"."]},
        {chomp,["x\r\n"]}, {take,[" aa!"," a"]}, {take,["aa!","!",true]},
        {take,[<<"x!!">>,"!",false,trailing]},
        {lexemes,[" a b "," "]}, {nth_lexeme,[<<" a b ">>,2," "]},
        {uppercase,[[223,97]]}, {lowercase,[[304,65]]}, {titlecase,[[223,65]]},
        {casefold,[[937,223]]}, {prefix,["hello","he"]},
        {split,["a..b..c",".."]}, {split,[<<"a..b..c">>,"..",all]},
        {replace,["a..b..c","..","!"]}, {replace,[<<"a..b..c">>,"..",<<"!">>,all]},
        {find,["abcabc","bc"]}, {find,[<<"abcabc">>,"bc",trailing]},
        {jaro_similarity,["MARTHA","MARHTA"]},
        {next_codepoint,[[97,778,98]]}, {next_grapheme,[[97,778,98]]},
        {to_float,[<<"1.25e-2tail">>]}, {to_integer,["-123tail"]},
        {len,["abc"]}, {concat,["ab","cd"]}, {chr,["abca",97]}, {rchr,["abca",97]},
        {str,["ababa","ba"]}, {rstr,["ababa","ba"]}, {span,["abbc","ab"]},
        {cspan,["abbc","c"]}, {substr,["abc",2]}, {substr,["abc",2,1]},
        {tokens,["a,,b,",","]}, {chars,[97,3]}, {chars,[97,2,[tail]]},
        {copies,["ab",3]}, {words,[" a b "]}, {words,[".a.b.",46]},
        {strip,[" a "]}, {strip,[" a ",left]}, {strip,[".a.",both,46]},
        {sub_word,[" a b ",2]}, {sub_word,[".a.b.",2,46]},
        {left,["abc",5]}, {left,["abc",2,46]}, {right,["abc",5]}, {right,["abc",2,46]},
        {sub_string,["abcd",2]}, {sub_string,["abcd",2,3]},
        {centre,["abc",6]}, {centre,["abc",2,46]}, {join,[["a","b"],","]},
        {to_upper,[[97,223,255,945]]}, {to_lower,[[65,196,931]]},
        {list_to_float,["1.25rest"]}, {list_to_integer,["123rest"]}
    ],
    Texts = [[],<<>>, "abc",<<"abc">>, " a\r\n ",
        [97,778,98,223,128105,8205,128187],
        unicode:characters_to_binary([97,778,98,223,128105,8205,128187]),
        [[],<<>>,[]], [<<"a">>,[98],<<"c">>], [97,<<"bc">>],
        [[97,778],<<"b">>,223], [<<"12">>,"3x"],
        [<<"a">>|<<"bc">>], <<255>>, [97,<<255>>], invalid, [97|invalid]],
    Unary = [{F,[T]} || T <- Texts, F <- [is_empty,length,to_graphemes,reverse,
        uppercase,lowercase,titlecase,casefold,next_codepoint,next_grapheme,trim,chomp,to_float,to_integer]],
    Slices = [{slice,[T,N,L]} || T <- Texts, N <- [0,1,2,5], L <- [0,1,2,infinity]],
    Patterns = [[],<<>>, "a", "b",[97,778],"bc"," "],
    Searches = [{F,[T,P,D]} || T <- Texts, P <- Patterns, F <- [find,split], D <- [leading,trailing]] ++
        [{prefix,[T,P]} || T <- Texts, P <- Patterns],
    Takes = [{take,[T,S,C,D]} || T <- Texts, S <- [[],"ab"," ",[97,[13,10]]],
        C <- [true,false], D <- [leading,trailing]],
    Trim = [{trim,[T,D,S]} || T <- Texts, S <- [[],"ab"," ",[97,[13,10]]], D <- [leading,trailing,both]],
    Equal = [{equal,[A,B,C,N]} || A <- lists:sublist(Texts,13), B <- lists:sublist(Texts,13),
        C <- [false,true], N <- [none,nfc,nfd,nfkc,nfkd]],
    Numbers = [{F,[T]} || T <- ["", "+", "-", "--1", "123", "+123x", "1.2e", "1.2e+", "1,5x", "1.0e9999", "-0.0x",
        <<"123x">>, [<<"12">>,"3x"], [49,<<"23">>,120], [49|tail], <<255>>],
        F <- [to_float,to_integer,list_to_float,list_to_integer]],
    Smoke ++ Unary ++ Slices ++ Searches ++ Takes ++ Trim ++ Equal ++ Numbers ++ extra_cases(Texts).

extra_cases(Texts) ->
    LegacyTexts = [[],"abba",[97,223,255,256],[a,b,a], [97|tail], <<"abc">>, invalid],
    Unary = [{F,[T]} || T <- LegacyTexts, F <- [len,to_upper,to_lower,words,strip]],
    Characters = [{F,[T,C]} || T <- LegacyTexts, C <- [97,32,invalid], F <- [chr,rchr,words]],
    Patterns = [{F,[T,P]} || T <- LegacyTexts, P <- [[],"a","ab","z",[a],invalid],
        F <- [str,rstr,span,cspan,tokens]],
    Padding = [{F,[T,N,C]} || T <- LegacyTexts, N <- [-1,0,1,3,6], C <- [32,46,invalid],
        F <- [left,right,centre]],
    Slices = [{F,[T,N,L]} || T <- LegacyTexts, N <- [0,1,2,5], L <- [-1,0,1,5], F <- [substr,sub_string]],
    Copies = [{copies,[T,N]} || T <- LegacyTexts, N <- [-1,0,1,3]],
    Strip = [{strip,[T,D,C]} || T <- LegacyTexts, D <- [left,right,both,invalid], C <- [97,32,invalid]],
    Words = [{sub_word,[T,N,C]} || T <- LegacyTexts, N <- [-1,0,1,2,5], C <- [97,32,invalid]],
    Chars = [{chars,[C,N,tail]} || C <- [97,invalid], N <- [-1,0,1,3]],
    Modern = [{F,[T,S]} || T <- Texts, S <- [[],"a"," ",[97,[13,10]]], F <- [lexemes]] ++
        [{nth_lexeme,[T,N,S]} || T <- Texts, S <- [[],"a"," ",[97,[13,10]]], N <- [1,2,5]] ++
        [{pad,[T,N,D,46]} || T <- Texts, N <- [-1,0,1,6], D <- [leading,trailing,both]],
    Replacements = [{replace,[T,P,<<"!">>,D]} || T <- Texts, P <- [[],"a","b","bc"], D <- [leading,trailing,all]] ++
        [{split,[T,P,all]} || T <- Texts, P <- [[],"a","b","bc"]],
    Unary ++ Characters ++ Patterns ++ Padding ++ Slices ++ Copies ++ Strip ++ Words ++ Chars ++ Modern ++ Replacements.
