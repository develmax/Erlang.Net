-module(beam_reuse_dying_call_slot).
-export([run/2, twice/2, prepare/1, observe/2, observe/3, finish/1]).

run(Seed, Last) ->
    Value = prepare(Seed),
    observe(Value, Last),
    finish(Value).

twice(Seed, Last) ->
    Value = prepare(Seed),
    observe(Value, Last, Last),
    finish(Value).

prepare(Seed) -> {value, Seed}.
observe(Value, Last) -> put(observed, {Value, Last}).
observe(Value, Left, Right) -> put(observed, {Value, Left, Right}).
finish(Value) -> {Value, get(observed)}.
