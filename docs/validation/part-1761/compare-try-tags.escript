-mode(compile).

main([Ref,Old,New,PathFile]) ->
    {ok,Data}=file:read_file(PathFile),
    Paths=[binary_to_list(P)||P<-binary:split(Data,<<10>>,[global]),P=/=<<>>],
    Rows=lists:append([compare(Ref,Old,New,P)||P<-Paths]),
    Gains=[I||{_,_,R,O,N}=I<-Rows,R=/=O,R=:=N],
    Losses=[I||{_,_,R,O,N}=I<-Rows,R=:=O,R=/=N],
    Absolute=[I||{_,_,R,O,N}=I<-Rows,R=/=O,R=:=N,element(1,R)=:=element(1,O)],
    io:format("~B changed comparable try frames, ~B new matches and ~B lost matches for {frame size,tag register}. ~B gains had matching frame sizes before the change.~n",
              [length(Rows),length(Gains),length(Losses),length(Absolute)]),
    Refs=[S-1-Y||{_,_,{S,Y},_,_}<-Rows],
    io:format("OTP places the tag in the highest slot in ~B of ~B changed comparable try frames.~n",
              [length([ok||0<-Refs]),length(Refs)]),
    io:format("Example gains: ~0p~nExample losses: ~0p~n",[lists:sublist(Gains,12),lists:sublist(Losses,12)]).

compare(Root,Old,New,P) ->
    Ref=read(Root,Root,P), Prev=read(Old,Root,P), Next=read(New,Root,P),
    [{P,F,R,O,N}||{F,N}<-maps:to_list(Next),{ok,R}<-[maps:find(F,Ref)],{ok,O}<-[maps:find(F,Prev)],O=/=N].

read(Dir,Root,P) ->
    Name=filename:join(Dir,P),
    File=case filelib:is_regular(Name) of true->Name;false->filename:join(Root,P) end,
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(File),
    maps:from_list([{{F,A},Pair}||{function,F,A,_,Code}<-Fs, {ok,Pair}<-[frame(Code)]]).

frame(Code) ->
    Alloc=[S||I<-Code,{ok,S}<-[size_of_frame(I)]],
    Tags=[Y||{'try',{y,Y},_}<-Code],
    case {Alloc,Tags} of {[S],[Y]}->{ok,{S,Y}};_->skip end.

size_of_frame({allocate,S,_}) -> {ok,S};
size_of_frame({allocate_heap,S,_,_}) -> {ok,S};
size_of_frame(_) -> skip.