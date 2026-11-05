-module(nif_resource_oracle).
-export([run/2]).
await(Value, Held, Left) ->
    case nif_resource_fixture:stats() of
        {Value, Held} = Result -> Result;
        _ when Left > 0 -> timer:sleep(5), await(Value, Held, Left - 1);
        Other -> error({destructor_timeout, Other})
    end.
native_word_bounds() ->
    TooWide = 1 bsl (8 * erlang:system_info(wordsize)),
    {'EXIT', {system_limit, _}} = (catch erts_internal:atomics_new(1, TooWide)),
    {'EXIT', {system_limit, _}} = (catch erts_internal:counters_new(TooWide)),
    ok.
run(Path, Output) ->
    ok = native_word_bounds(),
    ok = nif_resource_fixture:load(Path),
    ok = nif_resource_fixture:native_only(),
    {1, 0} = await(1, 0, 2000),
    Parent = self(),
    {Pid, Mon} = spawn_monitor(fun() -> Parent ! {self(), nif_resource_fixture:run()} end),
    Result = receive {Pid, R} -> R after 10000 -> error(worker_timeout) end,
    receive {'DOWN', Mon, process, Pid, normal} -> ok after 10000 -> error(down_timeout) end,
    Before = await(1, 1, 2000),
    ok = nif_resource_fixture:release(),
    After = await(2, 0, 2000),
    ok = file:write_file(Output, term_to_binary({Result, Before, After})).
