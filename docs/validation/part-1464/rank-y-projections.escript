#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(P,A) -> [P|A] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Category,Relative,Key} || {Key,Body} <- maps:to_list(Actual),
            Expected <- [maps:get(Key,Ref,[])],
            Direct <- [[{Field,Reg} || {get_tuple_element,_,Field,Reg={x,_}} <- Expected]],
            Category <- lists:usort(projections(Body,Direct))]
    end, Files),
    Groups = lists:usort([Type || {Type,_,_} <- Rows]),
    Counts = [begin
        Entries=[{Path,F} || {K,Path,F} <- Rows, K=:=Type],
        {length(lists:usort([Path || {Path,_} <- Entries])),length(Entries),Type,lists:sublist(lists:sort(Entries),4)}
    end || Type <- Groups],
    lists:foreach(fun({Images,Functions,Type,Samples}) ->
        io:format("~B images, ~B functions, ~p~n  Samples: ~0p~n",[Images,Functions,Type,Samples])
    end, lists:reverse(lists:sort(Counts))).

functions(Path) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Path),
    maps:from_list([{{N,A},Ops}||{function,N,A,_,Ops}<-Fs]).

projections([{get_tuple_element,_,Field,Tmp={y,_}},{move,Tmp,Dst={x,_}}|Rest], Direct) ->
    case lists:member({Field,Dst},Direct) of
        true -> [before_call(Rest)|projections(Rest,Direct)];
        false -> projections(Rest,Direct)
    end;
projections([_|Rest], Direct) -> projections(Rest,Direct);
projections([], _) -> [].

before_call([{move,_,_}|Rest]) -> before_call(Rest);
before_call([{line,_}|Rest]) -> before_call(Rest);
before_call([{call,_,_}|Rest]) -> {after_call,next(Rest)};
before_call([{call_ext,_,_}|Rest]) -> {after_call,next(Rest)};
before_call(Rest) -> {before_call,next(Rest)}.

next([{line,_}|Rest]) -> next(Rest);
next([{test,Test,_,_}|_]) -> {test,Test};
next([Instruction|_]) when is_tuple(Instruction) -> element(1,Instruction);
next([Instruction|_]) -> Instruction;
next([]) -> end_of_function.
