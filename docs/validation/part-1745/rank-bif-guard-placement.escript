-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Rows = [{P,compare(filename:join(Official,P),filename:join(Generated,P))} || P <- Paths],
    Matches = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    io:format("~B modules, ~B functions with an entry BIF before allocation in both images and an intervening OTP predicate.~n",
        [length(Matches),lists:sum([length(Fs) || {_,Fs} <- Matches])]),
    [io:format("~s ~0p~n",[P,Fs]) || {P,Fs} <- Matches],
    ok.

compare(Official,Generated) ->
    Reference = read(Official),
    Candidate = read(Generated),
    [{F,Name} || {F,Code} <- maps:to_list(Candidate), 
                 {ok,Ref} <- [maps:find(F,Reference)],
                 {Name,true} <- [pre_frame_predicate(Ref)],
                 {CandidateName,false} <- [pre_frame_predicate(Code)], CandidateName =:= Name].

read(File) ->
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Is} || {function,Name,Arity,_,Is} <- Fs]).

pre_frame_predicate(Code) ->
    {Prefix, Rest} = lists:splitwith(fun(I)->not lists:member(op(I),[allocate,allocate_heap]) end,Code),
    {BeforeBif, AfterBif} = lists:splitwith(fun(I)->not lists:member(op(I),[bif,gc_bif]) end,Prefix),
    case {AfterBif,Rest,lists:any(fun is_call/1,BeforeBif)} of
        {[Bif|Tail],[_|_],false} ->
            {element(2,Bif),lists:any(fun(I)->lists:member(op(I),[test,select_val,select_tuple_arity]) end,Tail)};
        _ -> none
    end.

is_call(I) -> lists:member(op(I),[call,call_ext,call_fun,call_fun2,apply]).
op(T) when is_tuple(T) -> element(1,T);
op(A) -> A.
