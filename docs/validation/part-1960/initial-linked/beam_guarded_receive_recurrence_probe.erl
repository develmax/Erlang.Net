-module(beam_guarded_receive_recurrence_probe).
-export([run/2, idle/0]).

idle() ->
    state(idle, undefined, 0, []).

run(Limit, Work) ->
    state(busy, Limit, 0, Work).

state(idle, _, 0, []) ->
    receive
        ready -> finish();
        Unexpected ->
            erlang:display({?MODULE, unexpected, Unexpected}),
            state(idle, undefined, 0, [])
    after 60_000 ->
        erlang:hibernate(?MODULE, idle, [])
    end;
state(busy, Limit, 0, []) ->
    state(drain, Limit, 0, erlang:ports());
state(drain, _, 0, []) ->
    finish();
state(Mode, Limit, Count, [Job | Rest]) when Count < Limit ->
    Started = score(Job),
    state(Mode, Limit, Count + Started, Rest);
state(Mode, Limit, Count, Jobs) ->
    receive
        ready ->
            state(Mode, Limit, Count, Jobs);
        Ack when is_pid(Ack), Count > 0 ->
            state(Mode, Limit, Count - 1, Jobs);
        Unexpected ->
            erlang:display({?MODULE, unexpected, Unexpected}),
            state(Mode, Limit, Count, Jobs)
    end.

finish() -> done.

score(Job) ->
    case Job =:= acknowledged of
        true -> 1;
        false -> 0
    end.
