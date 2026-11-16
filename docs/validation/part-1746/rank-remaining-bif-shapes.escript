-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Maps = [module_rows(Official,Generated,P) || P <- Paths],
    Counts = lists:foldl(fun(Rows,Acc0) ->
        maps:fold(fun(Key,Count,Acc) ->
            maps:update_with(Key,fun({M,F})->{M+1,F+Count} end,{1,Count},Acc)
        end,Acc0,Rows)
    end,#{},Maps),
    Sorted = lists:reverse(lists:sort([{M,F,K} || {K,{M,F}} <- maps:to_list(Counts)])),
    [io:format("~B modules, ~B functions: ~0p~n",[M,F,K]) || {M,F,K} <- Sorted],
    ok.

module_rows(Official,Generated,P) ->
    Reference = read(filename:join(Official,P)),
    Candidate = read(filename:join(Generated,P)),
    Shapes = [shape(Entry,Between,Dst) ||
        {F,Code} <- maps:to_list(Candidate),
        {ok,Ref} <- [maps:find(F,Reference)],
        {Name,_RefBefore,_RefDst} <- [first_bif(Ref)],
        {CandidateName,Before,Dst} <- [first_bif(Code)], CandidateName =:= Name,
        not lists:any(fun is_frame/1,_RefBefore),
        lists:any(fun is_frame/1,Ref),
        {Entry,[Frame|Between]} <- [lists:splitwith(fun(I)->not is_frame(I) end,Before)],
        is_frame(Frame)],
    lists:foldl(fun(S,Acc)->maps:update_with(S,fun(N)->N+1 end,1,Acc) end,#{},Shapes).

shape(Entry,Between,Dst) ->
    Frames = lists:any(fun(I)->lists:member(op(I),[call,call_ext,call_fun,call_fun2,apply]) end,Entry),
    FirstWork = lists:dropwhile(fun frame_setup/1,Between),
    case {Frames,FirstWork} of
        {true,_} -> call_before_frame;
        {false,[I|_]} -> {preceding,op(I)};
        {false,[]} -> {destination,op(Dst)}
    end.

frame_setup({move,{x,_},{y,_}}) -> true;
frame_setup({init_yregs,_}) -> true;
frame_setup({line,_}) -> true;
frame_setup(_) -> false.

first_bif(Code) -> first_bif(Code,[]).
first_bif([{gc_bif,Name,{f,0},_,_,Dst}|_],Before) -> {Name,lists:reverse(Before),reg(Dst)};
first_bif([{bif,Name,Fail,_,Dst}|_],Before) when Fail =:= {f,0}; Fail =:= nofail -> {Name,lists:reverse(Before),reg(Dst)};
first_bif([I|T],Before) -> first_bif(T,[I|Before]);
first_bif([],_) -> none.

read(File) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Is} || {function,Name,Arity,_,Is} <- Functions]).

reg({tr,R,_}) -> R;
reg(R) -> R.
is_frame(I) -> lists:member(op(I),[allocate,allocate_heap]).
op(T) when is_tuple(T) -> element(1,T);
op(A) -> A.
