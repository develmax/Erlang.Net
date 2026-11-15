-mode(compile).
main([Official, Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = lists:append([rows(Official, Generated, P) || P <- Paths]),
    Counts = lists:foldl(fun({P,_,_,Shape}, Acc) ->
        maps:update_with(Shape, fun({Files,N}) -> {Files#{P=>true},N+1} end, {#{P=>true},1}, Acc)
    end, #{}, Rows),
    io:format("Verified input shapes with a missing OTP list helper, counted once per function and surrounding construct.~n"),
    [io:format("~B modules ~B source functions ~0p~n", [M,N,S]) ||
        {M,N,S} <- lists:reverse(lists:sort([{map_size(P),N,S} || {S,{P,N}} <- maps:to_list(Counts)]))],
    [io:format("~s ~p/~B ~0p~n", [P,F,A,S]) || {P,F,A,S} <- Rows].

rows(Official, Generated, P) ->
    R = filename:join(Official,P),
    Ref = names(R),
    Actual = names(filename:join(Generated,P)),
    Parents = ordsets:from_list([Parent ||
        {Name,_} <- ordsets:subtract(Ref,Actual), {true,Parent} <- [list_helper_parent(Name)]]),
    case beam_lib:chunks(R, [abstract_code]) of
        {ok,{_,[{abstract_code,{raw_abstract_v1,Forms}}]}} ->
            [{P,F,A,Context} || {function,_,F,A,Cs} <- Forms,
             ordsets:is_element(<<"-", (atom_to_binary(F))/binary, "/", (integer_to_binary(A))/binary>>,Parents),
             LCs <- [list_comprehensions(Cs)], LCs =/= [], lists:all(fun(L) -> supported_shape(shape(L)) end, LCs),
             Context <- contexts(Cs, LCs)];
        _ -> []
    end.

list_helper_parent(Name) ->
    case re:run(atom_to_binary(Name), "(^-.+/[0-9]+)-lc[$].[0-9]+/[0-9]+-[0-9]+-$", [{capture,[1],binary}]) of
        {match,[Parent]} -> {true,Parent};
        _ -> false
    end.

list_comprehensions({lc,_,E,Qs}=LC) -> [LC|list_comprehensions([E|Qs])];
list_comprehensions(T) when is_tuple(T) -> list_comprehensions(tuple_to_list(T));
list_comprehensions([H|T]) -> list_comprehensions(H) ++ list_comprehensions(T);
list_comprehensions(_) -> [].

shape({lc,_,E,[G|Fs]}) ->
    case G of
        {generate,_,P,Source} -> {list, kind(P), kind(Source), kind(E), length(Fs), lists:all(fun is_filter/1,Fs)};
        {generate_strict,_,P,Source} -> {strict_list, kind(P), kind(Source), kind(E), length(Fs), lists:all(fun is_filter/1,Fs)};
        _ -> {other, kind(G)}
    end;
shape(_) -> no_qualifier.

kind(T) when is_tuple(T), tuple_size(T) > 0 -> element(1,T);
kind(_) -> other.

is_filter(T) -> not lists:member(kind(T),[generate,generate_strict,b_generate,b_generate_strict,m_generate,m_generate_strict,zip]).

names(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    ordsets:from_list([{N,A} || {function,N,A,_,_} <- Fs]).

supported_shape({list,P,S,_,N,true}) ->
    (P =:= var orelse P =:= tuple) andalso (S =:= var orelse S =:= call) andalso N =< 1;
supported_shape(_) -> false.

contains_fun({'fun',_,{clauses,_}}) -> true;
contains_fun({named_fun,_,_,_}) -> true;
contains_fun(T) when is_tuple(T) -> contains_fun(tuple_to_list(T));
contains_fun([H|T]) -> contains_fun(H) orelse contains_fun(T);
contains_fun(_) -> false.

contexts(Clauses, LCs) ->
    Nested = lists:any(fun(LC) -> length(list_comprehensions(LC)) > 1 end, LCs),
    Ancestors = lists:append([paths(Clause, []) || Clause <- Clauses]),
    Flattened = lists:append(Ancestors),
    Kinds =
        [{comprehension_in_fun, N} || N <- [length(LCs)], lists:member(anonymous_fun, Flattened)] ++
        [{fun_in_comprehension, N} || N <- [length(LCs)], lists:any(fun contains_fun/1, LCs)] ++
        [{independent_fun, N} || N <- [length(LCs)], contains_fun(Clauses),
            not lists:member(anonymous_fun, Flattened), not lists:any(fun contains_fun/1, LCs)] ++
        [{generator_contains_comprehension, N} || N <- [length(LCs)], Nested] ++
        [Kind || Kind <- [list_append, map, record, 'try', 'receive', 'maybe', short_circuit],
            lists:member(Kind, Flattened)],
    case Kinds of
        [] -> [{other, length(LCs)}];
        _ -> lists:usort(Kinds)
    end.

paths({lc,_,E,Qs}, Anc) -> [Anc|paths([E|Qs], Anc)];
paths({'fun',_,{clauses,Cs}}, Anc) -> paths(Cs, [anonymous_fun|Anc]);
paths({named_fun,_,_,Cs}, Anc) -> paths(Cs, [anonymous_fun|Anc]);
paths({op,_,'++',{lc,_,_,_}=Left,Right}, Anc) ->
    paths(Left, [list_append|Anc]) ++ paths(Right, [list_append|Anc]);
paths({op,_,Op,Left,Right}, Anc) when Op =:= 'andalso'; Op =:= 'orelse' ->
    paths([Left,Right], [short_circuit|Anc]);
paths(T, Anc) when is_tuple(T) ->
    Kind = kind(T),
    More = case lists:member(Kind, [map,record,'try','receive','maybe']) of
               true -> [Kind|Anc];
               false -> Anc
           end,
    paths(tuple_to_list(T), More);
paths([H|T], Anc) -> paths(H, Anc) ++ paths(T, Anc);
paths(_, _) -> [].
