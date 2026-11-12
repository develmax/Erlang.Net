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
    Families = lists:usort(lists:append([Families || {_, _, Families, _} <- Rows])),
    Counts = [score(Family, Rows) || Family <- Families],
    io:format("~B functions have a tuple case on a call and start differing at a larger allocate with equal live inputs.~n", [length(Rows)]),
    io:format("Source shapes can overlap for multiple clauses. Columns are images, functions, and shape.~n"),
    [io:format("~0p~n", [Row]) || Row <- lists:reverse(lists:sort(Counts))],
    Projected = [{DiffCount, Path, Key} || {Path, Key, Shapes, DiffCount} <- Rows,
                    lists:member({terminal_case, unguarded, projected_variable}, Shapes)],
    io:format("Near images with an unguarded arm returning a projected variable. Columns are total differing functions, image, and candidate.~n"),
    [io:format("~0p~n", [Row]) || Row <- lists:sublist(lists:sort(Projected), 16)].

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
            Keys = lists:usort(maps:keys(Expected) ++ maps:keys(Actual)),
            DiffCount = length([Key || Key <- Keys,
                maps:get(Key, Expected, missing) =/= maps:get(Key, Actual, missing)]),
            [{Path, Key, Shapes, DiffCount} ||
                Key <- Candidates, Shapes <- [maps:get(Key, Sources, [])], Shapes =/= []]
    end.

score(Family, Rows) ->
    Paths = [Path || {Path, _, Families, _} <- Rows, lists:member(Family, Families)],
    {length(lists:usort(Paths)), length(Paths), Family}.

larger_entry_frame([I | Expected], [I | Actual]) -> larger_entry_frame(Expected, Actual);
larger_entry_frame([{allocate, Expected, Live} | _], [{allocate, Actual, Live} | _]) ->
    Actual > Expected;
larger_entry_frame(_, _) -> false.

read_sources(Path) ->
    case beam_lib:chunks(Path, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            maps:from_list([{{Name, Arity}, lists:usort(lists:flatmap(fun classify/1, Clauses))} ||
                            {function, _, Name, Arity, Clauses} <- Forms]);
        _ -> #{}
    end.

classify({clause, _, Args, [], [{'case', _, {call, _, _, _}, Arms} | Rest]}) ->
    case lists:all(fun({var, _, _}) -> true; (_) -> false end, Args) of
        true ->
            Phase = case Rest of [] -> terminal_case; _ -> continuing_case end,
            [{Phase, Guard, Outcome} || Arm <- Arms,
                                        {Guard, Outcome} <- tuple_arm(Arm)];
        false -> []
    end;
classify(_) -> [].

tuple_arm({clause, _, [Pattern], Guards, Body}) ->
    case tuple_pattern(Pattern) of
        false -> [];
        true ->
            Guard = case Guards of [] -> unguarded; _ -> guarded end,
            [{Guard, body_kind(Body, Pattern)}]
    end.

tuple_pattern({tuple, _, _}) -> true;
tuple_pattern({match, _, {var, _, _}, Shape}) -> tuple_pattern(Shape);
tuple_pattern({match, _, Shape, {var, _, _}}) -> tuple_pattern(Shape);
tuple_pattern(_) -> false.

body_kind([{call, _, Target, Args}], Pattern) ->
    case static_target(Target) of
        false -> dynamic_call;
        true ->
            Fields = erl_syntax_lib:variables(Pattern),
            Names = [Name || {var, _, Name} <- Args],
            InFields = [sets:is_element(Name, Fields) || Name <- Names],
            Kind = case lists:all(fun simple_operand/1, Args) of
                false -> computed_operands;
                true ->
                    case {lists:member(true, InFields), lists:member(false, InFields)} of
                        {true, true} -> fields_and_outer_values;
                        {true, false} -> fields_and_literals;
                        {false, _} -> no_fields
                    end
            end,
            {direct_call, Kind}
    end;
body_kind([{var, _, Name}], Pattern) ->
    case sets:is_element(Name, erl_syntax_lib:variables(Pattern)) of
        true -> projected_variable;
        false -> outer_variable
    end;
body_kind([{tuple, _, Items}], _) ->
    case lists:all(fun simple_operand/1, Items) of
        true -> tuple_of_simple_operands;
        false -> tuple_with_computed_operands
    end;
body_kind([Expr], _) ->
    case erl_syntax:is_literal(Expr) of true -> literal; false -> element(1, Expr) end;
body_kind([{match, _, _, {call, _, _, _}} | _], _) -> bind_call_then_continue;
body_kind([{call, _, _, _} | _], _) -> discard_call_then_continue;
body_kind(_, _) -> sequence.

static_target({atom, _, _}) -> true;
static_target({remote, _, {atom, _, _}, {atom, _, _}}) -> true;
static_target(_) -> false.

simple_operand({var, _, _}) -> true;
simple_operand(Arg) -> erl_syntax:is_literal(Arg).
