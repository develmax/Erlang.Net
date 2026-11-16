-mode(compile).
main([Ref, Prior, Current]) ->
    R=read(Ref),
    P=read(Prior),
    C=read(Current),
    [io:format("~0p prior ~p current ~p~n",[F,maps:get(F,P,missing)=:=Code,maps:get(F,C,missing)=:=Code])
     || {F,Code} <- lists:sort(maps:to_list(R)), element(1,F) =/= module_info].

read(Path) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Path),
    maps:from_list([{{Name,A},normalize(Code)} || {function,Name,A,_,Code}<-Fs]).

normalize(Code) ->
    Labels = [L || {label,L} <- Code],
    Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
    [fix(I,Map) || I <- Code].

fix({label,L},Map) -> {label,maps:get(L,Map,L)};
fix({f,L},Map) -> {f,maps:get(L,Map,L)};
fix({line,[{location,_,N}]},_) -> {line,N};
fix({literal,_}=I,_) -> I;
fix(T,Map) when is_tuple(T) -> list_to_tuple([fix(I,Map)||I<-tuple_to_list(T)]);
fix([H|T],Map) -> [fix(H,Map)|fix(T,Map)];
fix(I,_) -> I.
