-module(keyfind_use_shapes).
-export([literal_result/2, literal_error/2, dynamic_error/3, literal_call/1, literal_call_error/1, bind/2]).
literal_result(Tree, As) ->
    case lists:keyfind(env, 1, As) of
        {env, Vars} -> bind(Tree, Vars);
        _ -> []
    end.
literal_error(Tree, As) ->
    case lists:keyfind(env, 1, As) of
        {env, Vars} -> bind(Tree, Vars);
        _ -> erlang:error(badarg)
    end.
dynamic_error(Tree, Key, As) ->
    case lists:keyfind(Key, 1, As) of
        {Key, Vars} -> bind(Tree, Vars);
        _ -> erlang:error(badarg)
    end.
literal_call(Tree) ->
    As = erl_syntax:get_ann(Tree),
    case lists:keyfind(env, 1, As) of
        {env, Vars} -> bind(Tree, Vars);
        _ -> []
    end.
literal_call_error(Tree) ->
    As = erl_syntax:get_ann(Tree),
    case lists:keyfind(env, 1, As) of
        {env, Vars} -> bind(Tree, Vars);
        _ -> erlang:error(badarg)
    end.
bind(Tree, Vars) -> {Tree, Vars}.

