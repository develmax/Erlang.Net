-mode(compile).
main([Official, Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = lists:append([rows(Official, Generated, P) || P <- Paths]),
    Counts = lists:foldl(fun({P,_,_,Shape}, Acc) ->
        maps:update_with(Shape, fun({Files,N}) -> {Files#{P=>true},N+1} end, {#{P=>true},1}, Acc)
    end, #{}, Rows),
    io:format("One filter after a relaxed variable or tuple generator over a variable, with a missing OTP list helper.~n"),
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
            [{P,F,A,filter_shape(LC, Forms)} || {function,_,F,A,Cs} <- Forms,
             ordsets:is_element(<<"-", (atom_to_binary(F))/binary, "/", (integer_to_binary(A))/binary>>,Parents),
             [LC] <- [list_comprehensions(Cs)], is_filter_shape(shape(LC))];
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

is_filter_shape({list, Pat, var, _, 1, true}) -> Pat =:= var orelse Pat =:= tuple;
is_filter_shape(_) -> false.

filter_shape({lc,_,_,[Pattern,Condition]}, Forms) ->
    Local = [{N,A} || {function,_,N,A,_} <- Forms],
    Imported = lists:append([Names || {attribute,_,import,{_,Names}} <- Forms]),
    Overridden = fun(FA) -> lists:member(FA,Local) orelse lists:member(FA,Imported) end,
    Kind = case erl_lint:is_guard_test(Condition, Forms, Overridden) of
        true -> guard;
        false -> boolean_expression
    end,
    {generate,_,Head,_}=Pattern,
    {Kind,kind(Head),operator(Condition)}.

operator({op,_,Name,_}) -> {op,Name};
operator({op,_,Name,_,_}) -> {op,Name};
operator({call,_,{atom,_,Name},Args}) -> {local,Name,length(Args)};
operator({call,_,{remote,_,{atom,_,Module},{atom,_,Name}},Args}) -> {Module,Name,length(Args)};
operator(Condition) -> kind(Condition).