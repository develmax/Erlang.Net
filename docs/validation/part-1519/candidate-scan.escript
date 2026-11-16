#!/usr/bin/env escript
-mode(compile).

main([Root, Paths]) ->
    {ok, Data} = file:read_file(Paths),
    Rows = lists:flatmap(
        fun(P) -> analyze(Root, binary_to_list(P)) end,
        [P || P <- binary:split(Data, <<10>>, [global]), P =/= <<>>]),
    Modules = lists:usort([Path || {Path, _, _, _} <- Rows]),
    io:format("~B modules with ~B candidate functions from OTP abstract forms.~n", [length(Modules), length(Rows)]),
    [io:format("~0p~n", [Row]) || Row <- Rows].

analyze(Root, Path) ->
    case beam_lib:chunks(filename:join(Root, Path), [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            [{Path, Name, Arity, Kinds} ||
                {function, _, Name, Arity, Clauses} <- Forms,
                Kinds <- [lists:usort(lists:flatmap(fun classify/1, Clauses))],
                Kinds =/= []];
        _ -> []
    end.

classify({clause, _, Args, _, Body}) ->
    Previous = variable_names(Args),
    lists:usort(walk_body(Body, Previous)).

walk_body([], _) -> [];
walk_body([Item | Rest], Previous) ->
    candidate(Item, Rest, Previous) ++
        walk_body(Rest, ordsets:union(Previous, variable_names([Item]))).

candidate({match, _, {tuple, _, Items}, {call, _, _, _}}, Rest, Previous) ->
    Pattern = {tuple, 0, Items},
    case lists:any(fun({tuple, _, _}) -> true; (_) -> false end, Items) andalso plain(Pattern) of
        true ->
            Refs = ordsets:union(Previous, variable_names(Rest)),
            Names = variable_occurrences([Pattern]),
            case [V || V <- Names, V =/= '_', not ordsets:is_element(V, Refs),
                       length([N || N <- Names, N =:= V]) =:= 1] of
                [] -> [];
                _ -> [unused_nested_tuple_binding]
            end;
        false -> []
    end;
candidate({'case', _, {call, _, _, _},
           [{clause, _, [{cons, _, {var, _, Head}, {nil, _}}], [],
             [{cons, _, Prefix, {cons, _, {var, _, Head}, {nil, _}}}]},
            {clause, _, [{var, _, Fallback}], [], [{var, _, Fallback}]}]}, [], Previous) ->
    case erl_syntax:is_literal(Prefix) andalso not ordsets:is_element(Head, Previous)
        andalso not ordsets:is_element(Fallback, Previous) of
        true -> [singleton_result_prefix];
        false -> []
    end;
candidate(_, _, _) -> [].

plain({tuple, _, Items}) -> lists:all(fun plain/1, Items);
plain({var, _, _}) -> true;
plain(Pattern) -> erl_syntax:is_literal(Pattern).

variable_names(Nodes) -> ordsets:from_list(variable_occurrences(Nodes)).
variable_occurrences(Nodes) ->
    lists:flatmap(fun(Node) ->
        erl_syntax_lib:fold(fun(V, Acc) ->
            case erl_syntax:type(V) of
                variable -> [erl_syntax:variable_name(V) | Acc];
                _ -> Acc
            end
        end, [], Node)
    end, Nodes).
