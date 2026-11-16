-mode(compile).

main([Root, Before, After, PathList]) ->
    {ok,Raw} = file:read_file(PathList),
    Paths = [binary_to_list(P) || P <- binary:split(Raw,<<10>>,[global]), P =/= <<>>],
    Rows = [{P, read(Root,Root,P), read(Before,Root,P), read(After,Root,P)} || P <- Paths],
    Changes = [{P, R, B, A} || {P, R, B, A} <- Rows, B =/= A],
    Cuts = [OldCount - NewCount || {_,_,{OldCount,_,_},{NewCount,_,_}} <- Changes],
    SamePositions = length([ok || {_,_,{_,Tail,_},{_,Tail,_}} <- Changes]),
    true = SamePositions =:= length(Changes),
    true = lists:all(fun(N) -> N > 0 end, Cuts),
    true = lists:all(fun({_,_,{Old,_,Old},{New,_,New}}) -> true; (_) -> false end, Changes),
    Gains = [P || {P,R,B,R} <- Changes, R =/= B],
    Losses = [P || {P,R,R,A} <- Changes, R =/= A],
    io:format("~B line instructions removed in ~B images. All changed counters match their emitted code and position tables retain their entries.~n",
              [lists:sum(Cuts),length(Changes)]),
    io:format("~B Line chunks gain exact equality with OTP and ~B lose it.~n",
              [length(Gains),length(Losses)]),
    [io:format("Equal Line ~s~n",[P]) || P <- Gains],
    [io:format("Different Line ~s~n",[P]) || P <- Losses].

read(Directory, Root, Relative) ->
    Choice = filename:join(Directory,Relative),
    Path = case filelib:is_regular(Choice) of true -> Choice; false -> filename:join(Root,Relative) end,
    {ok,{Module,[{"Line",Line}]}} = beam_lib:chunks(Path,["Line"]),
    <<Version:32,Flags:32,Encoded:32,Positions/binary>> = Line,
    {beam_file,Module,_,_,_,Fs} = beam_disasm:file(Path),
    Actual = length([L || {function,_,_,_,Is} <- Fs, {line,L} <- Is]),
    {Encoded,{Version,Flags,Positions},Actual}.
