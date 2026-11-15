-mode(compile).

main([Official,Before,After,PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Rows = [{P,changes(Official,Before,After,P)} || P <- Paths],
    Gains = lists:sum([G || {_,{G,_,_}} <- Rows]),
    Losses = lists:sum([L || {_,{_,L,_}} <- Rows]),
    Compared = lists:sum([N || {_,{_,_,N}} <- Rows]),
    io:format("~B entry guard placement comparisons across ~B changed images, with ~B new order matches and ~B lost matches.~n",
              [Compared,length(Paths),Gains,Losses]),
    FrameCounts = [frame_alignment(Official,Before,After,P) || P <- Paths],
    io:format("Stack allocation counts gain ~B matches and lose ~B across corresponding functions.~n",
              [lists:sum([G || {G,_} <- FrameCounts]),lists:sum([L || {_,L} <- FrameCounts])]),
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

frame_alignment(Official,Before,After,P) ->
    Ref = read(Official,Official,P),
    Old = read(Before,Official,P),
    New = read(After,Official,P),
    Matched = [{frame_count(R),frame_count(O),frame_count(N)} ||
                   {F,N} <- maps:to_list(New),{ok,O} <- [maps:find(F,Old)],{ok,R} <- [maps:find(F,Ref)]],
    {length([ok || {R,O,N} <- Matched,R =/= O,R =:= N]),
     length([ok || {R,O,N} <- Matched,R =:= O,R =/= N])}.

frame_count(Code) ->
    length([I || I <- Code, lists:member(op(I),[allocate,allocate_heap])]).

read(Dir,Root,P) ->
    Candidate = filename:join(Dir,P),
    File = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Code} || {function,Name,Arity,_,Code} <- Functions]).

position(Code) -> position(Code,none,[]).

position([{allocate,_,_}|Tail],none,Tests) ->
    position(Tail,length(Tests),Tests);
position([{allocate_heap,_,_,_}|Tail],none,Tests) ->
    position(Tail,length(Tests),Tests);
position([{test,Name,_,_}|Tail],Frame,Tests) -> position(Tail,Frame,[Name|Tests]);
position([{bif,Name,_,Args,_}|Tail],Frame,Tests) ->
    boundary({bif,Name,length(Args),Tests},Frame,Tail);
position([{gc_bif,Name,_,_,Args,_}|Tail],Frame,Tests) ->
    boundary({gc_bif,Name,length(Args),Tests},Frame,Tail);
position([{call,Arity,_}|_],Frame,Tests) when Frame =/= none ->
    {{call,Arity,Tests},Frame};
position([{call_ext,Arity,_}|_],Frame,Tests) when Frame =/= none ->
    {{call_ext,Arity,Tests},Frame};
position([I|Tail],Frame,Tests) ->
    case lists:member(op(I),[func_info,line,label,move,init_yregs,get_tuple_element,get_list,get_hd,get_tl]) of
        true -> position(Tail,Frame,Tests);
        false -> none
    end;
position([],_,_) -> none.

boundary(Sig,none,Tail) ->
    case lists:any(fun(I)->lists:member(op(I),[allocate,allocate_heap]) end,Tail) of
        true -> {Sig,after_bif};
        false -> none
    end;
boundary(Sig,Frame,_) -> {Sig,Frame}.

op(T) when is_tuple(T) -> element(1,T);
op(A) -> A.


