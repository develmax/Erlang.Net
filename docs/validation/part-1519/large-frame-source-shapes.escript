#!/usr/bin/env escript
-mode(compile).
main([Root,Generated,PathFile]) ->
    {ok, Content} = file:read_file(PathFile),
    Paths = [unicode:characters_to_list(Line) || Line <- binary:split(Content, <<10>>, [global]), Line =/= <<>>],
Norm=fun(Is) ->
    Labels=maps:from_list(lists:zip([L || {label,L}<-Is],lists:seq(1,length([L || {label,L}<-Is])))),
    Fix=fun F({label,L})->{label,maps:get(L,Labels,L)};
        F({f,L})->{f,maps:get(L,Labels,L)};
        F({literal,V})->{literal,V};
        F({make_fun3,M,I,_,Dst,Args})->{make_fun3,M,I,uniq,F(Dst),F(Args)};
        F(T) when is_tuple(T)->list_to_tuple([F(X)||X<-tuple_to_list(T)]);
        F([H|T])->[F(H)|F(T)];
        F(V)->V
    end,
    [Fix(I)||I<-Is]
end,
Read=fun(P)->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},Norm(Is)}||{function,N,A,_,Is}<-Fs])
end,
    Rows = lists:flatmap(fun(Path) -> inspect(Path, Root, Generated, Read) end, Paths),
    Families = lists:usort(lists:append([Families || {_, _, Families} <- Rows])),
    Counts = [score(Family, Rows) || Family <- Families],
    io:format("~B functions start differing at a larger allocate with the same live input count.~n", [length(Rows)]),
    io:format("Source shapes can overlap for multiple clauses. Columns are images, functions, and shape.~n"),
    [io:format("~0p~n", [Row]) || Row <- lists:reverse(lists:sort(Counts))].

inspect(Path, Root, Generated, Read) ->
    Original = filename:join(Root, Path),
    Expected = Read(Original),
    Actual = Read(filename:join(Generated, Path)),
    Candidates = [Key || {Key, Instructions} <- maps:to_list(Expected),
                       larger_entry_frame(Instructions, maps:get(Key, Actual, []))],
    case Candidates of
        [] -> [];
        _ ->
            Sources = read_sources(Original),
            [{Path, Key, maps:get(Key, Sources, [missing_abstract_form])} || Key <- Candidates]
    end.

score(Family, Rows) ->
    Paths = [Path || {Path, _, Families} <- Rows, lists:member(Family, Families)],
    {length(lists:usort(Paths)), length(Paths), Family}.

larger_entry_frame([I | Expected], [I | Actual]) -> larger_entry_frame(Expected, Actual);
larger_entry_frame([{allocate, Expected, Live} | _], [{allocate, Actual, Live} | _]) ->
    Actual > Expected;
larger_entry_frame(_, _) -> false.

read_sources(Path) ->
    case beam_lib:chunks(Path, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            maps:from_list([{{Name, Arity}, lists:usort([classify(C) || C <- Clauses])} ||
                            {function, _, Name, Arity, Clauses} <- Forms]);
        _ -> #{}
    end.

classify({clause, _, Args, Guards, [First | Rest]}) ->
    Entry = case lists:all(fun({var, _, _}) -> true; (_) -> false end, Args) of
        true -> variables;
        false -> patterns
    end,
    Guard = case Guards of [] -> unguarded; _ -> guarded end,
    {Entry, Guard, operation(First, Rest)}.

operation({match, _, {var, _, Result}, {call, _, _, _}}, [Next | _] = Rest) ->
    {bind_call, continuation(Result, Next, Rest)};
operation(First, _) -> operation(First).

continuation(Result, {'case', _, {var, _, Result}, _}, _) -> case_on_result;
continuation(Result, Next, Rest) ->
    case call_args(Next) of
        none -> other;
        Args ->
            case lists:any(fun({var, _, Name}) -> Name =:= Result; (_) -> false end, Args) of
                false -> next_call_without_result;
                true ->
                    case Rest of
                        [{call, _, _, _}] -> tail_call_with_result;
                        _ -> later_call_with_result
                    end
            end
    end.

call_args({call, _, _, Args}) -> Args;
call_args({match, _, _, {call, _, _, Args}}) -> Args;
call_args(_) -> none.

operation({match, _, {tuple, _, _}, {call, _, _, _}}) -> match_tuple_call;
operation({match, _, _, {call, _, _, _}}) -> bind_call;
operation({match, _, _, {'case', _, _, _}}) -> bind_case;
operation({match, _, _, _}) -> match;
operation({'case', _, {call, _, _, _}, Clauses}) ->
    Shapes = lists:usort([pattern_kind(Pattern) || {clause, _, [Pattern], _, _} <- Clauses,
                          element(1, Pattern) =/= var]),
    {case_call_result, Shapes};
operation({'case', _, _, _}) -> case_value;
operation({call, _, _, _}) -> call;
operation({'try', _, _, _, _, _}) -> try_expression;
operation({op, _, _, _, _}) -> binary_operator;
operation(Expr) -> element(1, Expr).

pattern_kind({tuple, _, _}) -> tuple;
pattern_kind({cons, _, _, _}) -> list;
pattern_kind({nil, _}) -> list;
pattern_kind({match, _, {var, _, _}, Shape}) -> pattern_kind(Shape);
pattern_kind({match, _, Shape, {var, _, _}}) -> pattern_kind(Shape);
pattern_kind(Pattern) -> element(1, Pattern).
