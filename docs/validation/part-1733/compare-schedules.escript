-mode(compile).
main([Root, Before, After, PathList]) ->
    {ok, Raw} = file:read_file(PathList),
    Paths = [binary_to_list(P) || P <- binary:split(Raw, <<10>>, [global]), P =/= <<>>],
    Data = [{P, compare(read(Root,P), read(Before,P), read(After,P))} || P <- Paths],
    Counts = lists:foldl(fun({_, M}, Acc) ->
        maps:fold(fun(K,V,A) -> maps:update_with(K, fun(N) -> N+V end,V,A) end,Acc,M)
    end,#{},Data),
    io:format("Comparable call schedules in ~B changed images. Compare same function, same call and same move operands with a single occurrence per image.~n", [length(Paths)]),
    io:format("~0p~n",[Counts]),
    [io:format("~s ~0p~n",[P,M]) || {P,M} <- Data, maps:get(lost,M,0) > 0].

read(Directory, Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(filename:join(Directory,Path)),
    maps:from_list([{Key, Orders} || {function,Name,Arity,_,Code} <- Functions,
        {Signature,Orders} <- maps:to_list(schedules(Code,[],#{})),
        Key <- [{Name,Arity,Signature}]]).

schedules([{move,_,{x,_}}=Move|Code], Moves, Acc) -> schedules(Code,[Move|Moves],Acc);
schedules([{line,_}|Code], Moves, Acc) -> schedules(Code,Moves,Acc);
schedules([Call|Code], [_,_|_]=Moves, Acc) when is_tuple(Call) ->
    case lists:member(element(1,Call),[call,call_ext,call_only,call_ext_only,call_last,call_ext_last,apply,apply_last]) of
        true ->
            Key = {Call, lists:sort(Moves)},
            schedules(Code,[],maps:update_with(Key,fun(Orders) -> [Moves|Orders] end,[Moves],Acc));
        false -> schedules(Code,[],Acc)
    end;
schedules([_|Code], _, Acc) -> schedules(Code,[],Acc);
schedules([], _, Acc) -> Acc.

compare(Otp, Before, After) ->
    maps:fold(fun(Key, Orders, Acc) ->
        case {maps:get(Key,Otp,[]), maps:get(Key,Before,[]), Orders} of
            {[Moves],[Moves],[Moves]} -> Acc;
            {[Moves],[Moves],[_]} -> count(lost,Acc);
            {[Moves],[_],[Moves]} -> count(gained,Acc);
            {[_],[Old],[New]} when Old =/= New -> count(changed_without_match,Acc);
            _ -> Acc
        end
    end,#{},After).

count(Key, Acc) -> maps:update_with(Key,fun(N) -> N+1 end,1,Acc).
