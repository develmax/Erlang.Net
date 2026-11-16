-mode(compile).

main([Original,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = [{P, compare(filename:join(Original,P),filename:join(Generated,P))} || P <- Paths],
    Counts = lists:foldl(fun({P,Values}, Acc0) ->
        lists:foldl(fun({F,State}, Acc) ->
            maps:update_with(State,fun({Ms,Fs,N}) -> {Ms#{P=>true},Fs#{{P,F}=>true},N+1} end,
                                {#{P=>true},#{{P,F}=>true},1},Acc)
        end,Acc0,Values)
    end,#{},Rows),
    Ranked = lists:reverse(lists:sort([{map_size(Ms),map_size(Fs),N,S} || {S,{Ms,Fs,N}} <- maps:to_list(Counts)])),
    io:format("First textual uses of spills in comparable oversized entry frames. Paths may diverge before a use.~n"),
    [io:format("~B modules ~B functions ~B spills ~0p~n",[M,F,N,S]) || {M,F,N,S} <- Ranked],
    ok.

compare(Original,Generated) ->
    R=functions(Original), G=functions(Generated),
    [{F,classify(X,Y,Body,false,none)} || {F,{K,Count,L,Saves,Body}} <- maps:to_list(G),
            {ok,{RKind,RS,RL,RSaves,_}} <- [maps:find(F,R)],
            RKind =:= K, RL =:= L, RS < Count, length(RSaves) < length(Saves),
            {X,Y} <- Saves].

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},entry(Code)} || {function,N,A,_,Code} <- Fs]).

entry([_,_,_,{label,_},{allocate,S,L}|Body]) ->
    {Saves,Rest}=spills(Body,[]),
    {allocate,S,L,Saves,Rest};
entry([_,_,_,{label,_},{allocate_heap,S,H,L}|Body]) ->
    {Saves,Rest}=spills(Body,[]),
    {{allocate_heap,H},S,L,Saves,Rest};
entry(_) -> none.

spills([{move,{x,_}=X,{y,_}=Y}|Rest],Acc) -> spills(Rest,[{X,Y}|Acc]);
spills([{init_yregs,_}|Rest],Acc) -> spills(Rest,Acc);
spills([{line,_}|Rest],Acc) -> spills(Rest,Acc);
spills(Body,Acc) -> {Acc,Body}.

classify(X,Y,[I|Rest],Call,Clobber) ->
    case occurs(Y,I) of
        true -> {Call,Clobber,opcode(I)};
        false ->
            NewCall = Call orelse lists:member(opcode(I),[call,call_ext,call_fun,call_fun2,apply,send,wait,wait_timeout]),
            NewClobber = case Clobber of
                none -> clobber(X,I);
                _ -> Clobber
            end,
            classify(X,Y,Rest,NewCall,NewClobber)
    end;
classify(_,_,[],_,_) -> no_use.

clobber(X,{move,_,X}) -> move;
clobber(X,{get_tuple_element,_,_,X}) -> get_tuple_element;
clobber(X,{get_list,_,Hd,Tl}) when X =:= Hd; X =:= Tl -> get_list;
clobber(X,{put_tuple2,X,_}) -> put_tuple2;
clobber(X,{put_list,_,_,X}) -> put_list;
clobber(X,{bif,_,_,_,X}) -> bif;
clobber(X,{gc_bif,_,_,_,_,X}) -> gc_bif;
clobber(_,_) -> none.

occurs(X,X) -> true;
occurs(X,{tr,Reg,_}) -> X =:= Reg;
occurs(_,{literal,_}) -> false;
occurs(X,T) when is_tuple(T) -> lists:any(fun(E) -> occurs(X,E) end,tuple_to_list(T));
occurs(X,L) when is_list(L) -> lists:any(fun(E) -> occurs(X,E) end,L);
occurs(_,_) -> false.

opcode({test,Name,_,_}) -> Name;
opcode(I) when is_tuple(I) -> element(1,I);
opcode(I) -> I.
