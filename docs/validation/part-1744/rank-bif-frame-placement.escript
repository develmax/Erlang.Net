-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Rows = [{P,compare(filename:join(Official,P),filename:join(Generated,P))} || P <- Paths],
    Matches = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    io:format("~B modules, ~B functions with a BIF before the first frame in OTP and after the first frame in C#.~n",
        [length(Matches),lists:sum([length(Fs) || {_,Fs} <- Matches])]),
    [io:format("~s ~0p~n",[P,Fs]) || {P,Fs} <- Matches],
    ok.

compare(Official,Generated) ->
    Reference = read(Official),
    Candidate = read(Generated),
    [{F,Name} || {F,Code} <- maps:to_list(Candidate),
                 {ok,Ref} <- [maps:find(F,Reference)],
                 {before_frame,Name} <- [first_eligible_bif(Ref)],
                 {after_frame,Actual} <- [first_eligible_bif(Code)], Actual =:= Name].

read(File) ->
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Is} || {function,Name,Arity,_,Is} <- Fs]).

first_eligible_bif(Code) ->
    Ops = [{op(I),I} || I <- Code],
    case lists:dropwhile(fun({Op,_}) -> not lists:member(Op,[allocate,allocate_heap]) end, Ops) of
        [] -> none;
        _ -> first_eligible_bif(Ops,before_frame)
    end.
first_eligible_bif([{Op,_}|Tail],_) when Op =:= allocate; Op =:= allocate_heap ->
    first_eligible_bif(Tail,after_frame);
first_eligible_bif([{gc_bif,{gc_bif,Name,{f,0},_,_,_}}|_],Position) -> {Position,Name};
first_eligible_bif([{bif,{bif,Name,{f,0},_,_}}|_],Position) -> {Position,Name};
first_eligible_bif([{Op,_}|Tail],Position) ->
    case lists:member(Op,[label,line,func_info,test,init_yregs,move,get_tuple_element,get_list,get_hd,get_tl]) of
        true -> first_eligible_bif(Tail,Position);
        false -> none
    end;
first_eligible_bif([],_) -> none.

op(T) when is_tuple(T) -> element(1,T);
op(A) -> A.

