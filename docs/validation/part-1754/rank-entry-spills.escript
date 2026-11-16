-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = [{P, compare(filename:join(Official,P), filename:join(Generated,P))} || P <- Paths],
    Diff = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    io:format("~B modules ~B functions with oversized entry frames, equal live X counts and extra entry argument spills.~n",
        [length(Diff),lists:sum([length(Fs) || {_,Fs} <- Diff])]),
    [io:format("~s ~0p~n",[P,Fs]) || {P,Fs} <- Diff],
    ok.

compare(Reference,Generated) ->
    R=functions(Reference), G=functions(Generated),
    [{F,{S0,S1},{M0,M1}} || {F,{K,S1,L,M1}} <- maps:to_list(G),
                            {ok,{K0,S0,L0,M0}} <- [maps:find(F,R)],
                            K =:= K0, L =:= L0, S1 > S0, M1 > M0].

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},entry(Code)} || {function,N,A,_,Code} <- Fs]).

entry([_,_,_,{label,_},{allocate,S,L}|Code]) ->
    {allocate,S,L,spills(Code,0)};
entry([_,_,_,{label,_},{allocate_heap,S,H,L}|Code]) ->
    {{allocate_heap,H},S,L,spills(Code,0)};
entry(_) ->
    none.

spills([{move,{x,_},{y,_}}|Rest],N) ->
    spills(Rest,N+1);
spills([{init_yregs,_}|Rest],N) ->
    spills(Rest,N);
spills([{line,_}|Rest],N) ->
    spills(Rest,N);
spills(_,N) ->
    N.
