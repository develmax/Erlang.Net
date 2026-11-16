-mode(compile).

main([Official, Old, New, PathFile]) ->
    {ok, Data} = file:read_file(PathFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data, <<10>>, [global]), P =/= <<>>],
    Rows = lists:append([changes(Official, Old, New, P) || P <- Paths]),
    Counts = lists:foldl(fun({P,_,Outcome}, Acc) ->
        maps:update_with(Outcome, fun({Modules,N}) -> {Modules#{P=>true},N+1} end, {#{P=>true},1}, Acc)
    end, #{}, Rows),
    io:format("Previous tagged-tuple versus allocation first differences whose shape changed.~n"),
    [io:format("~B modules ~B functions ~0p~n",[M,N,G]) ||
        {M,N,G} <- lists:reverse(lists:sort([{map_size(Ms),N,Shape} || {Shape,{Ms,N}} <- maps:to_list(Counts)]))],
    [io:format("~s ~0p ~0p~n",[P,F,G]) || {P,F,G} <- Rows].

changes(Official, Old, New, Path) ->
    Ref = filename:join(Official, Path),
    Before = compare(Ref, image(Old, Ref, Path)),
    After = maps:from_list(compare(Ref, image(New, Ref, Path))),
    [{Path,F,Difference} || {F,{is_tagged_tuple,allocate}} <- Before,
        Difference <- [maps:get(F,After,matching)], Difference =/= {is_tagged_tuple,allocate}].

image(Dir, Ref, Path) ->
    Candidate = filename:join(Dir,Path),
    case filelib:is_regular(Candidate) of true -> Candidate; false -> Ref end.

compare(Reference,Generated) ->
    R=functions(Reference),
    G=functions(Generated),
    [{F,first_difference(maps:get(F,R,undefined),maps:get(F,G,undefined))}
        || F <- lists:usort(maps:keys(R)++maps:keys(G)),
           maps:get(F,R,undefined) =/= maps:get(F,G,undefined)].

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
