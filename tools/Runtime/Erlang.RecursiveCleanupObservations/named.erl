-module(supplemental_cleanup_named).
-export([run/0]).

run() ->
    Owner = self(),
    {Child, Monitor} = spawn_monitor(fun() -> walk(256, Owner) end),
    receive {ready, Child} -> ok after 3000 -> error(no_ready) end,
    true = exit(Child, kill),
    receive
        {'DOWN', Monitor, process, Child, killed} -> ok
    after 3000 -> error(no_killed)
    end,
    receive
        {cleanup, Child, _} -> error(guest_after_ran)
    after 100 -> {killed, no_cleanup}
    end.

walk(0, Owner) ->
    Owner ! {ready, self()},
    receive never -> 0 end;
walk(Depth, Owner) ->
    try walk(Depth - 1, Owner) + 1
    after Owner ! {cleanup, self(), Depth}
    end.
