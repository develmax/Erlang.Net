-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Rows = [{P,compare(filename:join(Official,P),filename:join(Generated,P))} || P <- Paths],
    M = [{P,F} || {P,[_|_]=F} <- Rows],
    io:format("~B modules, ~B corresponding functions with larger live-X counts at equal-sized frames.~n",
        [length(M),lists:sum([length(F)||{_,F}<-M])]),
    [io:format("~s ~0p~n",[P,F]) || {P,F} <- M], ok.

compare(Root,Generated) ->
    Ref = read(Root), Candidate = read(Generated),
    [{F,R,C} || {F,C} <- maps:to_list(Candidate),
                {ok,R} <- [maps:find(F,Ref)],
                length(R) =:= length(C),
                [S || {S,_} <- R] =:= [S || {S,_} <- C],
                lists:any(fun({{_,L0},{_,L1}})->L0<L1 end,lists:zip(R,C))].
read(File) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},[A || I <- Code,A <- frame(I)]} || {function,Name,Arity,_,Code} <- Functions]).
frame({allocate,S,L}) -> [{{plain,S},L}];
frame({allocate_heap,S,_,L}) -> [{{heap,S},L}];
frame(_) -> [].
