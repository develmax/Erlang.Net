-module(beam_keyfind_tuple_type).
-export([lookup/1]).

lookup(Tree) ->
    Rows = erl_syntax:get_ann(Tree),
    case lists:keyfind(env, 1, Rows) of
        {env, Values} -> lookup(Tree, Values);
        _ -> erlang:error(badarg)
    end.

lookup(Tree, Values) -> {Tree, Values}.
