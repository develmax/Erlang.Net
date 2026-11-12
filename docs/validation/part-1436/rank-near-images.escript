#!/usr/bin/env escript
-mode(compile).

main([Root, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
Norm=fun(Is) ->
    Labels=maps:from_list(lists:zip([L || {label,L}<-Is],lists:seq(1,length([L || {label,L}<-Is])))),
    Fix=fun F({label,L})->{label,maps:get(L,Labels,L)};
        F({f,L})->{f,maps:get(L,Labels,L)};
        F({literal,V})->{literal,V};
        F({make_fun3,M,I,_,Dst,Args})->{make_fun3,M,I,uniq,F(Dst),F(Args)};
        F(T) when is_tuple(T)->list_to_tuple([F(X)||X<-tuple_to_list(T)]);
        F([H|T])->[F(H)|F(T)];
        F(V)->V
    end,
    [Fix(I)||I<-Is]
end,
Read=fun(P)->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},Norm(Is)}||{function,N,A,_,Is}<-Fs])
end,
    Rows = [begin
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = Read(filename:join(Root, Relative)),
        Actual = Read(Path),
        Keys = lists:usort(maps:keys(Ref) ++ maps:keys(Actual)),
        Diff = [Key || Key <- Keys, maps:find(Key, Ref) =/= maps:find(Key, Actual)],
        {length(Diff), Relative, Diff}
    end || Path <- Files],
    Close = [{Count, P, Diff} || {Count, P, Diff} <- Rows, Count =< 3],
    io:format("~p differing images, ~p with at most 3 differing normalized function bodies.~n",
              [length(Rows), length(Close)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Close)).
