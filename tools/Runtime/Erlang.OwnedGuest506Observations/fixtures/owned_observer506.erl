-module(owned_observer506).
-export([run/0]).
run() ->
    true = register(?MODULE, self()),
    try
        Value = owned_guest506:run(506),
        Closure = owned_guest506:make(506),
        First = Closure(),
        true = erlang:garbage_collect(),
        Second = Closure(),
        {Child, Monitor} = spawn_monitor(fun() -> owned_guest506:held(507) end),
        Held = receive {held, Child, Payload} -> Payload after 5000 -> error(held_timeout) end,
        true = exit(Child, kill),
        Down = receive {'DOWN', Monitor, process, Child, ExitReason} -> ExitReason after 5000 -> error(down_timeout) end,
        Failure = try owned_guest506:failure(506) of
            Unexpected -> {unexpected, Unexpected}
        catch Class:Reason:Stack -> {Class,Reason,Stack}
        end,
        erase(key),
        {Value, First, Second, Held, Down, Failure}
    after
        unregister(?MODULE)
    end.
