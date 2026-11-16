-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = lists:append([entries(P, filename:join(Official,P), filename:join(Generated,P)) || P <- Paths]),
    Counts = lists:foldl(fun({P,_,G}, Acc) ->
        maps:update_with(G, fun({Modules,N}) -> {Modules#{P=>true},N+1} end, {#{P=>true},1}, Acc)
    end, #{}, Rows),
    io:format("Functions whose first differing instruction is a tagged-tuple test in OTP and allocation in C#.~n"),
    [io:format("~B modules ~B functions ~0p~n",[M,N,G]) ||
        {M,N,G} <- lists:reverse(lists:sort([{map_size(Ms),N,Shape} || {Shape,{Ms,N}} <- maps:to_list(Counts)]))],
    [io:format("~s ~0p ~0p~n",[P,F,G]) || {P,F,G} <- Rows].

entries(Path, Reference, Generated) ->
    Diffs = [F || {F,{is_tagged_tuple,allocate}} <- compare(Reference,Generated)],
    case {Diffs,beam_lib:chunks(Reference,[abstract_code])} of
        {[_|_],{ok,{_,[{abstract_code,{raw_abstract_v1,Forms}}]}}} ->
            Index=maps:from_list([{{N,A},Cs} || {function,_,N,A,Cs} <- Forms]),
            [{Path,F,group(Cs)} || F <- Diffs, {ok,Cs} <- [maps:find(F,Index)]];
        _ -> []
    end.

group([Clause]) ->
    {single_clause, clause_kind(Clause)};
group([First | Rest]=Clauses) ->
    case shapes(First) of
        [] -> {multiple_clauses,other_entry};
        Shapes ->
            Shared=lists:max([length([ok || C <- Clauses, lists:member(S,shapes(C))]) || S <- Shapes]),
            Tagged = length([ok || C <- Clauses, shapes(C) =/= []]),
            case {Shared, Tagged, irrefutable(lists:last(Rest))} of
                {N,_,_} when N =:= length(Clauses) -> {multiple_clauses,shared_tag_all};
                {N,_,true} when N > 1 -> {multiple_clauses,shared_tag_and_default};
                {1,1,true} -> {multiple_clauses,one_tag_and_default};
                {1,_,true} -> {multiple_clauses,distinct_tags_and_default};
                {N,_,false} when N > 1 -> {multiple_clauses,shared_tag_and_other};
                {1,1,false} -> {multiple_clauses,one_tag_and_other};
                _ -> {multiple_clauses,distinct_tags_and_other}
            end
    end.

clause_kind(Clause) ->
    case sets:to_list(sets:from_list([type(Shape) || Shape <- shapes(Clause)])) of
        [] -> other_entry;
        [record] -> record_pattern;
        [tuple] -> tagged_tuple_pattern;
        _ -> record_and_tagged_tuple
    end.

type({_,{Kind,_,_}}) -> Kind.

shapes({clause,_,Patterns,_,_}) ->
    [{Index,Shape} || {Index,Pattern} <- lists:enumerate(Patterns), Shape <- tagged(Pattern)].

tagged({tuple,_,[{atom,_,Tag}|Tail]}) -> [{tuple,Tag,length(Tail)+1}];
tagged({record,_,Tag,_}) -> [{record,Tag,0}];
tagged({match,_,Left,Right}) -> tagged(Left) ++ tagged(Right);
tagged(_) -> [].

irrefutable({clause,_,Patterns,[],_}) ->
    Vars = [Name || {var,_,Name} <- Patterns],
    Distinct = [Name || Name <- Vars, Name =/= '_'],
    length(Vars) =:= length(Patterns) andalso length(Distinct) =:= length(lists:usort(Distinct));
irrefutable(_) -> false.

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
