-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = [{P, compare(filename:join(Official,P), filename:join(Generated,P))} || P <- Paths],
    Candidates = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    io:format("~B modules ~B functions with matching calls and Y projections before the call where OTP reads the fields after it.~n",
              [length(Candidates),lists:sum([length(Fs) || {_,Fs} <- Candidates])]),
    [io:format("~s ~0p~n",[P,Fs]) || {P,Fs} <- Candidates],
    ok.

compare(Reference,Generated) ->
    R=functions(Reference), G=functions(Generated),
    [{F,Matches} || {F,GC} <- maps:to_list(G),
                    {ok,RC} <- [maps:find(F,R)],
                    Matches <- [[{K,Before,After} || K <- lists:usort([Key || {Key,_,_} <- GC]),
                                        Go <- [[B || {Key,B,_} <- GC, Key =:= K]],
                                        Ro <- [[A || {Key,_,A} <- RC, Key =:= K]],
                                        length(Go) =:= length(Ro),
                                        {BG,RG} <- lists:zip(Go,Ro),
                                        {Before,After} <- related(BG,RG)]],
                    Matches =/= []].

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},calls(Code,[],[])} || {function,N,A,_,Code} <- Fs]).

calls([{Kind,Arity,MFA}|Rest],Prev,Acc) when Kind =:= call; Kind =:= call_ext ->
    Site={{Kind,Arity,MFA},fields(Prev,#{},before),fields(Rest,#{},after_call)},
    calls(Rest,[],[Site|Acc]);
calls([I|Rest],Prev,Acc) ->
    calls(Rest,[I|Prev],Acc);
calls([],_,Acc) ->
    lists:reverse(Acc).

related(BG,RG) ->
    [{Before,After} || {Src,Before} <- maps:to_list(BG), {_,After} <- maps:to_list(RG),
                       ordsets:is_subset(Before,After),
                       length(Before) >= 2 orelse (Before =/= [] andalso element(1,Src) =:= y)].

fields([{get_tuple_element,Source,Field,Dst}|Rest],Acc,Side) ->
    Fields = case {Side,Dst} of
        {before,{y,_}} -> add_field(Source,Field,Acc);
        {after_call,_} -> add_field(Source,Field,Acc);
        _ -> Acc
    end,
    fields(Rest,Fields,Side);
fields([I|Rest],Acc,Side) when is_tuple(I) ->
    Op=element(1,I),
    case lists:member(Op,[line,test_heap,move,swap,init_yregs,put_tuple2,put_list,get_list]) of
        true -> fields(Rest,Acc,Side);
        false -> Acc
    end;
fields(_,Acc,_) -> Acc.

add_field(Source,Field,Acc) ->
    maps:update_with(Source,fun(Fs) -> ordsets:add_element(Field,Fs) end,[Field],Acc).