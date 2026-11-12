-module(private_case_constant_probe).
-export([run/2, run_again/2, alternate/3, get_handler/0]).

run(Items, Default) ->
    take_entry(state, 1, Items, Default).

run_again(Items, Default) ->
    take_entry(state, 1, Items, Default).

take_entry(Key, Index, Items, Default) ->
    case lists:keysearch(Key, Index, Items) of
        {value, Found} -> {Found, lists:keydelete(Key, Index, Items)};
        _ -> {Default, Items}
    end.

alternate(state, Items, Default) ->
    pick(state, 1, Items, Default);
alternate(other, Items, Default) ->
    pick(other, 1, Items, Default).

pick(Key, Index, Items, Default) ->
    case lists:keysearch(Key, Index, Items) of
        {value, Found} ->
            Rest = lists:keydelete(Key, Index, Items),
            {Found, Rest};
        _ -> {Default, Items}
    end.

get_handler() ->
    fun escaped/4.

escaped(Key, Index, Items, Default) ->
    case lists:keysearch(Key, Index, Items) of
        {value, Found} -> {Found, lists:keydelete(Key, Index, Items)};
        _ -> {Default, Items}
    end.
