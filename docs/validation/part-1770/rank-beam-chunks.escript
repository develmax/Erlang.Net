-mode(compile).
main([Official, Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = [compare(Official, Generated, P) || P <- Paths],
    CountNoCode = length([ok || {_, Ds, _, _} <- Rows, not lists:member("Code", Ds)]),
    CountSamePayload = length([ok || {_, [], _, _} <- Rows]),
    CountSameInstructions = length([ok || {_,_,_,true} <- Rows]),
    io:format("~B differing images, ~B with the same Code payload and ~B with all chunk payloads equal.~n",
              [length(Paths), CountNoCode, CountSamePayload]),
    io:format("~B differing images have identical instruction bytes after removing the Code header.~n",[CountSameInstructions]),
    io:format("Difference sets ranked by release module count.~n"),
    print_rank([Ds || {_, Ds, _, _} <- Rows]),
    io:format("Individual chunks ranked by release module count.~n"),
    print_rank(lists:append([Ds || {_, Ds, _, _} <- Rows])),
    io:format("Modules with the same Code payload.~n"),
    [io:format("~s ~0p order_equal=~p~n",[P, Ds, Order]) ||
        {P, Ds, Order, _} <- Rows, not lists:member("Code", Ds)].

compare(Official, Generated, Path) ->
    R=chunks(filename:join(Official,Path)),
    G=chunks(filename:join(Generated,Path)),
    {RKeys,_}=lists:unzip(R),
    {GKeys,_}=lists:unzip(G),
    Keys=lists:usort(RKeys ++ GKeys),
    Ds=[K || K <- Keys, proplists:get_all_values(K,R) =/= proplists:get_all_values(K,G)],
    {Path,Ds,RKeys=:=GKeys,instructions(R) =:= instructions(G)}.

instructions(Chunks) ->
    <<Size:32,_:Size/binary,Body/binary>> = proplists:get_value("Code",Chunks),
    Body.

chunks(Path) ->
    {ok,_,Data}=beam_lib:all_chunks(Path),
    Data.

print_rank(Groups) ->
    Counts=lists:foldl(fun(K,M)->maps:update_with(K,fun(N)->N+1 end,1,M) end,#{},Groups),
    [io:format("~B ~0p~n",[N,K]) || {N,K} <-
        lists:reverse(lists:sort([{N,K} || {K,N} <- maps:to_list(Counts)]))].
