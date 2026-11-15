-mode(compile).

main([Official,Before,After,PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Rows = [changes(Official,Before,After,P) || P <- Paths],
    {Direct,Moves,Gains,Losses} = lists:foldl(fun sum/2,{0,0,0,0},Rows),
    io:format("~B more direct Y BIF destinations and ~B fewer X-to-Y moves across ~B changed images.~n",
              [Direct,Moves,length(Paths)]),
    io:format("Within functions with the same ordered BIF signatures, ~B destination kinds match OTP after the change and ~B cease to match.~n",
              [Gains,Losses]),
    [io:format("~s ~0p~n",[P,Counts]) || {P,Counts} <- lists:zip(Paths,Rows)],
    ok.

changes(Official,Before,After,P) ->
    Ref = read(Official,Official,P),
    Old = read(Before,Official,P),
    New = read(After,Official,P),
    {Gains,Losses} = alignment(Ref,Old,New),
    {direct(New)-direct(Old),moves(Old)-moves(New),Gains,Losses}.

read(Dir,Root,P) ->
    Candidate = filename:join(Dir,P),
    File = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Code} || {function,Name,Arity,_,Code} <- Functions]).

direct(Functions) ->
    length([ok || Code <- maps:values(Functions), I <- Code, {_,y} <- [bif(I)]]).
moves(Functions) ->
    length([ok || Code <- maps:values(Functions), {move,{x,_},{y,_}} <- Code]).

alignment(Ref,Old,New) ->
    Matches = [compare(B,A,R) || {F,Code} <- maps:to_list(New),
                                {ok,OldCode} <- [maps:find(F,Old)],
                                {ok,RefCode} <- [maps:find(F,Ref)],
                                {Ops,A} <- [signatures(Code)],
                                {OldOps,B} <- [signatures(OldCode)], OldOps =:= Ops,
                                {RefOps,R} <- [signatures(RefCode)], RefOps =:= Ops],
    lists:foldl(fun({G,L},{AG,AL})->{AG+G,AL+L} end,{0,0},Matches).

compare(Before,After,Ref) ->
    Zipped = lists:zip3(Before,After,Ref),
    {length([ok || {B,A,R} <- Zipped,B =/= R,A =:= R]),
     length([ok || {B,A,R} <- Zipped,B =:= R,A =/= R])}.

signatures(Code) ->
    lists:unzip([Bif || I <- Code, Bif <- [bif(I)], Bif =/= none]).

bif({gc_bif,Name,_,_,Args,Destination}) -> {{gc_bif,Name,length(Args)},kind(Destination)};
bif({bif,Name,_,Args,Destination}) -> {{bif,Name,length(Args)},kind(Destination)};
bif(_) -> none.
kind({tr,Operand,_}) -> kind(Operand);
kind({K,_}) -> K.

sum({D0,M0,G0,L0},{D1,M1,G1,L1}) -> {D0+D1,M0+M1,G0+G1,L0+L1}.
