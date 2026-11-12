#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(P, A) -> [P|A] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Category, Relative, Key} || {Key, Body} <- maps:to_list(Actual),
          Expected <- [maps:get(Key, Ref, [])],
          Fields <- [[{Index, Destination} || {get_tuple_element, _, Index, Destination} <- Expected]],
          Category <- lists:usort(projections(Body, Fields))]
    end, Files),
    Categories = lists:usort([Category || {Category, _, _} <- Rows]),
    lists:foreach(fun(Category) ->
        Entries = [{Path, Key} || {K, Path, Key} <- Rows, K =:= Category],
        Paths = lists:usort([Path || {Path, _} <- Entries]),
        io:format("~p: ~p functions in ~p images~n", [Category,length(Entries),length(Paths)]),
        io:format("  Samples: ~0p~n",[lists:sublist(lists:sort(Entries),6)])
    end, Categories).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Body} || {function, Name, Arity, _, Body} <- Functions]).

projections([{get_tuple_element, _, Index, Temporary}, {move, Temporary, {x, Dest} = Destination}|Rest], Fields) ->
    Type = case lists:member({Index,Destination},Fields) of
        true -> continuation(Rest,Temporary,Dest);
        false -> skip
    end,
    Found = case Type of skip -> []; _ -> [Type] end,
    Found ++ projections(Rest,Fields);
projections([_|Rest], Fields) ->
    projections(Rest,Fields);
projections([], _) ->
    [].

continuation([{line,_}|Rest],Temp,Dest) -> continuation(Rest,Temp,Dest);
continuation([return|_],_,0) -> return;
continuation([{deallocate,_},return|_],_,0) -> frame_return;
continuation([{call_only,Arity,_}|_],{x,Temp},Dest) when Temp >= Arity, Dest < Arity -> tail_call;
continuation([{call_ext_only,Arity,_}|_],{x,Temp},Dest) when Temp >= Arity, Dest < Arity -> tail_call;
continuation([{call_last,Arity,_,_}|_],{x,Temp},Dest) when Temp >= Arity, Dest < Arity -> tail_call;
continuation([{call_ext_last,Arity,_,_}|_],{x,Temp},Dest) when Temp >= Arity, Dest < Arity -> tail_call;
continuation([{call_last,Arity,_,_}|_],{y,_},Dest) when Dest < Arity -> frame_tail_call;
continuation([{call_ext_last,Arity,_,_}|_],{y,_},Dest) when Dest < Arity -> frame_tail_call;
continuation([{call,Arity,_}|_],{x,Temp},Dest) when Temp >= Arity, Dest < Arity -> call;
continuation([{call_ext,Arity,_}|_],{x,Temp},Dest) when Temp >= Arity, Dest < Arity -> call;
continuation(_,_,_) -> other.
