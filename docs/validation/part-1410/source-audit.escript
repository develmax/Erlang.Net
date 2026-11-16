#!/usr/bin/env escript
-mode(compile).

main([Root, Generated]) ->
    Paths = filelib:fold_files(Generated, "\\.beam$", true, fun(P, Ps) -> [P|Ps] end, []),
    Prefix = length(filename:split(Generated)),
    Entries = lists:append([audit(filename:join(lists:nthtail(Prefix, filename:split(Path))), Root, Generated)
                            || Path <- Paths]),
    lists:foreach(fun report/1, [{Kind, Entries} || Kind <- [tuple_projection, callback_before_call, call_result_tuple_match, entry_call_result_tuple_match, entry_tuple_match_tail_call]]).

audit(Path, Root, Generated) ->
    case beam_lib:chunks(filename:join(Root, Path), [abstract_code]) of
        {ok,{_,[{abstract_code,{raw_abstract_v1,Forms}}]}} ->
            Reference = frames(filename:join(Root, Path)),
            Compiled = frames(filename:join(Generated, Path)),
            [{Kind, Path, N, A} || {function,_,N,A,Clauses} <- Forms,
                                  maps:is_key({N,A},Compiled),
                                  maps:get({N,A}, Reference, missing) =/= maps:get({N,A}, Compiled),
                                  Kind <- lists:usort(entry_tuple_match(Clauses) ++ walk(Clauses))];
        _ -> []
    end.

entry_tuple_match([{clause,_,Heads,[],[{match,_,{tuple,_,Fields},{call,_,_,_}}|Tail]}]) ->
    case Tail =/= [] andalso lists:all(fun({var,_,_})->true; (_)->false end, Heads ++ Fields) of
        true ->
            case Tail of
                [{call,_,_,_}] -> [entry_call_result_tuple_match, entry_tuple_match_tail_call];
                _ -> [entry_call_result_tuple_match]
            end;
        false -> []
    end;
entry_tuple_match(_) -> [].

walk({call,_,_,Args}=Node) ->
    Extras = case lists:any(fun tuple_projection/1, Args) of true -> [tuple_projection]; false -> [] end,
    Cross = case crosses_call(Args) of true -> [callback_before_call]; false -> [] end,
    Extras ++ Cross ++ walk(tuple_to_list(Node));
walk({match,_,{tuple,_,Fields},{call,_,_,_}}=Node) ->
    Match = case lists:all(fun({var,_,_})->true; (_)->false end, Fields) of
                true -> [call_result_tuple_match]; false -> []
            end,
    Match ++ walk(tuple_to_list(Node));
walk(Tuple) when is_tuple(Tuple) -> walk(tuple_to_list(Tuple));
walk([First|Rest]) -> walk(First) ++ walk(Rest);
walk(_) -> [].

crosses_call([{'fun',_,{clauses,_}}|Rest]) -> lists:any(fun has_call/1, Rest) orelse crosses_call(Rest);
crosses_call([_|Rest]) -> crosses_call(Rest);
crosses_call([]) -> false.

has_call({call,_,_,_}) -> true;
has_call(T) when is_tuple(T) -> has_call(tuple_to_list(T));
has_call([First|Rest]) -> has_call(First) orelse has_call(Rest);
has_call(_) -> false.

tuple_projection({lc,_,{var,_,Result},[{generate,_,{tuple,_,Fields},{var,_,_}}]}) ->
    Names = [Name || {var,_,Name} <- Fields, Name =/= '_'],
    lists:all(fun({var,_,_})->true; (_)->false end, Fields)
      andalso lists:member(Result, Names) andalso length(Names) =:= length(lists:usort(Names));
tuple_projection(_) -> false.

report({Kind, Entries}) ->
    Rows = lists:sort([{Path,N,A} || {Category,Path,N,A} <- Entries, Category =:= Kind]),
    io:format("~p ~B modules ~B functions ~p~n",
              [Kind,length(lists:usort([P||{P,_,_}<-Rows])),length(Rows),lists:sublist(Rows,5)]).

frames(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    maps:from_list([{{N,A},first_frame(Instructions)} || {function,N,A,_,Instructions} <- Functions]).

first_frame([{allocate,S,L}|_]) -> {S,L};
first_frame([{allocate_heap,S,_,L}|_]) -> {S,L};
first_frame([_|Rest]) -> first_frame(Rest);
first_frame([]) -> none.