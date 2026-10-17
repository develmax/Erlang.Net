-module(common_lifecycle_wrapper_slots).
-export([run/0,leaf/0]).

run() ->
    Old = erlang:system_flag(backtrace_depth,8),
    Dictionary = get(),
    try
        Wake = proc_lib:wake_up(common_lifecycle_wrapper_slots,leaf,[]),
        [{erlang,hd,[[]]},{common_lifecycle_wrapper_slots,leaf,0},{proc_lib,wake_up,3}|_] = Wake,
        1 = length([ok || {proc_lib,wake_up,3} <- Wake]),
        Init = proc_lib:init_p(self(),[],fun common_lifecycle_wrapper_slots:leaf/0),
        [{erlang,hd,[[]]},{common_lifecycle_wrapper_slots,leaf,0},{proc_lib,init_p,3}|_] = Init,
        1 = length([ok || {proc_lib,init_p,3} <- Init]),
        Apply = proc_lib:init_p(self(),[],common_lifecycle_wrapper_slots,leaf,[]),
        [{erlang,hd,[[]]},{common_lifecycle_wrapper_slots,leaf,0},{proc_lib,init_p_do_apply,3}|_] = Apply,
        [] = [ok || {proc_lib,init_p,5} <- Apply],
        1 = length([ok || {proc_lib,init_p_do_apply,3} <- Apply]),
        done
    after
        restore('$ancestors',Dictionary),
        restore('$initial_call',Dictionary),
        erlang:system_flag(backtrace_depth,Old)
    end.

leaf() ->
    try erlang:hd([])
    catch error:badarg:Stack -> [{M,F,A} || {M,F,A,_} <- Stack]
    end.

restore(Key,Dictionary) ->
    case lists:keyfind(Key,1,Dictionary) of
        false -> erase(Key);
        {Key,Value} -> put(Key,Value)
    end.
