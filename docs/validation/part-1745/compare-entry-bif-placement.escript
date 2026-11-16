-mode(compile).

main([Official,Before,After,PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Rows = [{P,changes(Official,Before,After,P)} || P <- Paths],
    Gains = lists:sum([G || {_,{G,_,_}} <- Rows]),
    Losses = lists:sum([L || {_,{_,L,_}} <- Rows]),
    Compared = lists:sum([N || {_,{_,_,N}} <- Rows]),
    io:format("~B BIF placement comparisons across ~B changed images, with ~B new order matches and ~B lost matches.~n",
              [Compared,length(Paths),Gains,Losses]),
    [io:format("~s ~0p~n",[P,Delta]) || {P,Delta} <- Rows],
    ok.

changes(Official,Before,After,P) ->
    Ref = read(Official,Official,P),
    Old = read(Before,Official,P),
    New = read(After,Official,P),
    Matched = [{R,O,N} || {F,Code} <- maps:to_list(New),
                         {ok,OldCode} <- [maps:find(F,Old)],
                         {ok,RefCode} <- [maps:find(F,Ref)],
                         {Sig,N} <- [position(Code)],
                         {OldSig,O} <- [position(OldCode)], OldSig =:= Sig,
                         {RefSig,R} <- [position(RefCode)], RefSig =:= Sig,
                         O =/= N],
    {length([ok || {R,O,N} <- Matched,R =/= O,R =:= N]),
     length([ok || {R,O,N} <- Matched,R =:= O,R =/= N]),
     length(Matched)}.

read(Dir,Root,P) ->
    Candidate = filename:join(Dir,P),
    File = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Code} || {function,Name,Arity,_,Code} <- Functions]).

position(Code) ->
    case lists:any(fun is_frame/1,Code) of
        true -> position(Code,before_frame);
        false -> none
    end.
position([I|Tail],Position) ->
    case I of
        {gc_bif,Name,{f,0},_,Args,_} -> {{gc_bif,Name,length(Args)},Position};
        {bif,Name,Fail,Args,_} when Fail =:= {f,0}; Fail =:= nofail ->
            {{bif,Name,length(Args)},Position};
        _ -> position(Tail,case is_frame(I) of true -> after_frame; false -> Position end)
    end;
position([],_) -> none.

is_frame({allocate,_,_}) -> true;
is_frame({allocate_heap,_,_,_}) -> true;
is_frame(_) -> false.
