-module(magic_alias_observer576).
-export([run/0, worker/2]).

run() ->
    Parent = self(),
    Token = make_ref(),
    {Pid, Monitor} = spawn_monitor(?MODULE, worker, [Parent, Token]),
    {Value, Down} = try
        Value0 = receive {Token, Receipt} -> Receipt
            after 10000 -> error(missing_magic_alias_receipt576) end,
        Down0 = receive {'DOWN', Monitor, process, Pid, DownReason} -> DownReason
            after 10000 -> error(missing_magic_alias_down576) end,
        {Value0, Down0}
    catch Class:Failure:Stack ->
        exit(Pid, kill),
        receive {'DOWN', Monitor, process, Pid, _} -> ok after 10000 -> ok end,
        demonitor(Monitor, [flush]),
        erlang:raise(Class, Failure, Stack)
    end,
    normal = Down,
    {true, true, true, true, _, _, _, Sizes, Sizes} = Value,
    {Value, Down}.

worker(Parent, Token) ->
    {A, Alias, B, Copy} = native_resource_family527:aliases576(),
    Before = {sizes576(A), sizes576({A, Alias}), sizes576({A, B}),
              sizes576({A, Alias, B, Copy})},
    true = garbage_collect(),
    After = {sizes576(A), sizes576({A, Alias}), sizes576({A, B}),
             sizes576({A, Alias, B, Copy})},
    Parent ! {Token, {is_reference(A), A =:= Alias, A =:= B, A =:= Copy,
                     native_resource_family527:inspect527(A),
                     native_resource_family527:inspect527(B),
                     native_resource_family527:inspect527(Copy), Before, After}},
    ok.

sizes576(Value) ->
    {erts_debug:size(Value), erts_debug:size_shared(Value), erts_debug:flat_size(Value)}.
