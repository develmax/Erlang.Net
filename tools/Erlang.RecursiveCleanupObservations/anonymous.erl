-module(supplemental_cleanup_anonymous).
-export([run/0]).

run() ->
    Owner = self(),
    Walk = fun(_, 0) ->
                   Owner ! {ready, self()},
                   receive never -> 0 end;
              (Self, Depth) ->
                   try Self(Self, Depth - 1) + 1
                   after Owner ! {cleanup, self(), Depth}
                   end
           end,
    {Child, Monitor} = spawn_monitor(fun() -> Walk(Walk, 256) end),
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
