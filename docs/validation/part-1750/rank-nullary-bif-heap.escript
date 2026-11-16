-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = [{Path,compare(filename:join(Official,Path),filename:join(Generated,Path))}
            || Path <- Paths],
    Matches = [{P,Ms} || {P,[_|_]=Ms} <- Rows],
    io:format("~B modules, ~B paired heap tests after nullary BIFs that can move to the OTP live count.~n",
              [length(Matches),lists:sum([length(Ms) || {_,Ms} <- Matches])]),
    [io:format("~s ~0p~n",[P,Ms]) || {P,Ms} <- Matches],
    ok.

compare(Origin,Generated) ->
    R = functions(Origin), C = functions(Generated),
    [{F,Ref,Cur} || {F,CG} <- maps:to_list(C),
                  {ok,RG} <- [maps:find(F,R)],
                  [Sz || {Sz,_,_,_} <- RG] =:= [Sz || {Sz,_,_,_} <- CG],
                  {{_,Ref,_,Following},{_,Cur,Reduced,_}} <- lists:zip(RG,CG),
                  Reduced < Cur, Ref =:= Reduced, Following].

functions(File) ->
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(File),
    maps:from_list([{{Name,A},heaps(Code,[],[])} || {function,Name,A,_,Code} <- Fs]).

heaps([{test_heap,Size,Live}|Rest],Prev,Acc) ->
    Following = case Rest of [{bif,Name,nofail,[],{x,_}}|_] when Name =:= self; Name =:= node -> true; _ -> false end,
    heaps(Rest,[],[{Size,Live,reduce(Prev,Live),Following}|Acc]);
heaps([{bif,Name,nofail,[],{x,D}}|Rest],Prev,Acc) when Name =:= self; Name =:= node ->
    heaps(Rest,[{-1,D}|Prev],Acc);
heaps([_|Rest],_,Acc) ->
    heaps(Rest,[],Acc);
heaps([],_,Acc) ->
    lists:reverse(Acc).

reduce([{S,D}|Prev],Live) when S < Live, S =:= D orelse D >= Live ->
    reduce(Prev,Live);
reduce([{S,D}|Prev],Live) when D =:= Live-1, S < D ->
    reduce(Prev,Live-1);
reduce(_,Live) ->
    Live.
