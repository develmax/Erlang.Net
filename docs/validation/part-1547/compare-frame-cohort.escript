#!/usr/bin/env escript
-mode(compile).
main([Root,Gen]) ->
  Fs=filelib:fold_files(Gen,"\\.beam$",true,fun(P,A)->[P|A] end,[]),
  Base=length(filename:split(Gen)),
  Cases=lists:append([begin
    Rel=filename:join(lists:nthtail(Base,filename:split(P))),
    Ref=read(filename:join(Root,Rel)),
    [{D,Rel,K,O,N} || {K,N}<-maps:to_list(read(P)), O<-[maps:get(K,Ref,[])], D<-[diff(O,N)], D=/=false]
  end || P<-Fs]),
  Ranked=lists:sort(Cases),
  io:format("~B same-length functions with oversized allocate first~n",[length(Cases)]),
  lists:foreach(fun({D,P,K,O,N})->io:format("~0p ~0p ~0p ~0p~n",[D,P,K,[{A,B}||{A,B}<-lists:zip(O,N),A=/=B]]) end,lists:sublist(Ranked,100)).
read(P)->
  {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
  maps:from_list([{{N,A},norm(Is)}||{function,N,A,_,Is}<-Fs]).
norm(Is)->
  Ids=maps:from_list(lists:zip([L||{label,L}<-Is],lists:seq(1,length([L||{label,L}<-Is])))),
  F=fun G({label,L})->{label,maps:get(L,Ids,L)};
        G({f,L})->{f,maps:get(L,Ids,L)};
        G({literal,_}=T)->T;
        G(T) when is_tuple(T)->list_to_tuple(G(tuple_to_list(T)));
        G([H|T])->[G(H)|G(T)];
        G(T)->T end,
  F(Is).
diff([I|R],[I|C])->diff(R,C);
diff([{allocate,S,L}|R],[{allocate,S2,L}|C]) when S2>S,length(R)=:=length(C)->
  1+length([ok||{A,B}<-lists:zip(R,C),A=/=B]);
diff(_,_) -> false.
