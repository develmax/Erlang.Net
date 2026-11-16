-mode(compile).

main([Reference, Candidate, PathsFile]) ->
    {ok, Data} = file:read_file(PathsFile),
    Equal = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Different = filelib:wildcard("**/*.beam",Candidate),
    report(equal,Reference,Equal),
    report(release_different,Reference,Different),
    report(generated_different,Candidate,Different).

report(Group, Root, Paths) ->
    Rows = [{P,scan(filename:join(Root,P))} || P <- Paths],
    Found = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    All = lists:append([Fs || {_,Fs} <- Found]),
    io:format("~p: ~B initializations overwritten by argument saves in ~B functions across ~B of ~B modules.~n",
              [Group,lists:sum([N || {_,_,N} <- All]),length(All),length(Found),length(Paths)]),
    [io:format("~s ~0p~n",[P,lists:sublist(Fs,5)]) || {P,Fs} <- lists:sublist(Found,12)].

scan(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    [{Name,Arity,Count} || {function,Name,Arity,_,Code} <- Functions,
                          Count <- [count(Code,0)], Count > 0].
count([Alloc|T], N) when is_tuple(Alloc), element(1,Alloc) =:= allocate;
                          is_tuple(Alloc), element(1,Alloc) =:= allocate_heap ->
    {Discarded,Rest} = moves(T,[],0),
    count(Rest,N+Discarded);
count([_|T], N) -> count(T,N);
count([], N) -> N.

moves([{init_yregs,{list,Slots}}|T], Pending, N) ->
    moves(T, ordsets:union(Pending,ordsets:from_list(Slots)), N);
moves([{move,{x,_},{y,_}=Slot}|T], Pending, N) ->
    Cut = case ordsets:is_element(Slot,Pending) of true -> 1; false -> 0 end,
    moves(T,ordsets:del_element(Slot,Pending),N+Cut);
moves(Rest, _, N) -> {N,Rest}.
