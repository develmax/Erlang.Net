#!/usr/bin/env escript
-mode(compile).
main([Root, Candidate]) ->
    Paths = filelib:fold_files(Candidate, "\\.beam$", true, fun(P, Ps) -> [P | Ps] end, []),
    Base = length(filename:split(Candidate)),
    Results = lists:foldl(fun(Path, Acc) ->
        Ref = filename:join([Root | lists:nthtail(Base, filename:split(Path))]),
        lists:foldl(fun(Pair, Counts) -> maps:update_with(Pair, fun(N) -> N+1 end, 1, Counts) end,
                    Acc, lists:usort(compare(Ref, Path)))
    end, #{}, Paths),
    Ranked = lists:reverse(lists:sort([{Count, Pair} || {Pair, Count} <- maps:to_list(Results)])),
    io:format("Modules with first instruction mismatch per common function (~B modules examined):~n", [length(Paths)]),
    lists:foreach(fun({Count,{R,C}}) -> io:format("~B ~p -> ~p~n", [Count,R,C]) end, lists:sublist(Ranked,30)).
compare(Ref, Path) ->
    case {beam_disasm:file(Ref), beam_disasm:file(Path)} of
        {{beam_file,_,_,_,_,Rs},{beam_file,_,_,_,_,Cs}} ->
            Current = maps:from_list([{{N,A},normalize(Is)} || {function,N,A,_,Is} <- Cs]),
            [Pair || {function,N,A,_,Is} <- Rs, {ok,Cs1} <- [maps:find({N,A},Current)],
                     Pair <- [first(normalize(Is),Cs1,normalize(Is),Cs1)], Pair =/= same, begin io:format("~s ~p/~p ~0p~n",[filename:basename(Path),N,A,Pair]), true end];
        _ -> []
    end.
normalize(Is) ->
    Labels = [L || {label,L} <- Is],
    Table = maps:from_list(lists:zip(Labels, lists:seq(1,length(Labels)))),
    [norm(I,Table) || I <- Is, true].
as_tuple(T) when is_tuple(T) -> T;
as_tuple(V) -> {V}.
norm({label,L},Table) -> {label,maps:get(L,Table)};
norm({f,L},Table) -> {f,maps:get(L,Table,external)};
norm({literal,V},_) -> {literal,V};
norm({make_fun3,M,I,_,Dst,Args},Table) -> {make_fun3,M,I,unique,norm(Dst,Table),norm(Args,Table)};
norm(T,Table) when is_tuple(T) -> list_to_tuple([norm(V,Table) || V <- tuple_to_list(T)]);
norm([H|T],Table) -> [norm(H,Table)|norm(T,Table)];
norm(V,_) -> V.
first([{label,_}|R],C,Rs,Cs) -> first(R,C,Rs,Cs);
first(R,[{label,_}|C],Rs,Cs) -> first(R,C,Rs,Cs);
first([H|R],[H|C],Rs,Cs) -> first(R,C,Rs,Cs);
first([{test,is_nonempty_list,{f,RF},Reg},{get_list,_,RH,RT}|_],
      [{test,is_nonempty_list,{f,CF},Reg},{get_list,_,CH,CT}|_],Rs,Cs) when RF =/= CF ->
    signature(RF,CF,Reg,RH,RT,CH,CT,Rs,Cs);
first(_,_,_,_) -> same.
target(L, Code) when is_integer(L) ->
    case lists:dropwhile(fun(I) -> I =/= {label,L} end,Code) of
        [_|Rest] -> first_op(Rest);
        _ -> unknown
    end;
target(_,_) -> external.
first_op([{line,_}|R]) -> first_op(R);
first_op([{label,_}|R]) -> first_op(R);
first_op([{test,Op,_,_}|_]) -> Op;
first_op([I|_]) -> element(1,as_tuple(I));
first_op([]) -> ended.
reuses([Src],Src,_) -> head;
reuses([Src],_,Src) -> tail;
reuses(_,_,_) -> neither.
signature(RF,CF,Reg,RH,RT,CH,CT,Rs,Cs) ->
    R = {target(RF,Rs),reuses(Reg,RH,RT)},
    C = {target(CF,Cs),reuses(Reg,CH,CT)},
    case R =:= C of true -> same; false -> {R,C} end.