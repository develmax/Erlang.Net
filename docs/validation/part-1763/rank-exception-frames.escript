-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Counts = lists:foldl(fun(P,Acc0) ->
        R=functions(filename:join(Official,P)),
        G=functions(filename:join(Generated,P)),
        lists:foldl(fun({F,Code},Acc) ->
            case maps:find(F,R) of
                {ok,Ref} when Ref =/= Code ->
                    case first_difference(Ref,Code) of
                        {allocate,slots,higher} ->
                            lists:foldl(fun(S,M) ->
                                maps:update_with(S,fun({Files,N}) -> {Files#{P=>true},N+1} end,
                                                 {#{P=>true},1},M)
                            end,Acc,[all|classify_code(Code)]);
                        _ -> Acc
                    end;
                _ -> Acc
            end
        end,Acc0,maps:to_list(G))
    end,#{},Paths),
    Ranked=lists:reverse(lists:sort([{map_size(Files),N,S} || {S,{Files,N}}<-maps:to_list(Counts)])),
    io:format("Functions whose first difference is a larger allocate frame with equal live X count. Handler rows report {handlers,TryCount,CatchCount,FrameCount,UsesTrim,UsesReceive,UsesDebug}.~n"),
    [io:format("~B modules ~B functions ~0p~n",[M,N,S]) || {M,N,S} <- Ranked],
    ok.

classify_code(Code) ->
    Ops=[opcode(I)||I<-Code],
    Try=length([ok||'try'<-Ops]),
    Catch=length([ok||'catch'<-Ops]),
    case Try+Catch of
        0 -> [];
        _ ->
            Frames=length([ok||O<-Ops,lists:member(O,[allocate,allocate_heap,allocate_zero,allocate_heap_zero])]),
            Trim=lists:member(trim,Ops),
            Receive=lists:any(fun(O)->lists:member(O,[loop_rec,wait,wait_timeout]) end,Ops),
            Debug=lists:member(debug_line,Ops),
            [exception_handler,{handlers,Try,Catch,Frames,Trim,Receive,Debug}]
    end.
first_difference([I|R],[I|G]) -> first_difference(R,G);
first_difference([{allocate,S,R}|_],[{allocate,S,G}|_]) -> {allocate,live,order(R,G)};
first_difference([{allocate,R,L}|_],[{allocate,G,L}|_]) -> {allocate,slots,order(R,G)};
first_difference([{allocate,RS,RL}|_],[{allocate,GS,GL}|_]) ->
    {allocate,slots_and_live,order(RS,GS),order(RL,GL)};
first_difference([R|_],[G|_]) -> {opcode(R),opcode(G)};
first_difference([],[_|_]) -> extra;
first_difference([_|_],[]) -> shortened;
first_difference(undefined,_) -> added;
first_difference(_,undefined) -> missing.

order(R,G) when G > R -> higher;
order(R,G) when G < R -> lower.

opcode({test,Name,_,_}) -> Name;
opcode({bif,Name,_,_,_}) -> {bif,Name};
opcode(I) when is_tuple(I) -> element(1,I);
opcode(I) -> I.

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},normalize(Is)} || {function,N,A,_,Is} <- Fs]).

normalize(Is) ->
    Labels=maps:from_list(lists:zip([L || {label,L} <- Is],lists:seq(1,length([L || {label,L} <- Is])))),
    [fix(I,Labels) || I <- Is].

fix({label,L},Labels) -> {label,maps:get(L,Labels,L)};
fix({f,L},Labels) -> {f,maps:get(L,Labels,L)};
fix({literal,V},_) -> {literal,V};
fix({make_fun3,M,I,_,Dst,Args},Ls) -> {make_fun3,M,I,uniq,fix(Dst,Ls),fix(Args,Ls)};
fix(T,Ls) when is_tuple(T) -> list_to_tuple([fix(X,Ls) || X <- tuple_to_list(T)]);
fix([H|T],Ls) -> [fix(H,Ls)|fix(T,Ls)];
fix(V,_) -> V.
