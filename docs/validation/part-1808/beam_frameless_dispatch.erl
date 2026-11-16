-module(beam_frameless_dispatch).
-export([run/4]).

run(passive, _, 0, []) ->
    receive
        notify -> done()
    after 0 -> passive
    end;
run(processes, Limit, 0, []) ->
    run(ports, Limit, 0, collect());
run(ports, _, 0, []) -> done();
run(State, Limit, Outstanding, [First|Rest]) when Outstanding < Limit ->
    note(First),
    run(State, Limit, Outstanding+1, Rest);
run(State, Limit, Outstanding, Remaining) ->
    receive
        notify -> {State, Limit, Outstanding, Remaining}
    after 0 -> {waiting, Outstanding}
    end.

note(Value) -> put(last, Value).
collect() -> [request].
done() -> {done, get(last)}.
