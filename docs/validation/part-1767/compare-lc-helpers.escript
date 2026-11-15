-mode(compile).
main([Official, Before, After, PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Rows = [{P,changes(Official,Before,After,P)} || P <- Paths],
    Count = [D || {_,D} <- Rows, D =/= {0,0,0,0}],
    [G,L,Code,CodeLost] = lists:foldl(fun({A,B,C,D},[S,T,U,V])->[S+A,T+B,U+C,V+D] end,[0,0,0,0],Count),
    io:format("~B new OTP list comprehension helper identities and ~B lost, across ~B changed modules with helper differences.~n",
              [G,L,length(Count)]),
    io:format("Normalized helper instructions gained ~B matches and lost ~B.~n",[Code,CodeLost]),
    [io:format("~s ~0p~n",[P,D]) || {P,D} <- Rows, D =/= {0,0,0,0}].

changes(Official,Before,After,P) ->
    Ref = read(Official,Official,P),
    Old = read(Before,Official,P),
    New = read(After,Official,P),
    Helper = [F || {N,_}=F <- maps:keys(Ref), is_helper(N)],
    Gains = length([F || F <- Helper, maps:is_key(F,New), not maps:is_key(F,Old)]),
    Losses = length([F || F <- Helper, maps:is_key(F,Old), not maps:is_key(F,New)]),
    CodeGains = length([F || F <- Helper, instruction_equal(F,Ref,New), not instruction_equal(F,Ref,Old)]),
    CodeLosses = length([F || F <- Helper, instruction_equal(F,Ref,Old), not instruction_equal(F,Ref,New)]),
    {Gains,Losses,CodeGains,CodeLosses}.

is_helper(Name) ->
    re:run(atom_to_binary(Name), "^-.*-lc[$].[0-9]+/[0-9]+-[0-9]+-$", [{capture,none}]) =:= match.

instruction_equal(F,R,G) ->
    case maps:find(F,G) of
        {ok,I} -> normalize(I) =:= normalize(maps:get(F,R));
        error -> false
    end.

read(Dir,Root,P) ->
    Candidate = filename:join(Dir,P),
    File = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Code} || {function,Name,Arity,_,Code} <- Functions]).

normalize(Code) ->
    Labels = [L || {label,L} <- Code],
    Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
    [fix(I,Map) || I <- Code].

fix({label,L},Map) -> {label,maps:get(L,Map,L)};
fix({f,L},Map) -> {f,maps:get(L,Map,L)};
fix({line,[{location,_,N}]},_) -> {line,N};
fix({literal,_}=I,_) -> I;
fix({make_fun3,M,I,_,Dst,Args},Ls) -> {make_fun3,M,I,uniq,fix(Dst,Ls),fix(Args,Ls)};
fix(T,Map) when is_tuple(T) -> list_to_tuple([fix(I,Map)||I<-tuple_to_list(T)]);
fix([H|T],Map) -> [fix(H,Map)|fix(T,Map)];
fix(I,_) -> I.
