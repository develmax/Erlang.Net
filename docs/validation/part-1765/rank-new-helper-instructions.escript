-mode(compile).
main([Official, Before, After, PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Rows = lists:append([compare(Official,Before,After,P) || P <- Paths]),
    Counts = lists:foldl(fun({P,_,Shape},Acc) ->
        maps:update_with(Shape,fun({Files,N})->{Files#{P=>true},N+1} end,{#{P=>true},1},Acc)
    end,#{},Rows),
    io:format("First normalized differences in new matching-name list helpers.~n"),
    [io:format("~B modules ~B helpers ~0p~n",[M,N,S]) ||
        {M,N,S}<-lists:reverse(lists:sort([{map_size(Fs),N,S} || {S,{Fs,N}}<-maps:to_list(Counts)]))],
    [io:format("~s ~0p ~0p~n",[P,F,S]) || {P,F,S} <- Rows].

compare(Official, Before, After, P) ->
    R=read(Official,Official,P),
    O=read(Before,Official,P),
    N=read(After,Official,P),
    [{P,F,mismatch(R0,N0)} || {Name,_}=F<-maps:keys(R), is_helper(Name), not maps:is_key(F,O),
     {ok,Code}<-[maps:find(F,N)], R0<-[normalize(maps:get(F,R))], N0<-[normalize(Code)], R0=/=N0].

mismatch([I|R],[I|A]) -> mismatch(R,A);
mismatch([{line,_}|R],[{line,_}|A]) -> mismatch(R,A);
mismatch([R|_],[A|_]) -> {opcode(R),opcode(A)};
mismatch([],[]) -> source_line;
mismatch([],[_|_]) -> extra;
mismatch([_|_],[]) -> shortened.

opcode({test,Name,_,_}) -> Name;
opcode(I) when is_tuple(I) -> element(1,I);
opcode(I) -> I.

is_helper(Name) ->
    re:run(atom_to_binary(Name), "^-.*-lc[$].[0-9]+/[0-9]+-[0-9]+-$", [{capture,none}]) =:= match.

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
