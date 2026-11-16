-mode(compile).

main([Official, Before, After, PathsFile]) ->
    {ok, Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data, <<10>>, [global]), P =/= <<>>],
    Rows = lists:append([compare(Official, Before, After, P) || P <- Paths]),
    Gap = lists:sum([abs(R-O)-abs(R-N) || {_,_,R,O,N} <- Rows]),
    Gains = [Row || {_,_,R,O,N}=Row <- Rows, R =:= N, R =/= O],
    Losses = [Row || {_,_,R,O,N}=Row <- Rows, R =:= O, R =/= N],
    io:format("~B frame size changes, ~B new OTP matches, ~B lost matches, ~B fewer slots of absolute difference.~n",
              [length(Rows),length(Gains),length(Losses),Gap]),
    [io:format("~s ~0p OTP ~B, previous ~B, current ~B.~n", [P,F,R,O,N]) || {P,F,R,O,N} <- Rows],
    ok.

compare(Official, Before, After, P) ->
    Reference = frames(Official, Official, P),
    Old = frames(Before, Official, P),
    New = frames(After, Official, P),
    [{P,Key,R,O,N} || {Key,N} <- maps:to_list(New),
        {ok,R} <- [maps:find(Key,Reference)], {ok,O} <- [maps:find(Key,Old)], O =/= N].

frames(Dir, Root, P) ->
    Candidate = filename:join(Dir,P),
    File = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Slots} || {function,Name,Arity,_,Code} <- Functions,
                                           [Slots] <- [[S || I <- Code, S <- allocation(I)]]]).

allocation({allocate,S,_}) -> [S];
allocation({allocate_heap,S,_,_}) -> [S];
allocation(_) -> [].
