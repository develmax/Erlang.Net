#!/usr/bin/env escript
-mode(compile).
main([Root, Generated]) ->
    Files = filelib:fold_files(Generated, "\\.beam$", true, fun(P, Ps) -> [P|Ps] end, []),
    RootParts = length(filename:split(Generated)),
    Rows = lists:foldl(fun(Path, Acc) ->
        Rel = filename:join(lists:nthtail(RootParts, filename:split(Path))),
        Ref = filename:join(Root, Rel),
        case beam_lib:chunks(Ref, [abstract_code]) of
            {ok,{_,[{abstract_code,{raw_abstract_v1,Forms}}]}} ->
                O=frames(Ref), C=frames(Path),
                [{kind(First),kind(Last),Rel,N,A,maps:get({N,A},O),maps:get({N,A},C),[kind(X)||X<-lists:droplast([Second,Rest|Tail])]} ||
                    {function,_,N,A,[{clause,_,Heads,[],[First,Second,Rest|Tail]}]} <- Forms,
                    lists:all(fun({var,_,_})->true; (_)->false end,Heads),
                    Last <- [lists:last([Rest|Tail])],
                    maps:is_key({N,A},O),maps:is_key({N,A},C),
                    maps:get({N,A},O) =/= maps:get({N,A},C)] ++ Acc;
            _ -> Acc
        end
    end, [], Files),
    Groups=lists:foldl(fun(Row={First,Last,_,_,_,_,_,_},Acc) ->
        maps:update_with({First,Last},fun(X)->[Row|X] end,[Row],Acc)
    end,#{},Rows),
    Ordered=lists:reverse(lists:sort([{length(lists:usort([M || {_,_,M,_,_,_,_,_}<-Rs])),length(Rs),K,Rs}
        || {K,Rs}<-maps:to_list(Groups)])),
    io:format("Scanned ~p differing images for frame mismatches in variable-head sequences.~n",[length(Files)]),
    lists:foreach(fun({M,N,K,Rs}) ->
        io:format("~p modules, ~p functions ~p ~p~n",[M,N,K,lists:sublist(lists:sort(Rs),2)])
    end, lists:sublist(Ordered,8)),
    Key = {{bound,local_call},local_call},
    Relevant = maps:get(Key,Groups,[]),
    Middle = lists:foldl(fun({_,_,M,N,A,O,C,MiddleKinds},Acc) ->
        maps:update_with(MiddleKinds,fun(X)->[{M,N,A,O,C}|X] end,[{M,N,A,O,C}],Acc)
    end,#{},Relevant),
    io:format("Largest group by middle operations:~n",[]),
    Fine = lists:reverse(lists:sort([{length(lists:usort([M||{M,_,_,_,_}<-Rs])),length(Rs),Ks,Rs}
        || {Ks,Rs} <- maps:to_list(Middle)])),
    lists:foreach(fun({M,N,Ks,Rs})->io:format("~p modules, ~p functions ~p ~p~n",[M,N,Ks,lists:sublist(lists:sort(Rs),2)]) end,
        lists:sublist(Fine,12)).
kind({match,_,{var,_,_},V})->{bound,kind(V)};
kind({call,_,{atom,_,_},_})->local_call;
kind({call,_,{remote,_,_,_},_})->remote_call;
kind({call,_,_,_})->dynamic_call;
kind({record,_,_,_})->record;
kind({record,_,_,_,_})->record;
kind(V)->element(1,V).
frames(Path) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Path),
    maps:from_list([{{N,A},frame(Is)} || {function,N,A,_,Is} <- Fs]).
frame(Is) ->
    case lists:dropwhile(fun({allocate,_,_})->false; ({allocate_heap,_,_,_})->false; (_)->true end,Is) of
        [{allocate,S,L}|_]->{S,L};
        [{allocate_heap,S,_,L}|_]->{S,L};
        []->none
    end.


