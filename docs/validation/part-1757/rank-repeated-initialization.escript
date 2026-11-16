-mode(compile).

main([Official,Generated]) ->
    Paths=filelib:wildcard("**/*.beam",Generated),
    Rows=[{P,compare(filename:join(Official,P),filename:join(Generated,P))} || P <- Paths],
    Matches=[{P,Fs} || {P,[_|_]=Fs} <- Rows],
    io:format("~B modules ~B functions with more repeated clears of already-cleared Y slots in initial straight-line frame setup than OTP.~n",
        [length(Matches),lists:sum([length(Fs) || {_,Fs} <- Matches])]),
    [io:format("~s ~0p~n",[P,Fs]) || {P,Fs} <- Matches],
    ok.

compare(Reference,Generated) ->
    R=functions(Reference), G=functions(Generated),
    [{F,RD,GD} || {F,GD} <- maps:to_list(G), GD > 0, {ok,RD} <- [maps:find(F,R)], GD > RD].

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},frames(Is,0)} || {function,N,A,_,Is} <- Fs]).

frames([{allocate,_,_}|Code],Count) ->
    {Rest,Dups}=prefix(Code,[],0),
    frames(Rest,Count+Dups);
frames([{allocate_heap,_,_,_}|Code],Count) ->
    {Rest,Dups}=prefix(Code,[],0),
    frames(Rest,Count+Dups);
frames([_|Code],Count) -> frames(Code,Count);
frames([],Count) -> Count.

prefix([{init_yregs,{list,Ys}}|Code],Cleared,Count) ->
    Set=ordsets:from_list(Ys),
    prefix(Code,ordsets:union(Cleared,Set),Count+length(ordsets:intersection(Set,Cleared)));
prefix([{move,{x,_},{y,_}=D}|Code],Cleared,Count) ->
    prefix(Code,ordsets:del_element(D,Cleared),Count);
prefix([{line,_}|Code],Cleared,Count) ->
    prefix(Code,Cleared,Count);
prefix(Code,_,Count) -> {Code,Count}.
