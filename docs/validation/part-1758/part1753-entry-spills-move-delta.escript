-mode(compile).

main([Official,Before,After,PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Rows = [{P,changes(Official,Before,After,P)} || P <- Paths],
    Gains = lists:sum([G || {_,{G,_,_,_}} <- Rows]),
    Losses = lists:sum([L || {_,{_,L,_,_}} <- Rows]),
    Compared = lists:sum([N || {_,{_,_,N,_}} <- Rows]),
    Gap = lists:sum([D || {_,{_,_,_,D}} <- Rows]),
    io:format("~B entry spill-count comparisons at matching allocation kinds and live X counts across ~B changed images, with ~B new matches and ~B lost matches.~n",
              [Compared,length(Paths),Gains,Losses]),
    io:format("Sum of absolute spill-count gaps decreases by ~B across comparable allocations.~n",[Gap]),
    [io:format("~s ~0p~n",[P,Delta]) || {P,Delta} <- Rows],
    ok.

changes(Official,Before,After,P) ->
    Ref = read(Official,Official,P),
    Old = read(Before,Official,P),
    New = read(After,Official,P),
    Matched = lists:append([compare(R,O,N)
               || {F,N} <- maps:to_list(New),
                  {ok,O} <- [maps:find(F,Old)], {ok,R} <- [maps:find(F,Ref)]]),
    {length([ok || {R,O,N} <- Matched,R =/= O,R =:= N]),
     length([ok || {R,O,N} <- Matched,R =:= O,R =/= N]),
     length(Matched),
     lists:sum([abs(R-O)-abs(R-N) || {R,O,N} <- Matched])}.

compare(R,O,N) ->
    {RS,RL} = lists:unzip(R),
    {OS,OL} = lists:unzip(O),
    {NS,NL} = lists:unzip(N),
    case RS =:= OS andalso OS =:= NS of
        true -> [{R0,O0,N0} || {R0,O0,N0} <- lists:zip3(RL,OL,NL), O0 =/= N0];
        false -> []
    end.

read(Dir,Root,P) ->
    Candidate = filename:join(Dir,P),
    File = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},entry(Code)} || {function,Name,Arity,_,Code} <- Functions]).

entry([_,_,_,{label,_},{allocate,_,Live}|Code]) ->
    [{{allocate,Live},spills(Code,0)}];
entry([_,_,_,{label,_},{allocate_heap,_,Heap,Live}|Code]) ->
    [{{allocate_heap,Heap,Live},spills(Code,0)}];
entry(_) ->
    [].

spills([{move,{x,_},{y,_}}|Rest],N) ->
    spills(Rest,N+1);
spills([{init_yregs,_}|Rest],N) ->
    spills(Rest,N);
spills([{line,_}|Rest],N) ->
    spills(Rest,N);
spills(_,N) ->
    N.