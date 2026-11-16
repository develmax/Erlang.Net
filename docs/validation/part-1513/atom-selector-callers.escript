#!/usr/bin/env escript
-mode(compile).
main([Root, Rows]) ->
    {ok, Text} = file:read_file(Rows),
    [analyze(Root, Line) || Line <- binary:split(Text, <<10>>, [global]), Line =/= <<>>],
    ok.

analyze(Root, Line) ->
    {ok, Tokens, _} = erl_scan:string(string:trim(binary_to_list(Line)) ++ "."),
    {ok, {Path, {Name, Arity}, {select_val, {tr,{x,Register},_}, _, _}, _, _}} = erl_parse:parse_term(Tokens),
    {ok, {_, Chunks}} = beam_lib:chunks(filename:join(Root, Path), [abstract_code, exports]),
    {raw_abstract_v1, Forms} = proplists:get_value(abstract_code, Chunks),
    Exported = lists:member({Name, Arity}, proplists:get_value(exports, Chunks)),
    Calls = [{Caller, ast_shape(lists:nth(Register + 1, Arguments))} ||
        {function, _, Caller, _, Clauses} <- Forms,
        {call, _, {atom, _, Local}, Arguments} <- walk(Clauses),
        Local =:= Name, length(Arguments) =:= Arity],
    Shapes = lists:foldl(fun({_, Shape}, M) -> maps:update_with(Shape, fun(N) -> N+1 end, 1, M) end, #{}, Calls),
    io:format("~s ~p/~p x~B exported=~p local_calls=~B ~0p~n",
              [Path, Name, Arity, Register, Exported, length(Calls), Shapes]).

walk(Tuple) when is_tuple(Tuple) ->
    [Tuple | walk(tuple_to_list(Tuple))];
walk([Head | Tail]) ->
    walk(Head) ++ walk(Tail);
walk(_) ->
    [].

ast_shape({call, _, {atom, _, Name}, Args}) ->
    {call, Name, length(Args)};
ast_shape({call, _, {remote, _, {atom, _, Module}, {atom, _, Name}}, Args}) ->
    {call, Module, Name, length(Args)};
ast_shape(Expr) ->
    element(1, Expr).
