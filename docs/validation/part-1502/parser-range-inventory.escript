#!/usr/bin/env escript
-mode(compile).
main([Root,Current,Paths]) ->
    {ok,Data}=file:read_file(Paths),
    Names=[unicode:characters_to_list(P)||P<-binary:split(Data,<<10>>,[global]),P=/=<<>>],
    Counts=[compare(Root,Current,Path)||Path<-Names],
    lists:foreach(fun({P,Matched,All,Other})->io:format("~s ~B/~B ranges matched, ~B different~n",[P,Matched,All,Other]) end,Counts),
    io:format("~B modules, ~B matched ranges, ~B different ranges~n",
      [length(Names),lists:sum([M||{_,M,_,_}<-Counts]),lists:sum([D||{_,_,_,D}<-Counts])]).

compare(Root,Current,Path) ->
    Ref=selectors(filename:join(Root,Path)),
    New=selectors(filename:join(Current,Path)),
    Matched=length([ok||{K,V}<-maps:to_list(New),maps:get(K,Ref,missing)=:=V]),
    {Path,Matched,map_size(New),map_size(New)-Matched}.

selectors(Path) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Path),
    maps:from_list([{{N,A},Operands}||{function,N,A,_,Code}<-Fs, lists:prefix("yeccgoto_",atom_to_list(N)),
       Operands<-[[{R,T}||{select_val,{tr,R,T={t_integer,_}},_,_}<-Code]],Operands=/=[]]).
