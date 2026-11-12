#!/usr/bin/env escript
-mode(compile).
main([Root, Generated]) ->
    Files = filelib:fold_files(Generated, "\\.beam$", true, fun(P, A) -> [P|A] end, []),
    Prefix = length(filename:split(Generated)),
    Rows = [compare(Root, Prefix, P) || P <- Files],
    io:format("~B differing images compared by section payload~n", [length(Rows)]),
    summarize("Differing section sets", [S || {_, S} <- Rows], 18),
    summarize("Files affected by section", lists:append([S || {_, S} <- Rows]), 20),
    Closest = lists:sublist(lists:sort([{length(S), P, S} || {P,S} <- Rows]), 15),
    io:format("Closest files by count of changed sections~n"),
    lists:foreach(fun({N, P, S}) -> io:format("~B ~s ~0p~n", [N, P, S]) end, Closest).
compare(Root, Prefix, Path) ->
    Rel = lists:nthtail(Prefix, filename:split(Path)),
    Ref = filename:join([Root|Rel]),
    {ok, _, Original} = beam_lib:all_chunks(Ref),
    {ok, _, Produced} = beam_lib:all_chunks(Path),
    A = maps:from_list(Original),
    B = maps:from_list(Produced),
    Keys = ordsets:union(lists:sort(maps:keys(A)), lists:sort(maps:keys(B))),
    Changed = [K || K <- Keys, maps:get(K, A, missing) =/= maps:get(K, B, missing)],
    SameSections = [K || K <- Keys, maps:is_key(K,A), maps:is_key(K,B)],
    OriginalOrder = [K || {K,_} <- Original, lists:member(K, SameSections)],
    ProducedOrder = [K || {K,_} <- Produced, lists:member(K, SameSections)],
    Order = case OriginalOrder =:= ProducedOrder of true -> []; false -> [section_order] end,
    {filename:join(Rel), Changed ++ Order}.
summarize(Label, Values, Limit) ->
    Histogram = lists:foldl(fun(S, M) -> maps:update_with(S, fun(N) -> N+1 end, 1, M) end, #{}, Values),
    Sorted = lists:reverse(lists:sort([{N,S} || {S,N} <- maps:to_list(Histogram)])),
    io:format("~s~n", [Label]),
    lists:foreach(fun({N,S}) -> io:format("~B ~0p~n", [N,S]) end, lists:sublist(Sorted, Limit)).
