-mode(compile).
main([Official,Before,After,PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Rows = lists:append([changes(Official,Before,After,P) || P <- Paths]),
    [io:format("~s ~0p ~B instructions ~0p -> ~0p, OTP ~0p~n", [P,F,N,O,A,R])
        || {N,P,F,O,A,R} <- lists:sort(Rows)].

changes(Root,Before,After,P) ->
    Ref = read(Root,Root,P),
    Old = read(Before,Root,P),
    New = read(After,Root,P),
    [{length(N),P,F,frame(O),frame(N),frame(R)}
        || {F,N} <- maps:to_list(New), {ok,O} <- [maps:find(F,Old)], {ok,R} <- [maps:find(F,Ref)],
           frame(O) =/= frame(N), lists:keymember(trim,1,frame(O))].

read(Dir,Root,P) ->
    Candidate = filename:join(Dir,P),
    File = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Code} || {function,Name,Arity,_,Code} <- Functions]).

frame(Code) -> [I || I <- Code, is_tuple(I), lists:member(element(1,I),[allocate,allocate_heap,trim,deallocate])].
