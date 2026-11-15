-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Results = [{P,compare(filename:join(Official,P),filename:join(Generated,P))} || P <- Paths],
    Counts = lists:foldl(fun({P,Rows},Acc0) ->
        lists:foldl(fun({F,_,Shape},Acc) ->
            maps:update_with(Shape,fun({Ms,Fs,N}) -> {Ms#{P=>true},Fs#{{P,F}=>true},N+1} end,
                {#{P=>true},#{{P,F}=>true},1},Acc)
        end,Acc0,Rows)
    end,#{},Results),
    Ranked = lists:reverse(lists:sort([{map_size(Ms),map_size(Fs),N,S} || {S,{Ms,Fs,N}} <- maps:to_list(Counts)])),
    io:format("Remaining projection candidates ranked by explicit uses before the next textual write to the slot. Matching field indices are candidates for proof.~n"),
    [io:format("~B modules ~B functions ~B sites ~0p~n",[M,F,N,S]) || {M,F,N,S} <- Ranked],
    ok.

compare(Official,Generated) ->
    R=functions(Official), G=functions(Generated),
    lists:usort([{F,I,shape(Code,I,Call,Dst)} || {F,{Code,GCalls}} <- maps:to_list(G),
                {ok,{_,RCalls}} <- [maps:find(F,R)],
                Key <- lists:usort([K || {K,_,_,_} <- GCalls]),
                GSites <- [[{C,B} || {K,C,B,_} <- GCalls, K=:=Key]],
                RSites <- [[A || {K,_,_,A} <- RCalls, K=:=Key]],
                length(GSites) =:= length(RSites),
                {{Call,Before},After} <- lists:zip(GSites,RSites),
                {Source,Index,I,Dst} <- Before,
                lists:any(fun({_,Field,_,_}) -> Field =:= Index end,After),
                element(1,Source) =:= y orelse
                    length([ok || {S,_,_,_} <- Before, S=:=Source]) >= 2]).

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([begin
        Indexed=lists:zip(lists:seq(1,length(Code)),Code),
        {{F,A},{Indexed,calls(Indexed,[],[])}}
    end || {function,F,A,_,Code} <- Fs]).

calls([{Index,{Kind,Arity,MFA}}|Rest],Prev,Acc) when Kind =:= call; Kind =:= call_ext ->
    Site={{Kind,Arity,MFA},Index,fields(Prev,[],before),fields(Rest,[],after_call)},
    calls(Rest,[],[Site|Acc]);
calls([I|Rest],Prev,Acc) -> calls(Rest,[I|Prev],Acc);
calls([],_,Acc) -> lists:reverse(Acc).

fields([{I,{get_tuple_element,Source,Field,Dst}}|Rest],Acc,Side) ->
    Selected = case {Side,Dst} of
        {before,{y,_}} -> [{Source,Field,I,Dst}|Acc];
        {after_call,_} -> [{Source,Field,I,Dst}|Acc];
        _ -> Acc
    end,
    fields(Rest,Selected,Side);
fields([{_,I}|Rest],Acc,Side) when is_tuple(I) ->
    case lists:member(element(1,I),[line,test_heap,move,swap,init_yregs,put_tuple2,put_list,get_list]) of
        true -> fields(Rest,Acc,Side);
        false -> Acc
    end;
fields(_,Acc,_) -> Acc.

shape(Code,Definition,Call,Slot) ->
    Lifetime=lists:takewhile(fun({_,I}) -> not ends_slot_lifetime(Slot,I) end,
                             lists:dropwhile(fun({Pos,_}) -> Pos =< Definition end,Code)),
    Early=[I || {Pos,I} <- Lifetime, Pos < Call, occurs(Slot,I)],
    Late=[{Pos,I} || {Pos,I} <- Lifetime, Pos > Call, occurs(Slot,I)],
    case {Early,Late} of
        {[_|_],_} -> used_before_call;
        {[],[]} -> no_explicit_use_after_call;
        {[],[{Pos,I}]} -> {single_use,region(Code,Call,Pos),opcode(I)};
        {[],_} ->
            {Last,_}=lists:last(Late),
            {multiple_uses,region(Code,Call,Last)}
    end.

ends_slot_lifetime(Y,{move,_,Y}) -> true;
ends_slot_lifetime(Y,{swap,Y,_}) -> true;
ends_slot_lifetime(Y,{swap,_,Y}) -> true;
ends_slot_lifetime(Y,{get_tuple_element,_,_,Y}) -> true;
ends_slot_lifetime(Y,{get_list,_,Y,_}) -> true;
ends_slot_lifetime(Y,{get_list,_,_,Y}) -> true;
ends_slot_lifetime(Y,{get_hd,_,Y}) -> true;
ends_slot_lifetime(Y,{get_tl,_,Y}) -> true;
ends_slot_lifetime(Y,{put_tuple2,Y,_}) -> true;
ends_slot_lifetime(Y,{put_list,_,_,Y}) -> true;
ends_slot_lifetime(Y,{bif,_,_,_,Y}) -> true;
ends_slot_lifetime(Y,{gc_bif,_,_,_,_,Y}) -> true;
ends_slot_lifetime(Y,{init_yregs,{list,Ys}}) -> lists:member(Y,Ys);
ends_slot_lifetime(_,{trim,_,_}) -> true;
ends_slot_lifetime(_,{deallocate,_}) -> true;
ends_slot_lifetime(_,_) -> false.

region(Code,Call,Last) ->
    Ops=[opcode(I) || {Pos,I} <- Code, Pos > Call, Pos < Last],
    case lists:any(fun(Op) -> lists:member(Op,[call,call_ext,call_fun,call_fun2,apply,send,wait,wait_timeout]) end,Ops) of
        true -> additional_call;
        false ->
            case lists:member(label,Ops) of
                true -> another_block;
                false -> same_block
            end
    end.

occurs(X,X) -> true;
occurs(X,{tr,Reg,_}) -> X =:= Reg;
occurs(_,{literal,_}) -> false;
occurs(X,T) when is_tuple(T) -> lists:any(fun(E) -> occurs(X,E) end,tuple_to_list(T));
occurs(X,L) when is_list(L) -> lists:any(fun(E) -> occurs(X,E) end,L);
occurs(_,_) -> false.

opcode({test,Name,_,_}) -> Name;
opcode({bif,Name,_,_,_}) -> {bif,Name};
opcode({gc_bif,Name,_,_,_,_}) -> {gc_bif,Name};
opcode(I) when is_tuple(I) -> element(1,I);
opcode(I) -> I.
