-module(native_resource_family527).
-export([load/2, run/0, held/2, opens527/0, outside527/0, make527/0, native_only527/0, inspect527/1, count527/0, aliases576/0, binary_aliases577/0]).

load(Path, Info) -> erlang:load_nif(Path, Info).
opens527() -> erlang:nif_error(nif_not_loaded).
outside527() -> erlang:nif_error(nif_not_loaded).
make527() -> erlang:nif_error(nif_not_loaded).
native_only527() -> erlang:nif_error(nif_not_loaded).
inspect527(_) -> erlang:nif_error(nif_not_loaded).
count527() -> erlang:nif_error(nif_not_loaded).

aliases576() -> erlang:nif_error(nif_not_loaded).
binary_aliases577() -> erlang:nif_error(nif_not_loaded).

run() ->
    {Version, Initial, _} = count527(),
    ok = native_only527(),
    NativeOnly = await527(Version, Initial + 1, 2000),
    {Version, Before, _} = count527(),
    Ref = make_ref(),
    {Pid, Monitor} = spawn_monitor(?MODULE, held, [self(), Ref]),
    try
        Receipt = receive {Ref, Value} -> Value after 10000 -> error(missing_ack527) end,
        HeldCount = count527(),
        Pid ! {Ref, release},
        Down = receive {'DOWN', Monitor, process, Pid, DownReason} -> DownReason
            after 10000 -> error(missing_down527) end,
        After = await527(Version, Before + 1, 2000),
        {opens527(), outside527(), Receipt, HeldCount, Down, After, NativeOnly}
    catch Class:Failure:Stack ->
        exit(Pid, kill),
        receive {'DOWN', Monitor, process, Pid, _} -> ok after 10000 -> ok end,
        demonitor(Monitor, [flush]),
        erlang:raise(Class, Failure, Stack)
    end.

held(Parent, Ref) ->
    {Resource, Binary, NativeReceipt} = make527(),
    Parent ! {Ref, {NativeReceipt, binary_to_list(Binary), binary:referenced_byte_size(Binary)}},
    receive {Ref, release} ->
        {0, _, 4096} = inspect527(Resource),
        4096 = byte_size(Binary),
        ok
    after 10000 -> error(missing_release527)
    end.

await527(Version, Expected, 0) -> error({destructor_timeout527, Version, Expected, count527()});
await527(Version, Expected, Left) ->
    case count527() of
        {Version, Expected, _} = Value -> Value;
        {Version, Count, _} when Count < Expected -> timer:sleep(5), await527(Version, Expected, Left - 1);
        Other -> error({unexpected_destructor527, Other})
    end.
