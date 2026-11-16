-mode(compile).

main([Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = [{Path, scan(filename:join(Generated,Path))} || Path <- Paths],
    Counts = lists:foldl(fun({_,Cases},Acc) ->
        Groups = lists:usort([G||{_,_,G} <- Cases]),
        lists:foldl(fun(Group, A) ->
            F=length(lists:usort([{N,Arity} || {N,Arity,G} <- Cases, G =:= Group])),
            maps:update_with(Group,fun({M,T})->{M+1,T+F} end,{1,F},A)
        end,Acc,Groups)
    end,#{},Rows),
    io:format("Saved call results consumed after a heap test with zero live X registers. ~B images examined.~n",[length(Paths)]),
    [io:format("~B modules, ~B functions: ~0p~n",[M,F,G])
        || {M,F,G} <- lists:reverse(lists:sort([{M,F,G}||{G,{M,F}}<-maps:to_list(Counts)]))].

scan(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    [{N,A,G} || {function,N,A,_,Code} <- Functions, G <- pattern([I || I <- Code, op(I) =/= line])].

pattern([Call,{move,{x,0},{y,_}=Slot},{test_heap,_,0},Use|Tail]=Code) ->
    case lists:member(op(Call),[call,call_ext,apply,call_fun,call_fun2]) andalso reads(Use,Slot) of
        true -> [{op(Use), next(Tail)}|pattern(Tail)];
        false -> pattern(tl(Code))
    end;
pattern([_|Tail]) -> pattern(Tail);
pattern([]) -> [].
next([{label,_}|Tail]) -> next(Tail);
next([{deallocate,_},return|_]) -> returns;
next([I|_]) -> op(I);
next([]) -> ended.
reads(V,V) -> true;
reads({literal,_},_) -> false;
reads(T,V) when is_tuple(T) -> reads(tuple_to_list(T),V);
reads(List,V) when is_list(List) -> lists:any(fun(I)->reads(I,V) end,List);
reads(_,_) -> false.
op(T) when is_tuple(T) -> element(1,T);
op(V) -> V.
