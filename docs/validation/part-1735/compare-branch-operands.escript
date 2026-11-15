-mode(compile).

main([Root, Before, After, PathList]) ->
    {ok, Raw} = file:read_file(PathList),
    Paths = [binary_to_list(P) || P <- binary:split(Raw, <<10>>, [global]), P =/= <<>>],
    Rows = [{P, compare(read(Root, Root, P), read(Before, Root, P), read(After, Root, P))}
            || P <- Paths],
    Changed = [{P, Ms} || {P, [_|_] = Ms} <- Rows],
    All = [M || {_, Ms} <- Changed, {_, Moves} <- Ms, M <- Moves],
    Counts = lists:foldl(fun({K, _}, Acc) -> maps:update_with(K, fun(N) -> N+1 end, 1, Acc) end, #{}, All),
    io:format("~B branch operands change from X to Y in ~B functions across ~B modules with the same previous and current predicate sequence.~n",
        [length(All),lists:sum([length(Ms) || {_,Ms} <- Changed]),length(Changed)]),
    io:format("~B match the reference operand in functions with the same reference, previous and current predicate sequence.~n",
        [length([M || {_,true} = M <- All])]),
    [io:format("~p ~B~n",[K,N]) || {N,K} <- lists:reverse(lists:sort([{N,K} || {K,N} <- maps:to_list(Counts)]))].

read(Directory, Root, Relative) ->
    Candidate = filename:join(Directory, Relative),
    Path = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root, Relative) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name,Arity},[P || I <- Code, P <- [operand(I)], P =/= none]}
        || {function,Name,Arity,_,Code} <- Functions]).

compare(Otp, Before, After) ->
    maps:fold(fun(Key, New, Acc) ->
        Old = maps:get(Key, Before, []),
        Ref = maps:get(Key, Otp, []),
        case kinds(Old) =:= kinds(New) andalso Old =/= New of
            true ->
                Reference = case kinds(Ref) =:= kinds(New) of
                    true -> Ref;
                    false -> lists:duplicate(length(New), none)
                end,
                Moves = [{K, E =:= {K,Y}} || {{K,X},{K,Y},E} <- lists:zip3(Old,New,Reference),
                                    bank(X) =:= x, bank(Y) =:= y],
                case Moves of [] -> Acc; _ -> [{Key,Moves}|Acc] end;
            false -> Acc
        end
    end, [], After).

kinds(Operands) -> [K || {K,_} <- Operands].
operand({K,V,_,_}) when K =:= select_val; K =:= select_tuple_arity -> {K,V};
operand({test,K,_,[V|_]}) -> {K,V};
operand(_) -> none.
bank({tr,V,_}) -> bank(V);
bank({Tag,_}) -> Tag;
bank(_) -> none.
