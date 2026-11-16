-mode(compile).

main([Official, Before, After, PathsFile]) ->
    {ok, Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data, <<10>>, [global]), P =/= <<>>],
    Rows = [{P, counts(Before,Official,P), counts(After,Official,P)} || P <- Paths],
    Deltas = [difference(Old,New) || {_,Old,New} <- Rows],
    {Y,X,Moves,Frames,Slots} = lists:foldl(fun sum/2,{0,0,0,0,0},Deltas),
    io:format("~B more direct X projections, ~B fewer Y projections, ~B fewer Y-to-X moves, and ~B fewer slots across ~B changed images.~n",
              [-X,Y,Moves,Slots,length(Paths)]),
    io:format("Allocation instructions removed: ~B.~n",[Frames]),
    [io:format("~s ~0p~n",[P,difference(Old,New)]) || {P,Old,New} <- Rows],
    ok.

counts(Dir,Root,P) ->
    Candidate = filename:join(Dir,P),
    File = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    lists:foldl(fun count/2,{0,0,0,0,0},[I || {function,_,_,_,Is} <- Functions,I <- Is]).

count({get_tuple_element,_,_,{y,_}},{Y,X,M,F,S}) -> {Y+1,X,M,F,S};
count({get_tuple_element,_,_,{x,_}},{Y,X,M,F,S}) -> {Y,X+1,M,F,S};
count({move,{y,_},{x,_}},{Y,X,M,F,S}) -> {Y,X,M+1,F,S};
count({allocate,Slots,_},{Y,X,M,F,S}) -> {Y,X,M,F+1,S+Slots};
count({allocate_heap,Slots,_,_},{Y,X,M,F,S}) -> {Y,X,M,F+1,S+Slots};
count(_I,Acc) -> Acc.

difference({Y0,X0,M0,F0,S0},{Y1,X1,M1,F1,S1}) -> {Y0-Y1,X0-X1,M0-M1,F0-F1,S0-S1}.
sum({Y0,X0,M0,F0,S0},{Y1,X1,M1,F1,S1}) -> {Y0+Y1,X0+X1,M0+M1,F0+F1,S0+S1}.
