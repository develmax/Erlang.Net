-module(recursive_timeout_probe).
-export([run/0]).
run() ->
    Timeout = 1,
    self() ! again,
    kept = case is_pid(self()) of
        true -> try keep(Timeout, waiting) catch _:_ -> failed end;
        false -> failed
    end,
    self() ! again,
    changed(Timeout, waiting).
keep(Timeout, State) ->
    receive
        again -> keep(Timeout, kept)
    after Timeout ->
        State
    end.
changed(Timeout, State) ->
    receive
        again -> changed(2, changed)
    after Timeout ->
        State
    end.
