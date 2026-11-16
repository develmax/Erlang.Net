-mode(compile).

main([Generated]) ->
    Files = filelib:wildcard("**/*.beam", Generated),
    Rows = [{P, candidates(filename:join(Generated,P))} || P <- Files],
    Groups = lists:foldl(fun({_, Cs}, Acc) ->
        Kinds = lists:usort([K || {_, _, K} <- Cs]),
        lists:foldl(fun(K, S) ->
            F = length(lists:usort([{N,A} || {N,A,T} <- Cs, T =:= K])),
            maps:update_with(K, fun({M,N}) -> {M+1,N+F} end, {1,F}, S)
        end,Acc,Kinds)
    end,#{},Rows),
    io:format("Call result spills immediately before dispatch that does not consume X0. ~B images examined.~n",[length(Files)]),
    [io:format("~B modules, ~B functions: ~p~n",[M,F,K])
        || {M,F,K} <- lists:reverse(lists:sort([{M,F,K} || {K,{M,F}} <- maps:to_list(Groups)]))].

candidates(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    [{N,A,K} || {function,N,A,_,Code} <- Functions, K <- scans([I || I <- Code, op(I) =/= line])].

scans([Call,{move,{x,0},{y,_}},Branch|Rest]) ->
    case lists:member(op(Call),[call,call_ext,apply,call_fun,call_fun2]) andalso not reads_result(Branch) of
        true ->
            case Branch of
                {K,_,_,_} when K =:= select_val; K =:= select_tuple_arity -> [K|scans([Branch|Rest])];
                {test,K,_,_} -> [K|scans([Branch|Rest])];
                _ -> scans([Branch|Rest])
            end;
        false -> scans([Branch|Rest])
    end;
scans([_|Rest]) -> scans(Rest);
scans([]) -> [].
reads_result({x,0}) -> true;
reads_result({literal,_}) -> false;
reads_result(T) when is_tuple(T) -> lists:any(fun reads_result/1, tuple_to_list(T));
reads_result(L) when is_list(L) -> lists:any(fun reads_result/1, L);
reads_result(_) -> false.

op(T) when is_tuple(T) -> element(1,T);
op(V) -> V.
