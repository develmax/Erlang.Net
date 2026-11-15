-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Rows = [{P,compare(filename:join(Official,P),filename:join(Generated,P))} || P <- Paths],
    Matches = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    io:format("~B modules, ~B functions with X-to-Y BIF result copies and direct Y destinations for the same BIF in OTP.~n",
        [length(Matches),lists:sum([length(Fs) || {_,Fs} <- Matches])]),
    [io:format("~s ~0p~n",[P,Fs]) || {P,Fs} <- Matches],
    ok.

compare(Official,Generated) ->
    Reference = read(Official),
    Candidate = read(Generated),
    [F || {F,Code} <- maps:to_list(Candidate),
          {ok,Ref} <- [maps:find(F,Reference)],
          not ordsets:is_disjoint(copied_bifs(Code),direct_bifs(Ref))].

read(File) ->
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Is} || {function,Name,Arity,_,Is} <- Fs]).

copied_bifs(Code) ->
    ordsets:from_list(copied_bifs([I || I <- Code, op(I) =/= line],[])).
copied_bifs([I,{move,{x,_}=X,{y,_}}|Tail],Acc) ->
    case bif(I) of
        {Name,Arity,X} -> copied_bifs(Tail,[{Name,Arity}|Acc]);
        _ -> copied_bifs(Tail,Acc)
    end;
copied_bifs([_|Tail],Acc) -> copied_bifs(Tail,Acc);
copied_bifs([],Acc) -> Acc.

direct_bifs(Code) ->
    ordsets:from_list([{Name,Arity} || I <- Code, {Name,Arity,{y,_}} <- [bif(I)]]).

bif({gc_bif,Name,_,_,Args,Destination}) -> {Name,length(Args),Destination};
bif({bif,Name,_,Args,Destination}) -> {Name,length(Args),Destination};
bif(_) -> none.

op(T) when is_tuple(T) -> element(1,T);
op(A) -> A.
