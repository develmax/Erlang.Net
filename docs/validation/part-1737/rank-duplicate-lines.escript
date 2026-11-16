-mode(compile).

main([Reference, Candidate, PathsFile]) ->
    {ok, Data} = file:read_file(PathsFile),
    Equal = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Roots = [{equal,[{P,filename:join(Reference,P)} || P <- Equal]},
             {release_different,[{P,filename:join(Reference,P)} || P <- filelib:wildcard("**/*.beam",Candidate)]},
             {generated_different,[{P,filename:join(Candidate,P)} || P <- filelib:wildcard("**/*.beam",Candidate)]}],
    [report(Kind, Rows) || {Kind, Rows} <- Roots].

report(Kind, Rows) ->
    Ms = [{P, scan(File)} || {P,File} <- Rows],
    WithLines = [{P, Fs} || {P, [_|_] = Fs} <- Ms],
    Functions = lists:sum([length(Fs) || {_,Fs} <- WithLines]),
    Count = lists:sum([N || {_,Fs} <- WithLines, {_,_,N} <- Fs]),
    io:format("~p: ~B redundant lines in ~B functions across ~B of ~B modules.~n",
              [Kind,Count,Functions,length(WithLines),length(Rows)]),
    [io:format("~s ~0p~n",[P,lists:sublist(Fs,5)]) || {P,Fs} <- lists:sublist(WithLines,5)].

scan(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    [{N,A,Count} || {function,N,A,_,Is} <- Functions, Count <- [count(Is,none,0)], Count > 0].
count([{line,Loc}|T], Loc, N) -> count(T,Loc,N+1);
count([{line,Loc}|T], _, N) -> count(T,Loc,N);
count([{label,_}|T], _, N) -> count(T,none,N);
count([{debug_line,_,_,_,_}|T], _, N) -> count(T,none,N);
count([{executable_line,_,_}|T], _, N) -> count(T,none,N);
count([_|T], Loc, N) -> count(T,Loc,N);
count([], _, N) -> N.
