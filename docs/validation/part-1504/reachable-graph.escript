#!/usr/bin/env escript
-mode(compile).
main([Before,After,PathFile]) ->
    {ok,Data}=file:read_file(PathFile),
    Paths=[unicode:characters_to_list(P) || P <- binary:split(Data,<<10>>,[global]),P=/=<<>>],
    Results=[compare_module(filename:join(Before,P),filename:join(After,P),P) || P <- Paths],
    N=lists:sum([Count || {Count,_,_} <- Results]),
    Errors=lists:append([Bad || {_,Bad,_} <- Results]),
    Handlers=lists:sum([H || {_,_,H} <- Results]),
    io:format("~B modules, ~B functions compared, ~B reachable instruction graph differences, ~B handler entries checked~n",
              [length(Paths),N,length(Errors),Handlers]),
    lists:foreach(fun(E) -> io:format("~0p~n",[E]) end,lists:sublist(Errors,30)),
    true=Errors=:=[].

compare_module(Before,After,Path) ->
    {beam_file,Module,_,_,_,Old}=beam_disasm:file(Before),
    {beam_file,Module,_,_,_,New}=beam_disasm:file(After),
    true=length(Old)=:=length(New),
    Errors=lists:flatmap(fun({{function,F,A,L,C},{function,F,A,L2,C2}}) ->
        case compare_function(L,C,L2,C2) of
            ok -> [];
            Error -> [{Path,{F,A},Error}]
        end
    end,lists:zip(Old,New)),
    Handlers=length([ok || {function,_,_,_,Code}<-Old, I<-Code, handler_setup(I)]),
    {length(Old),Errors,Handlers}.

compare_function(L1,C1,L2,C2) ->
    G1=graph(C1), G2=graph(C2),
    try
        true = handler_entries(C1,G1) =:= handler_entries(C2,G2),
        compare([{position(L1,G1),position(L2,G2)}],G1,G2,#{})
    catch error:Reason -> {oracle_error,Reason} end.

handler_setup({Kind,_,{f,_}}) when Kind =:= 'catch'; Kind =:= 'try' -> true;
handler_setup(_) -> false.

handler_entries(Code,G) ->
    [{Kind,Slot,first_handler_instruction(position(L,G),G)} ||
        I={Kind,Slot,{f,L}} <- Code, handler_setup(I)].

first_handler_instruction(P,G={Code,_}) ->
    case element(P,Code) of
        {label,_} -> first_handler_instruction(P+1,G);
        I -> element(1,shape(I))
    end.
graph(Code) ->
    Indexed=lists:zip(lists:seq(1,length(Code)),Code),
    {list_to_tuple(Code),maps:from_list([{L,I} || {I,{label,L}} <- Indexed])}.

position(L,{_,Labels}) -> maps:get(L,Labels).

follow(I,G,Seen) ->
    case maps:is_key(I,Seen) of
        true -> cycle;
        false ->
            {Code,_}=G,
            case I>tuple_size(Code) of
                true -> ended;
                false ->
                    case element(I,Code) of
                        {label,_} -> follow(I+1,G,Seen);
                        {jump,{f,L}} -> follow(position(L,G),G,Seen#{I=>true});
                        _ -> I
                    end
            end
    end.

compare([],_,_,_) -> ok;
compare([{I,J}|Rest],G1,G2,Seen) ->
    K={follow(I,G1,#{}),follow(J,G2,#{})},
    case {K,maps:is_key(K,Seen)} of
        {_,true} -> compare(Rest,G1,G2,Seen);
        {{P,Q},false} when is_integer(P),is_integer(Q) ->
            {C1,_}=G1, {C2,_}=G2,
            L=element(P,C1), R=element(Q,C2),
            {S1,B1}=shape(L), {S2,B2}=shape(R),
            case S1=:=S2 andalso length(B1)=:=length(B2) of
                true ->
                    Edges=[{position(L1,G1),position(L2,G2)} || {L1,L2} <- lists:zip(B1,B2)],
                    Next=case terminal(L) of true -> Edges; false -> [{P+1,Q+1}|Edges] end,
                    compare(Next++Rest,G1,G2,Seen#{K=>true});
                false -> {instruction,L,R}
            end;
        {{X,X},false} -> compare(Rest,G1,G2,Seen);
        _ -> {edge,K}
    end.

shape({make_fun3,Target,Index,_OldUnique,Dest,Captures}) ->
    shape({make_fun3,Target,Index,Dest,Captures});
shape({tr,Register,_}) -> shape(Register);
shape({literal,_}=T) -> {T,[]};
shape({f,0}=T) -> {T,[]};
shape({f,L}) when is_integer(L) -> {{f,'@label'},[L]};
shape(T) when is_tuple(T) ->
    {S,B}=shape(tuple_to_list(T)),
    {list_to_tuple(S),B};
shape(T) when is_list(T) ->
    {Ss,Bs}=lists:unzip([shape(X) || X <- T]),
    {Ss,lists:append(Bs)};
shape(T) -> {T,[]}.

terminal({bif,raise,{f,0},_,_}) -> true;
terminal(I) when is_tuple(I) -> terminal(element(1,I));
terminal(Op) ->
    lists:member(Op,[return,func_info,select,select_val,select_tuple_arity,
                     call_only,call_last,call_ext_only,call_ext_last,apply_last,
                     case_end,if_end,badmatch,badrecord,try_case_end,raise,loop_rec_end,wait]).