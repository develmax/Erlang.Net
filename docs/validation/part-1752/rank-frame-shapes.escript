-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = [{P, compare(filename:join(Official,P), filename:join(Generated,P))} || P <- Paths],
    Counts = lists:foldl(fun({P,Functions}, Acc0) ->
        lists:foldl(fun({_,Shapes}, Acc1) ->
            lists:foldl(fun(Shape,Acc) ->
                maps:update_with(Shape,fun({Ms,N}) -> {Ms#{P=>true},N+1} end,{#{P=>true},1},Acc)
            end, Acc1, Shapes)
        end, Acc0, Functions)
    end, #{}, Rows),
    Ranked = lists:reverse(lists:sort([{map_size(Ms),N,S} || {S,{Ms,N}} <- maps:to_list(Counts)])),
    io:format("Overlapping shapes in functions whose first allocation has more slots than OTP:~n"),
    [io:format("~B modules ~B functions ~p~n",[M,N,S]) || {M,N,S} <- Ranked],
    ok.

compare(Reference,Generated) ->
    R=functions(Reference),
    G=functions(Generated),
    [{F,shapes(CG)} || {F,CG} <- maps:to_list(G), {ok,RG} <- [maps:find(F,R)],
                       [CS|_] <- [[N || I <- CG, N <- frame(I)]],
                       [RS|_] <- [[N || I <- RG, N <- frame(I)]],
                       CS > RS].

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},Code} || {function,N,A,_,Code} <- Fs]).

shapes(Code) ->
    Ops=[opcode(I) || I <- Code],
    Frames=[N || I <- Code,N <- frame(I)],
    [_|_] = Frames,
    [_|Pred] = lists:dropwhile(fun(O) -> O =/= func_info end, Ops),
    Entry = lists:dropwhile(fun(O) -> lists:member(O,[label,line]) end, Pred),
    StackFrameAtEntry = case Entry of [allocate|_] -> true; [allocate_heap|_] -> true; _ -> false end,
    [oversized] ++ [single_frame || length(Frames) =:= 1] ++ [multiple_frames || length(Frames) > 1]
      ++ [frame_at_entry || StackFrameAtEntry] ++ [late_frame || not StackFrameAtEntry]
      ++ [trim || lists:member(trim,Ops)] ++ [wait || lists:member(wait,Ops) orelse lists:member(wait_timeout,Ops)]
      ++ [exception_frame || lists:member('try',Ops) orelse lists:member('catch',Ops)]
      ++ [frame_at_entry_without_trim_or_try || StackFrameAtEntry, length(Frames) =:= 1,
                    not lists:any(fun(O) -> lists:member(O,[trim,'try','catch']) end,Ops)].

frame({allocate,S,_}) -> [S];
frame({allocate_heap,S,_,_}) -> [S];
frame(_) -> [].

opcode(I) when is_tuple(I) -> element(1,I);
opcode(I) -> I.
