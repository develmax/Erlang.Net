-mode(compile).
main([Official, Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = lists:append([rows(Official, Generated, P) || P <- Paths]),
    Counts = lists:foldl(fun({P,_,_,Shape}, Acc) ->
        maps:update_with(Shape, fun({Files,N}) -> {Files#{P=>true},N+1} end, {#{P=>true},1}, Acc)
    end, #{}, Rows),
    io:format("Missing release list comprehension helpers in functions with exactly one source LC.~n"),
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
            [{P,F,A,shape(LC)} || {function,_,F,A,Cs} <- Forms,
             ordsets:is_element(<<"-", (atom_to_binary(F))/binary, "/", (integer_to_binary(A))/binary>>,Parents),
             [LC] <- [list_comprehensions(Cs)]];
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
